#addin "Cake.Powershell"

#tool "nuget:?package=xunit.runner.console"
#tool "nuget:?package=WiX"
#addin nuget:?package=SharpZipLib   
#addin nuget:?package=Cake.Compression
#addin nuget:?package=Cake.FileHelpers
#addin nuget:?package=Cake.Incubator

///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////

var target          = Argument<string>("target", "Default");
var configuration   = Argument<string>("Configuration", "Release");
var Version         = Argument<string>("Version", "1.0.0");
var runtime         = Argument<string>("Runtime", "win-x64");
var GitRevision     = Argument<string>("GitRevision", "0000000000000000000000000000000000000000");
var GitRepo         = Argument<string>("GitRepo", "broken/repo");

var isWindows       = Argument<bool>("isWindows", runtime.StartsWithIgnoreCase("win"));
var isLinux         = Argument<bool>("isLinux", runtime.StartsWithIgnoreCase("lin"));
var isOsx           = Argument<bool>("isOsx", runtime.StartsWithIgnoreCase("osx"));
var is64            = Argument<bool>("is64", runtime.EndsWithIgnoreCase("X64"));
var solutionPath = "./scalus.sln";


var publishdir="Publish/" + configuration + "/" + runtime;
var builddir="Build/" + configuration + "/" + runtime;
var outputdir="Output/" + configuration + "/" + runtime;
var bindir="src/bin/" + configuration;
var testdir="test/bin/" + configuration;

// Run dotnet restore to restore all package references.
Task("Restore")
    .Does(() =>
    {
        DotNetCoreRestore();
    });



    //.IsDependentOn("Publish")
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
	var msiPath=outputdir + "/scalus-setup-" + Version + "-" + runtime + ".msi";
	CopyDirectory("scripts/Win", tmpdir);

	var readme = tmpdir + "/readme.txt";
	CopyFile("./scripts/readme.txt", readme);
	ReplaceTextInFiles(readme, "SCALUSVERSION", Version);


        var wxsFiles = GetFiles(tmpdir + "/*.wxs");
	var arch = Architecture.X86;
	var Minimum_Version = "100";
	var Program_Files = "ProgramFilesFolder";
	if (!is64)
	{
	    arch=Architecture.X64;
	    Minimum_Version = "200";
	    Program_Files = "ProgramFiles64Folder";
	}
	Information("arch:" +  arch + ", prog:" + Program_Files);
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
	var culture = "en-us";
	var prodCulturePath = tmpdir + "/Product_" + culture + ".wxl";
	WiXLight(wobjFiles, new LightSettings
	{
		Extensions = new[] { "WixUIExtension" },
		//RawArguments = "-cultures:" + culture + " -loc " + prodCulturePath,
		OutputFile = msiPath
	});
	DeleteDirectory(tmpdir, new DeleteDirectorySettings {
	    Recursive = true,
	    Force = true
	});
    });


Task("Build")
    .IsDependentOn("Restore")
    .Does(() =>
    {
       DotNetCoreBuild(solutionPath,
           new DotNetCoreBuildSettings()
	{
	Configuration = configuration,
        OutputDirectory = builddir
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
                DotNetCoreTest(
                    project.FullPath,
                    new DotNetCoreTestSettings()
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
           "./src/scalus.csproj",
           new DotNetCorePublishSettings()
       {
           Configuration = configuration,
           DiagnosticOutput = true,
           OutputDirectory = publishdir,
           SelfContained = true,
           Runtime = runtime,
           PublishSingleFile = true,
           MSBuildSettings = new DotNetCoreMSBuildSettings()
	   {
	   }
       });
    });

Task("OsxInstall")
	.WithCriteria(isOsx)
	.Does(() =>
	{
		if (!DirectoryExists(outputdir))
		{
			CreateDirectory(outputdir); 
		}
	});

Task("LinuxInstall")
	.WithCriteria(isLinux)
	.Does(() =>
	{
		if (!DirectoryExists(outputdir))
		{
			CreateDirectory(outputdir); 
		}

		var readme = publishdir + "/readme.txt";
		CopyFile("./scripts/readme.txt", readme);
		ReplaceTextInFiles(readme, "SCALUSVERSION", Version);
		CopyDirectory("scripts/Linux", publishdir);

		var zipfile= outputdir +  "/scalus-" + Version + "_" + runtime + ".tar.gz";
		Information("zipfile:" +  zipfile);
		GZipCompress(publishdir, zipfile);
	}); 

if (BuildSystem.AzurePipelines.IsRunningOnAzurePipelines)
{
    BuildSystem.AzurePipelines.Commands.WriteWarning( "Building " + target + " on azure for: " + runtime + "...");
}
else
{
	Information("Building " + target + " locally for: " + runtime  + "...");
}
Task("Default")
    .IsDependentOn("Publish")
    .IsDependentOn("LinuxInstall")
    .IsDependentOn("OsxInstall")
    .IsDependentOn("MsiInstaller");

RunTarget(target);
