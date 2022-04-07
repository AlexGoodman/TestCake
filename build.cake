#tool nuget:?package=NUnit.ConsoleRunner
#tool nuget:?package=JetBrains.dotCover.CommandLineTools

#addin nuget:?package=Cake.Coverlet

var target = Argument("target", "Publish-TeamCity-Artifacts");
var configuration = Argument("configuration", "Release");
var solutionFolder = "./";
var outputFolder = "./artifacts";
var testResultFolder = "./test_result";

DirectoryPath TestResultsDirectory = "./test_result";
FilePath CodeCoverageReportFile = TestResultsDirectory + "/coverage.xml";

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
        // DotNetCoreTest(
        //     solutionFolder, 
        //     new DotNetCoreTestSettings {
        //         NoRestore = true,
        //         Configuration = configuration,
        //         NoBuild = true,
        //         Logger = "trx",
        //         ResultsDirectory = testResultFolder
        //     }, 
        //     new CoverletSettings {
        //         CollectCoverage = true,
        //         CoverletOutputDirectory = CodeCoverageReportFile.GetDirectory(),
        //         CoverletOutputName = CodeCoverageReportFile.GetFilename().ToString(),
        //         CoverletOutputFormat = CoverletOutputFormat.teamcity
        //         // CoverletOutputFormat = CoverletOutputFormat.opencover
        //     }
        // );

        // DotCoverCover(
        //     t => {                
        //         t.NUnit3(
        //             $"**/bin/{configuration}/*Tests.dll",
        //             new NUnit3Settings
        //             {
        //                 // Results = CodeCoverageReportFile,
        //                 TeamCity = true,                        
        //             }
        //         );
        //     },
        //     CodeCoverageReportFile,
        //     new DotCoverCoverSettings()
        //         .WithFilter("+:Api*")
        //         .WithFilter("-:Tests")
        // );

        // var projects = GetFiles("**/Tests.csproj");

        // foreach(var project in projects){
        //     DotCoverCover(
        //         t => {                
        //             t.DotNetCoreTest(
        //                 project.ToString(), 
        //                 new DotNetCoreTestSettings {
        //                     NoRestore = true,
        //                     Configuration = configuration,
        //                     NoBuild = true,
        //                     Loggers = new HashSet<string>{"trx"},
        //                     ResultsDirectory = testResultFolder
        //                 },
        //                 new CoverletSettings {
        //                     CollectCoverage = true,
        //                     CoverletOutputDirectory = CodeCoverageReportFile.GetDirectory(),
        //                     CoverletOutputName = CodeCoverageReportFile.GetFilename().ToString(),
        //                     CoverletOutputFormat = CoverletOutputFormat.teamcity
        //                     // CoverletOutputFormat = CoverletOutputFormat.opencover
        //                 }                                        
        //             );
        //         },
        //         CodeCoverageReportFile,
        //         new DotCoverCoverSettings()
        //             .WithFilter("+:Api*")
        //             .WithFilter("-:Tests*")
        //     );
        // }

        // TeamCity.ImportDotCoverCoverage(MakeAbsolute(CodeCoverageReportFile));    



        var outputDirectory = MakeAbsolute(Directory("./test_result"));        
        var testCoverageResultsDirectory = $"{outputDirectory}/TestCoverage";

        EnsureDirectoryExists(testCoverageResultsDirectory);
        var projects = GetFiles("**/Tests.csproj");
        var coverageResultsFile = new FilePath($"{testCoverageResultsDirectory}/Results.dcvr");
        var coverageReportFile = new FilePath($"{testCoverageResultsDirectory}/DotCover.html");                

        var testSettings = new DotNetCoreTestSettings() {
            // Configuration = "Release",
            // NoBuild = true,
            NoRestore = true,
            Configuration = configuration,
            NoBuild = true,
            Loggers = new HashSet<string>{"trx"},
            ResultsDirectory = testResultFolder
        };
        var coverageSettings = new DotCoverCoverSettings();
            // .WithFilter("+:*Api*")
            // .WithFilter("-:*Tests*");
        
        var coverageReportSettings = new DotCoverReportSettings {
            ReportType = DotCoverReportType.HTML
        };
        
        foreach(var project in projects) {
            Information("Test - 1");
            DotCoverCover(testRunner => 
                testRunner.DotNetCoreTest(project.FullPath, testSettings),                
                coverageResultsFile,
                coverageSettings
            );
        }
        Information("Test - 2");
        // DotCoverReport(coverageResultsFile, coverageReportFile, coverageReportSettings);
        
        var testResultFiles = GetFiles("**/test_result/*.trx");
        foreach(var testResultFile in testResultFiles) {
            Information(testResultFile.ToString());
            TeamCity.ImportData("nunit", testResultFile);            
        }
        
        TeamCity.ImportDotCoverCoverage(
            MakeAbsolute(coverageResultsFile)
            // ,MakeAbsolute(Directory("./tools/JetBrains.dotCover.CommandLineTools/tools"))
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