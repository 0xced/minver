using System;
using System.IO;
using System.Reflection;
using MinVerTests.Infra;
using static Bullseye.Targets;
using static SimpleExec.Command;

Target("build", () => RunAsync("dotnet", "build --configuration Release --nologo --verbosity quiet"));

Target(
    "test-lib",
    "test the MinVer.Lib library",
    DependsOn("build"),
    () => RunAsync("dotnet", $"test ./MinVerTests.Lib --framework {Environment.GetEnvironmentVariable("MINVER_TESTS_FRAMEWORK") ?? "net5.0"} --configuration Release --no-build --nologo"));

Target(
    "test-packages",
    "test the MinVer package and the minver-cli console app",
    DependsOn("build"),
    () => RunAsync("dotnet", "test ./MinVerTests.Packages --configuration Release --no-build --nologo"));

Target(
    "eyeball-minver-logs",
    "build a test project with the MinVer package to eyeball the diagnostic logs",
    DependsOn("build"),
    async () =>
    {
        var path = TestDirectory.Get("MinVer.Targets", "eyeball-minver-logs");

        await Sdk.CreateProject(path);

        await Git.Init(path);
        await Git.Commit(path);
        await Git.Tag(path, "v.1.2.3-alpha.4");
        await Git.Commit(path);

        await RunAsync(
            "dotnet",
            $"build --no-restore --nologo {path}",
            configureEnvironment: env =>
            {
                env.Add("MinVerBuildMetadata", "build.5");
                env.Add("MinVerTagPrefix", "v.");
                env.Add("MinVerVerbosity", "diagnostic");
            });
    });

Target(
    "eyeball-minver-cli-logs",
    "run the minver-cli console app on a test directory to eyeball the trace logs",
    DependsOn("build"),
    async () =>
    {
        var path = TestDirectory.Get("MinVer.Targets", "eyeball-minver-cli-logs");

        FileSystem.EnsureEmptyDirectory(path);

        await Git.Init(path);
        await Git.Commit(path);
        await Git.Tag(path, "v.1.2.3-alpha.4");
        await Git.Commit(path);

        var minVerCliPath = Path.Combine(
            MethodBase.GetCurrentMethod().DeclaringType.Assembly.Location,
            "../../../../../minver-cli/bin/Release/netcoreapp2.1/minver-cli.dll");

        await RunAsync(
            "dotnet",
            $"exec {minVerCliPath} {path}",
            configureEnvironment: env =>
            {
                env.Add("MinVerBuildMetadata", "build.5");
                env.Add("MinVerTagPrefix", "v.");
                env.Add("MinVerVerbosity", "trace");
            });
    });

Target("eyeball-logs", DependsOn("eyeball-minver-logs", "eyeball-minver-cli-logs"));

Target("default", DependsOn("test-lib", "test-packages", "eyeball-logs"));

await RunTargetsAndExitAsync(args, ex => ex is SimpleExec.NonZeroExitCodeException);
