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


Task("MsiInstaller")
    .IsDependentOn("Publish")
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

	var examples = tmpdir + "/examples";
	CopyDirectory("scripts/examples", examples);

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
	//var culture = "en-us";
	//var prodCulturePath = tmpdir + "/Product_" + culture + ".wxl";
	WiXLight(wobjFiles, new LightSettings
	{
		Extensions = new[] { "WixUIExtension", "WixUtilExtension" },
		//RawArguments = "-cultures:" + culture + " -loc " + prodCulturePath,
		OutputFile = msiPath
	});
	if (BuildSystem.AzurePipelines.IsRunningOnAzurePipelines)
	{
    		BuildSystem.AzurePipelines.Commands.WriteWarning( "Building " + runtime + " msiPath: " + msiPath);
	}
	else
	{
		Information( "Building " + runtime + " msiPath: " + msiPath);
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
    	.IsDependentOn("Publish")
	.WithCriteria(isOsx)
	.Does(() =>
	{
		var tmpdir = outputdir + "/tmp";
		var scalusappdir = tmpdir + "/scalus.app";
		var targetdir = scalusappdir + "/Contents/MacOS";
		if (!DirectoryExists(scalusappdir))
		{
			CreateDirectory(scalusappdir); 
		}
		CopyDirectory("scripts/Osx/scalus.app", scalusappdir);
		var resourceDir = scalusappdir + "/Contents/Resources/Examples";
		CreateDirectory(resourceDir); 

		CopyFile("./scripts/readme.txt", resourceDir + "/readme.txt");
		ReplaceTextInFiles(resourceDir + "/readme.txt", "SCALUSVERSION", Version);
		CopyDirectory("scripts/examples", resourceDir);

		CopyFile(publishdir + "/scalus", targetdir + "/scalus");
		CopyFile(publishdir + "/web.config", targetdir + "/web.config");

		CopyFile(publishdir + "/appsettings.json", resourceDir + "/appsettings.json");
		CopyFile(publishdir + "/scalus.json", resourceDir + "/scalus.json");
	
		var zipfile= outputdir +  "/scalus-" + Version + "_" + runtime + ".tar.gz";
		if (BuildSystem.AzurePipelines.IsRunningOnAzurePipelines)
		{
    			BuildSystem.AzurePipelines.Commands.WriteWarning( "Building " + runtime + " zipfile: " + zipfile);
		}
		else 
		{
			Information( "Building " + runtime + " zipfile: " + zipfile);
		}
		
		GZipCompress(tmpdir, zipfile);
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

		var from = publishdir + "/scalus.json";
		var to = examples + "/scalus.json";
		CopyFile(from, to);

		var zipfile= outputdir +  "/scalus-" + Version + "_" + runtime + ".tar.gz";
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


if (BuildSystem.AzurePipelines.IsRunningOnAzurePipelines)
{
    BuildSystem.AzurePipelines.Commands.WriteWarning( "Building " + target + " on azure for: " + runtime + "...");
}
else
{
	Information("Building " + target + " locally for: " + runtime  + "...");
}

Task("Default")
    .IsDependentOn("LinuxInstall")
    .IsDependentOn("OsxInstall")
    .IsDependentOn("MsiInstaller");


RunTarget(target);
