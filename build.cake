#addin "Cake.Powershell&version=2.0.0"
#tool "nuget:?package=xunit.runner.console&version=2.4.2"
#tool "nuget:?package=WiX&version=3.11.2"
#addin nuget:?package=SharpZipLib&version=1.4.0
#addin nuget:?package=Cake.Compression&version=0.3.0
#addin nuget:?package=Cake.FileHelpers&version=5.0.0
#addin nuget:?package=Cake.Incubator&version=7.0.0


///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////

var target          = Argument<string>("target", "Default");
var configuration   = Argument<string>("Configuration", "Release");
var edition         = Argument<string>("Edition", "community");
var Version         = Argument<string>("Version", "1.0.0");
var runtime         = Argument<string>("Runtime", "win-x64");
var GitRevision     = Argument<string>("GitRevision", "0000000000000000000000000000000000000000");
var GitRepo         = Argument<string>("GitRepo", "broken/repo");
var CertPath        = Argument<string>("CertPath", "");
var CertPass        = Argument<string>("CertPass", "");
var ToolPath        = Argument<string>("ToolPath", "");
var SignFiles       = Argument<bool>("SignFiles", false);

var isWindows       = Argument<bool>("isWindows", runtime.StartsWithIgnoreCase("win"));
var isLinux         = Argument<bool>("isLinux", runtime.StartsWithIgnoreCase("lin"));
var isOsx           = Argument<bool>("isOsx", runtime.StartsWithIgnoreCase("osx"));
var is64            = Argument<bool>("is64", runtime.EndsWithIgnoreCase("X64"));

var isLocalBuild = BuildSystem.IsLocalBuild;

var signTool        = ToolPath + "/SignTool.exe";
var canSign = false;
if (SignFiles)
{
    if ((ToolPath == "") || (CertPath == "") || (CertPass == ""))
    {
        Information("Code sign not selected");
    }
    else
    {
        if ((!DirectoryExists(ToolPath)) || (!FileExists(CertPath)))
        {
            if (!DirectoryExists(ToolPath))
            {
                Information("Cannot sign code - invalid tool path: " + ToolPath);
            }
            if (!FileExists(CertPath))
            {
                Information("Cannot sign code - invalid cert path: " + CertPath);
            }
        }
        else
        {
            canSign = true;
            Information("Signing with " + ToolPath + ", cert:" + CertPath );
        }
    }
}
var baseDir = Context.Environment.WorkingDirectory;
var solution = "scalus.sln";
var solutionPath = baseDir.GetFilePath(solution);
var solutionDir = solutionPath.GetDirectory();
var publishdir=solutionDir + "/Publish/" + configuration + "/" + runtime;
var builddir=solutionDir + "/Build/" + configuration + "/" + runtime;
var outputdir=solutionDir + "/Output/" + configuration + "/" + runtime;
var bindir=solutionDir + "/src/bin/" + configuration;
var testdir=solutionDir + "/test/bin/" + configuration;

Information("Building in " + solutionDir);
var scalusExe=publishdir + "/scalus";
var fileToSign=publishdir + "/scalus";
var msiPath=outputdir + "/scalus-setup-" + Version + "-" + runtime + ".msi";

if (isWindows)
{
    scalusExe=publishdir + "/scalus.exe";
    fileToSign=scalusExe;
}

// Run dotnet restore to restore all package references.
Task("Restore")
    .Does(() =>
    {
        DotNetRestore();
    });


Task("WindowsInstall")
    .IsDependentOn("Publish")
    .IsDependentOn("SignPath")
    .IsDependentOn("MsiInstaller")
    .IsDependentOn("SignMsi")
    .WithCriteria(isWindows);


Task("MsiInstaller")
    .WithCriteria(isWindows)
    .Does(() =>
    {
        if (!DirectoryExists(outputdir))
        {
            CreateDirectory(outputdir);
        }
        var tmpdir = outputdir + "/tmp";
        if (!DirectoryExists(tmpdir))
        {
            CreateDirectory(tmpdir);
        }

        var sourcedir = publishdir;
        fileToSign = msiPath;

        var examples = tmpdir + "/examples";
        CopyDirectory("scripts/examples", examples);
        CopyFile("scripts/Win/SCALUS.json", examples + "/SCALUS.json");
        CopyFile("scripts/Win/Product.wxs", tmpdir + "/Product.wxs");

        var readme = tmpdir + "/readme.txt";
        CopyFile("./scripts/readme.txt", readme);
        ReplaceTextInFiles(readme, "SCALUSVERSION", Version);

        var license = tmpdir + "/license.rtf";
        CopyFile("./scripts/license.rtf", license);


        var wxsFiles = GetFiles(tmpdir + "/*.wxs");
        var arch = Architecture.X86;
        var Minimum_Version = "100";
        var Program_Files = "ProgramFilesFolder";
        if (is64)
        {
            arch=Architecture.X64;
            Minimum_Version = "200";
            Program_Files = "ProgramFiles64Folder";
        }
        WiXCandle(wxsFiles, new CandleSettings
        {
            Architecture = arch,
            WorkingDirectory = tmpdir,
            OutputDirectory = tmpdir,
            Verbose=true,
            Defines = new Dictionary<string,string>
            {
                { "sourcedir", sourcedir },
                { "tmpdir", tmpdir },
                { "Configuration", configuration },
                { "Version", Version },
                { "Minimum_Version", Minimum_Version },
                { "Program_Files", Program_Files }
            }
        });

        var wobjFiles = GetFiles(tmpdir + "/*.wixobj");
        WiXLight(wobjFiles, new LightSettings
        {
            Extensions = new[] { "WixUIExtension", "WixUtilExtension" },
            OutputFile = msiPath
        });

        if (BuildSystem.AzurePipelines.IsRunningOnAzurePipelines)
        {
            BuildSystem.AzurePipelines.Commands.WriteWarning( "Building " + runtime + " msiPath: " + msiPath);
        }
        else
        {
            Information( "Building locally " + runtime + " msiPath: " + msiPath);
        }

        DeleteDirectory(tmpdir, new DeleteDirectorySettings {
            Recursive = true,
            Force = true
        });
    });


