#tool nuget:?package=JetBrains.dotCover.CommandLineTools

var target = Argument("target", "Start-Task");
var configuration = Argument("configuration", "Release");
var solutionFolder = "./";
var outputFolder = "./artifacts";
var testResultFolder = "./test_result";
var testCoverageResultsDirectory = $"{MakeAbsolute(Directory(testResultFolder))}/TestCoverage";
var coverageResultsFile = new FilePath($"{testCoverageResultsDirectory}/Results.dcvr"); 

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
        EnsureDirectoryExists(testCoverageResultsDirectory);                                      

        var testSettings = new DotNetCoreTestSettings() {            
            NoRestore = true,
            Configuration = configuration,
            NoBuild = true,
            Loggers = new HashSet<string>{"trx"},
            ResultsDirectory = testResultFolder
        };
        var coverageSettings = new DotCoverCoverSettings()
            .WithFilter("+:*Api*")
            .WithFilter("-:*Tests*");
                        
        foreach(var project in GetFiles("**/Tests.csproj")) {            
            DotCoverCover(testRunner => 
                testRunner.DotNetCoreTest(project.FullPath, testSettings),                
                coverageResultsFile,
                coverageSettings
            );
        }        
        
        // DotCoverReport(
        //     coverageResultsFile, 
        //     new FilePath($"{testCoverageResultsDirectory}/DotCover.html"), 
        //     new DotCoverReportSettings {
        //         ReportType = DotCoverReportType.HTML
        //     }
        // );                                   
    });

Task("Publish-TeamCity-Test-Results")
    .WithCriteria(() => BuildSystem.IsRunningOnTeamCity)
    .IsDependentOn("Test")
    .Does(() => {
        foreach (var result in GetFiles($"{testResultFolder}/*.trx")) {
            TeamCity.ImportData("vstest", result);
        }
    });

Task("Publish-TeamCity-Test-Coverage")
    .WithCriteria(() => BuildSystem.IsRunningOnTeamCity)
    .IsDependentOn("Test")
    .Does(() => {
        TeamCity.ImportDotCoverCoverage(MakeAbsolute(coverageResultsFile));     
    }); 

Task("Publish")    
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

Task("Start-Task")   
    .IsDependentOn("Clean")     
    .IsDependentOn("Restore")     
    .IsDependentOn("Build")     
    .IsDependentOn("Test")     
    .IsDependentOn("Publish-TeamCity-Test-Results")     
    .IsDependentOn("Publish-TeamCity-Test-Coverage")     
    .IsDependentOn("Publish")     
    .IsDependentOn("Publish-TeamCity-Artifacts");     

RunTarget(target);