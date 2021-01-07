#if !NETCOREAPP2_1
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Loader;
using System.Threading.Tasks;
using CliWrap.Buffered;

namespace MinVerTests.Infra
{
    public static class Sdk
    {
        private static readonly string version = Environment.GetEnvironmentVariable("MINVER_TESTS_SDK");

        public static async Task CreateProject(string path)
        {
            FileSystem.EnsureEmptyDirectory(path);

            if (!string.IsNullOrWhiteSpace(version))
            {
                File.WriteAllText(
                    Path.Combine(path, "global.json"),
$@"{{
{"  "}""sdk"": {{
{"    "}""version"": ""{version.Trim()}"",
{"    "}""rollForward"": ""disable""
{"  "}}}
}}
");
            }

#if DEBUG
            var source = Path.GetFullPath($"{typeof(Sdk).Assembly.Location}/../../../../../MinVer/bin/Debug/");
#else
            var source = Path.GetFullPath($"{typeof(Sdk).Assembly.Location}/../../../../../MinVer/bin/Release/");
#endif

            var minVerPackageVersion = Path.GetFileNameWithoutExtension(Directory.EnumerateFiles(source, "*.nupkg").First()).Split("MinVer.", 2)[1];

            await CliWrap.Cli.Wrap("dotnet").WithArguments($"new classlib --name test --output {path}").ExecuteAsync();
            await CliWrap.Cli.Wrap("dotnet").WithArguments($"add package MinVer --source {source} --version {minVerPackageVersion} --package-directory packages").WithWorkingDirectory(path).ExecuteAsync();
            await CliWrap.Cli.Wrap("dotnet").WithArguments($"restore --source {source} --packages packages").WithWorkingDirectory(path).ExecuteAsync();
        }

        public static async Task<(Package, string)> BuildProject(string path, params (string, string)[] envVars)
        {
            var environmentVariables = envVars.ToDictionary(envVar => envVar.Item1, envVar => envVar.Item2, StringComparer.OrdinalIgnoreCase);
            _ = environmentVariables.TryAdd("MinVerVerbosity".ToAltCase(), "diagnostic");
            _ = environmentVariables.TryAdd("GeneratePackageOnBuild", "true");
            _ = environmentVariables.TryAdd("NoPackageAnalysis", "true");

            var result = await CliWrap.Cli.Wrap("dotnet").
                WithArguments($"build --no-restore{((version?.StartsWith("2.") ?? false) ? "" : " --nologo")}")
                .WithWorkingDirectory(path)
                .WithEnvironmentVariables(environmentVariables)
                .ExecuteBufferedAsync();

            var packageFileName = Directory.EnumerateFiles(path, "*.nupkg", new EnumerationOptions { RecurseSubdirectories = true }).First();
            var extractedPackageDirectoryName = Path.Combine(Path.GetDirectoryName(packageFileName), Path.GetFileNameWithoutExtension(packageFileName));
            ZipFile.ExtractToDirectory(packageFileName, extractedPackageDirectoryName);

            var nuspec = await File.ReadAllTextAsync(Directory.EnumerateFiles(extractedPackageDirectoryName, "*.nuspec").First());
            var nuspecVersion = nuspec.Split("<version>")[1].Split("</version>")[0];

            var assemblyFileName = Directory.EnumerateFiles(extractedPackageDirectoryName, "*.dll", new EnumerationOptions { RecurseSubdirectories = true }).First();

            var systemAssemblyVersion = GetAssemblyVersion(assemblyFileName);
            var assemblyVersion = new AssemblyVersion(systemAssemblyVersion.Major, systemAssemblyVersion.Minor, systemAssemblyVersion.Build, systemAssemblyVersion.Revision);

            var fileVersionInfo = FileVersionInfo.GetVersionInfo(assemblyFileName);
            var fileVersion = new FileVersion(fileVersionInfo.FileMajorPart, fileVersionInfo.FileMinorPart, fileVersionInfo.FileBuildPart, fileVersionInfo.FilePrivatePart, fileVersionInfo.ProductVersion);

            return result.ExitCode == 0
                ? (new Package(nuspecVersion, assemblyVersion, fileVersion), result.StandardOutput)
                : throw new Exception(result.StandardOutput);
        }

        private static System.Version GetAssemblyVersion(string assemblyFileName)
        {
            var assemblyLoadContext = new AssemblyLoadContext(default, true);
            var assembly = assemblyLoadContext.LoadFromAssemblyPath(assemblyFileName);

            try
            {
                return assembly.GetName().Version;
            }
            finally
            {
                assemblyLoadContext.Unload();
            }
        }
    }
}
#endif