Task("Build")
    .IsDependentOn("Restore")
    .Does(() =>
    {
       DotNetBuild(solution,
            new DotNetBuildSettings()
            {
                Configuration = configuration,
                OutputDirectory = builddir,
                MSBuildSettings = new DotNetMSBuildSettings()
                    .WithProperty("Edition", edition)
                    .WithProperty("NativeWindowing", isWindows)
            });
    });


Task("Clean")
    .Does(() =>
    {
        CleanDirectory(bindir);
        CleanDirectory(testdir);
        CleanDirectory(publishdir);
    });


Task("Test")
    .IsDependentOn("Build")
    .Does(() =>
    {
        var projects = GetFiles("./test/**/*.csproj");
        foreach(var project in projects)
        {
            DotNetTest(
                project.FullPath,
                new DotNetTestSettings()
            {
                Configuration = configuration
            });
        }
    });


Task("Publish")
    .IsDependentOn("Test")
    .Does(() =>
    {
       DotNetPublish(
            "./src/OneIdentity.Scalus.csproj",
            new DotNetPublishSettings()
            {
                Configuration = configuration,
                DiagnosticOutput = true,
                OutputDirectory = publishdir,
                SelfContained = true,
                Runtime = runtime,
                PublishSingleFile = true,
                MSBuildSettings = new DotNetMSBuildSettings() {}
            });
    });

Task("OsxInstall")
    .IsDependentOn("Publish")
    .WithCriteria(isOsx)
    .Does(() =>
    {
        var  exdir = publishdir + "/examples";

        if (DirectoryExists(exdir))
        {
            DeleteDirectory(exdir, new DeleteDirectorySettings {
                Recursive = true,
                Force = true
            });
        }

        CreateDirectory(exdir);
        CopyDirectory("scripts/examples", exdir);

        CopyFile("./scripts/readme.txt", exdir + "/readme.txt");
        ReplaceTextInFiles(exdir + "/readme.txt", "SCALUSVERSION", Version);

        CopyFile(publishdir + "/appsettings.json", exdir + "/appsettings.json");

        var tmpdir = outputdir + "/tmp";
        var scalusappdir = tmpdir + "/scalus.app";
        var targetdir = scalusappdir + "/Contents/MacOS";

        if (!DirectoryExists(scalusappdir))
        {
            CreateDirectory(scalusappdir);
        }
        CopyDirectory("scripts/Osx/scalus.app", scalusappdir);

        CopyFile("scripts/Osx/scalus.json", exdir + "/scalus.json");
        CopyFile(publishdir + "/scalus", targetdir + "/scalus");

        var resourceDir = scalusappdir + "/Contents/Resources/examples";
        CopyDirectory(exdir, resourceDir);
        var zipfile= outputdir +  "/scalus-" + Version + "-" + runtime + ".tar.gz";
        Information( "Building " + runtime + " zipfile: " + zipfile);
        GZipCompress(tmpdir, zipfile);
        DeleteDirectory(tmpdir, new DeleteDirectorySettings {
            Recursive = true,
            Force = true
        });
    });

Task("LinuxInstall")
    .IsDependentOn("Publish")
    .WithCriteria(isLinux)
    .Does(() =>
    {
        if (!DirectoryExists(outputdir))
        {
            CreateDirectory(outputdir);
        }
        var examples = publishdir + "/examples";
        CopyDirectory("scripts/examples", examples);

        var readme = publishdir + "/readme.txt";
        CopyFile("./scripts/readme.txt", readme);
        ReplaceTextInFiles(readme, "SCALUSVERSION", Version);
        CopyDirectory("scripts/Linux", publishdir);

        var zipfile= outputdir +  "/scalus-" + Version + "-" + runtime + ".tar.gz";
        if (BuildSystem.AzurePipelines.IsRunningOnAzurePipelines)
        {
                BuildSystem.AzurePipelines.Commands.WriteWarning( "Building " + runtime + " zipfile: " + zipfile);
        }
        else
        {
            Information( "Building " + runtime + " zipfile: " + zipfile);
        }

        GZipCompress(publishdir, zipfile);
    });

Task("Default")
    .IsDependentOn("LinuxInstall")
    .IsDependentOn("OsxInstall")
    .IsDependentOn("WindowsInstall");

Task("SignPath")
    .WithCriteria(canSign)
    .Does(() =>
    {
         Information("Signing " + scalusExe);
         Sign(new string[] { scalusExe },
                new SignToolSignSettings {
                    ToolPath = signTool,
                    CertPath = CertPath,
                    Password = CertPass,
                    DigestAlgorithm = SignToolDigestAlgorithm.Sha256
            });
        });

Task("SignMsi")
    .WithCriteria(canSign)
    .Does(() =>
    {
         Information("Signing " + msiPath);
         Sign(new string[] { msiPath },
                new SignToolSignSettings {
                    ToolPath = signTool,
                    CertPath = CertPath,
                    Password = CertPass,
                    DigestAlgorithm = SignToolDigestAlgorithm.Sha256
            });
        });

Information("Building " + target + "(" + configuration + ")  for runtime:" + runtime  + "...");
RunTarget(target);
