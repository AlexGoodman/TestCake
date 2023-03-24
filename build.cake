#load "build/console_logger.cake"

#tool "nuget:?package=MSBuild.SonarQube.Runner.Tool"
#tool "nuget:?package=JetBrains.dotCover.CommandLineTools&version=2022.3.3"

#addin "nuget:?package=Cake.Sonar"

var target = Argument("target", "Start-Task");
var configuration = Argument("configuration", "Release");
var solutionFolder = "./";
var outputFolder = "./artifacts";
var testResultFolder = "./test_result";
var testCoverageResultsDirectory = $"{MakeAbsolute(Directory(testResultFolder))}/TestCoverage";
var testCoverageResultsPath = $"{testCoverageResultsDirectory}/DotCover.html";
var coverageResultsFile = new FilePath($"{testCoverageResultsDirectory}/Results.dcvr");
var sonarLogin = EnvironmentVariable("SONAR_LOGIN", "admin");
var sonarPassword = EnvironmentVariable("SONAR_PASSWORD", "password");


var cleanTask = Task("Clean")
    .Does(() => {        
        CleanDirectory(outputFolder);
        CleanDirectory(testResultFolder);
    });

var restoreTask = Task("Restore")
    .Does(() => {
        DotNetRestore(solutionFolder);
    });

var initializeSonarTask = Task("Initialize-Sonar")    
    .Does(() => {
        SonarBegin(new SonarBeginSettings{
            Name = "TestCake",
            Key = "TestCake_key",
            // Url = "http://localhost:9000",
            Url = "http://host.docker.internal:9000",
            DotCoverReportsPath = testCoverageResultsPath,
            Login = sonarLogin, 
            Password = sonarPassword,    
        });
    });

var buildTask = Task("Build")
    .IsDependentOn(restoreTask)
    .IsDependentOn(cleanTask)    
    .Does(() => {
        DotNetBuild(solutionFolder, new DotNetBuildSettings {
            NoRestore = true,
            Configuration = configuration
        });
    });

var testTask = Task("Test")
    .IsDependentOn(buildTask)
    .Does(() => {                                              
        EnsureDirectoryExists(testCoverageResultsDirectory);                                      

        var testSettings = new DotNetTestSettings() {            
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
                testRunner.DotNetTest(project.FullPath, testSettings),                
                coverageResultsFile,
                coverageSettings
            );
        }        
        
        DotCoverReport(
            coverageResultsFile, 
            new FilePath(testCoverageResultsPath), 
            new DotCoverReportSettings {
                ReportType = DotCoverReportType.HTML
            }
        );                                   
    });

var sonarAnalyseTask = Task("Sonar-Analyse")
    .IsDependentOn(initializeSonarTask)
    .IsDependentOn(buildTask)
    .Does(() => { 
        SonarEnd(new SonarEndSettings{
            Login = sonarLogin, 
            Password = sonarPassword,
        }); 
    });

var publishTeamCityTestResultsTask = Task("Publish-TeamCity-Test-Results")
    .WithCriteria(() => BuildSystem.IsRunningOnTeamCity)
    .IsDependentOn(testTask)
    .Does(() => {
        foreach (var result in GetFiles($"{testResultFolder}/*.trx")) {
            TeamCity.ImportData("vstest", result);
        }
    });

var publishTeamCityTestCoverageTask = Task("Publish-TeamCity-Test-Coverage")
    .WithCriteria(() => BuildSystem.IsRunningOnTeamCity)
    .IsDependentOn(testTask)
    .Does(() => {
        TeamCity.ImportDotCoverCoverage(
            MakeAbsolute(coverageResultsFile)
            // MakeAbsolute(Directory("./tools/JetBrains.dotCover.CommandLineTools.2021.3.4/tools"))
            // Directory("C:\\TeamCity\\buildAgent\\tools\\JetBrains.dotCover.CommandLineTools.bundled")
        );     
    }); 

var publishTask = Task("Publish") 
    .IsDependentOn(testTask)   
    .Does(() => {
        DotNetPublish(solutionFolder, new DotNetPublishSettings {
            NoRestore = true,
            Configuration = configuration,
            NoBuild = true,
            OutputDirectory = outputFolder
        });
        ConsoleMessage("Hello world!");
    });

var publishTeamCityArtifactsTask = Task("Publish-TeamCity-Artifacts")
    .WithCriteria(() => BuildSystem.IsRunningOnTeamCity)
    .IsDependentOn(publishTask)
    .Does(() => {
        TeamCity.PublishArtifacts(outputFolder);
    });    

Task("Start-Task")       
    .IsDependentOn(cleanTask)     
    .IsDependentOn(restoreTask)
    .IsDependentOn(initializeSonarTask)     
    .IsDependentOn(buildTask)     
    .IsDependentOn(testTask)              
    .IsDependentOn(sonarAnalyseTask)     
    .IsDependentOn(publishTeamCityTestResultsTask)     
    .IsDependentOn(publishTeamCityTestCoverageTask)     
    .IsDependentOn(publishTask)     
    .IsDependentOn(publishTeamCityArtifactsTask);     

RunTarget(target);