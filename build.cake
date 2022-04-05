#addin nuget:?package=Cake.Coverlet

var target = Argument("target", "Publish-TeamCity-Artifacts");
var configuration = Argument("configuration", "Release");
var solutionFolder = "./";
var outputFolder = "./artifacts";
var testResultFolder = "./test_result";
var testCoverageFile = "coverage.xml";;

Task("Clean")
    .Does(() => {
        CleanDirectory(outputFolder);
        CleanDirectory(testResultFolder);
    });

Task("Restore")
    .Does(() => {
        DotNetRestore(solutionFolder);
    });

Task("Build")
    .IsDependentOn("Restore")
    .IsDependentOn("Clean")
    .Does(() => {
        DotNetBuild(solutionFolder, new DotNetBuildSettings {
            NoRestore = true,
            Configuration = configuration
        });
    });

Task("Test")
    .IsDependentOn("Build")
    .Does(() => {    
        DotNetCoreTest(
            solutionFolder, 
            new DotNetCoreTestSettings {
                NoRestore = true,
                Configuration = configuration,
                NoBuild = true,
                Logger = "trx",
                ResultsDirectory = testResultFolder
            }, 
            new CoverletSettings {
                CollectCoverage = true,
                CoverletOutputDirectory = testResultFolder,
                CoverletOutputName = testCoverageFile,
                CoverletOutputFormat = CoverletOutputFormat.teamcity
            }
        );
    });

Task("Publish-TeamCity-Test-Results")
    .WithCriteria(() => BuildSystem.IsRunningOnTeamCity)
    .IsDependentOn("Test")
    .Does(() => {
    var testResults = GetFiles(testResultFolder + "/*.trx");

    foreach (var result in testResults) {
        TeamCity.ImportData("vstest", result);
    }
});    

Task("Publish")
    .IsDependentOn("Publish-TeamCity-Test-Results")
    .Does(() => {
        DotNetPublish(solutionFolder, new DotNetPublishSettings {
            NoRestore = true,
            Configuration = configuration,
            NoBuild = true,
            OutputDirectory = outputFolder
        });
    });

Task("Publish-TeamCity-Artifacts")
    .WithCriteria(() => BuildSystem.IsRunningOnTeamCity)
    .IsDependentOn("Publish")
    .Does(() => {
        TeamCity.PublishArtifacts(outputFolder);
    });    

RunTarget(target);