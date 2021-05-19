#addin "Cake.Powershell"

#tool "nuget:?package=xunit.runner.console"

///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////

var target          = Argument<string>("target", "Default");
var configuration   = Argument<string>("Configuration", "Release");
var Version         = Argument<string>("Version", "1.0.0");
var runtime         = Argument<string>("Runtime", "win10-x64");
var GitRevision     = Argument<string>("GitRevision", "0000000000000000000000000000000000000000");
var GitRepo         = Argument<string>("GitRepo", "broken/repo");

var isWindows       = Argument<bool>("isWindows", runtime.StartsWith("win"));
var isLinux         = Argument<bool>("isLinux", runtime.StartsWith("ubu"));
var isOsx           = Argument<bool>("isOsx", runtime.StartsWith("osx"));
var solutionPath = "./scalus.sln";

//runtime eg. "ubuntu.20.04-x64", "osx.10.15-x64"

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

if (BuildSystem.AzurePipelines.IsRunningOnAzurePipelines)
{
    BuildSystem.AzurePipelines.Commands.WriteWarning( "Building " + target + " on azure for: " + runtime + "...");
}
else
{
	Information("Building " + target + " locally for: " + runtime  + "...");
}
RunTarget(target);
