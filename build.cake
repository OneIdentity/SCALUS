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
var SignToolPath    = Argument<string>("SignToolPath", "");
var SignFiles       = Argument<bool>("SignFiles", false);

var isWindows       = Argument<bool>("isWindows", runtime.StartsWithIgnoreCase("win"));
var isLinux         = Argument<bool>("isLinux", runtime.StartsWithIgnoreCase("lin"));
var isOsx           = Argument<bool>("isOsx", runtime.StartsWithIgnoreCase("osx"));
var is64            = Argument<bool>("is64", runtime.EndsWithIgnoreCase("X64"));

var isLocalBuild = BuildSystem.IsLocalBuild;

var canSign = false;
if (SignFiles)
{
    if (SignToolPath == "")
    {
        Information("The SignFiles parameter was set to true, but the SignToolPath parameter did not have a value.");
    }
    else
    {
        if (!FileExists(SignToolPath))
        {
            Information("The sign tool file does not exist at: " + SignToolPath);
        }
        else
        {
            canSign = true;
            Information("Signing with " + SignToolPath);
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
        DotNetCoreRestore(solution,
            new DotNetRestoreSettings()
            {
                Runtime = runtime
            });
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

        MoveFile(publishdir + "/scalus.exe", publishdir + "/scalus.exe");
        CopyFile("scripts/Win/SCALUS.json", examples + "/SCALUS.json");
        CopyFile("scripts/Win/Product.wxs", tmpdir + "/Product.wxs");

        var readme = tmpdir + "/readme.txt";
        CopyFile("./scripts/readme.txt", readme);
        ReplaceTextInFiles(readme, "SCALUSVERSION", Version);

        var license = tmpdir + "/license.rtf";
        CopyFile("./scripts/license.rtf", license);

        var iconFile = tmpdir + "/icon.ico";
        var scalusExeFile = publishdir + "/scalus.exe";
        var bannerBmp = tmpdir + "/banner.bmp";
        var dialogBmp = tmpdir + "/dialog.bmp";
        if (edition == "community")
        {
            CopyFile("./src/scalus-community.ico", iconFile);
            CopyFile("./src/Banner-CommunityScalus.bmp", bannerBmp);
            CopyFile("./src/Dialog-CommunityScalus.bmp", dialogBmp);
        }
        else
        {
            CopyFile("./src/scalus-safeguard.ico", iconFile);
            CopyFile("./src/Banner-SafeguardScalus.bmp", bannerBmp);
            CopyFile("./src/Dialog-SafeguardScalus.bmp", dialogBmp);
        }

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
            Information( "Building " + runtime + " msiPath: " + msiPath);
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
       DotNetCoreBuild(solution,
            new DotNetBuildSettings()
            {
                Configuration = configuration,
                OutputDirectory = builddir,
                NoRestore = true,
                Runtime = runtime,
                Framework = "net6.0",
                MSBuildSettings = new DotNetMSBuildSettings()
                    .WithProperty("Edition", edition)
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
            DotNetTest(project.FullPath,
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
       DotNetCorePublish(
            "./src/OneIdentity.Scalus.csproj",
            new DotNetPublishSettings()
            {
                Configuration = configuration,
                DiagnosticOutput = true,
                OutputDirectory = publishdir,
                SelfContained = true,
                Runtime = runtime,
                Framework = "net6.0",
                NoRestore = true,
                IncludeAllContentForSelfExtract = true,
                PublishSingleFile = true,
                MSBuildSettings = new DotNetMSBuildSettings()
                    .WithProperty("Edition", edition)
                    .WithProperty("NativeWindowing", isWindows ? "true" : "false")
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

        CopyDirectory(builddir + "/Ui", targetdir + "/Ui");

        var resourceDir = scalusappdir + "/Contents/Resources/examples";
        CopyDirectory(exdir, resourceDir);

        CopyFile("scalusmac/.build/release/scalusmac", targetdir + "/scalusmac");

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
            Information( "Building " + runtime + " zipfile: " + zipfile);
        }
        else
        {
            Information( "Building locally " + runtime + " zipfile: " + zipfile);
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
         // https://stackoverflow.com/questions/66049792/how-to-sign-binaries-during-publishing-a-single-file-dotnet-core-app
         Information("Signing " + scalusExe);
         Sign(new string[] { scalusExe },
                new SignToolSignSettings {
                    ToolPath = SignToolPath,
                    CertSubjectName = "One Identity LLC",
                    DigestAlgorithm = SignToolDigestAlgorithm.Sha256,
                    TimeStampDigestAlgorithm = SignToolDigestAlgorithm.Sha256,
                    TimeStampUri = new Uri("http://ts.ssl.com"),
            });
        });

Task("SignMsi")
    .WithCriteria(canSign)
    .Does(() =>
    {
         Information("Signing " + msiPath);
         Sign(new string[] { msiPath },
                new SignToolSignSettings {
                    ToolPath = SignToolPath,
                    CertSubjectName = "One Identity LLC",
                    DigestAlgorithm = SignToolDigestAlgorithm.Sha256,
                    TimeStampDigestAlgorithm = SignToolDigestAlgorithm.Sha256,
                    TimeStampUri = new Uri("http://ts.ssl.com"),
            });
        });

Information("Building " + target + "(" + configuration + ")  for runtime:" + runtime  + "...");
RunTarget(target);
