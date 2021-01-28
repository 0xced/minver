using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CliWrap.Buffered;

namespace MinVerTests.Infra
{
    public static class MinVerCli
    {
        public static async Task<(string, string)> Run(string workingDirectory, string buildMetadata = null, string verbosity = "trace", params (string, string)[] envVars)
        {
            var environmentVariables = envVars.ToDictionary(envVar => envVar.Item1, envVar => envVar.Item2, StringComparer.OrdinalIgnoreCase);
            _ = environmentVariables.TryAdd("MinVerVerbosity".ToAltCase(), "trace");

            var path = Path.GetFullPath(Path.Combine(
                typeof(MinVerCli).Assembly.Location,
#if DEBUG
                "/../../../../../minver-cli/bin/Debug/netcoreapp2.1/minver-cli.dll"));
#else
                "/../../../../../minver-cli/bin/Release/netcoreapp2.1/minver-cli.dll"));
#endif

            var result = await CliWrap.Cli.Wrap("dotnet")
                .WithArguments($"exec {path} {workingDirectory}")
                .WithEnvironmentVariables(environmentVariables)
                .ExecuteBufferedAsync();

            return result.ExitCode == 0 ? (result.StandardOutput.Trim(), result.StandardError) : throw new Exception(result.StandardError);
        }
    }
}
