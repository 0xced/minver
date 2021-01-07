using System;
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

            var result = await CliWrap.Cli.Wrap("dotnet")
#if DEBUG
                .WithArguments($"exec {typeof(MinVerCli).Assembly.Location}/../../../../../minver-cli/bin/Debug/netcoreapp2.1/minver-cli.dll {workingDirectory}")
#else
                .WithArguments($"exec {typeof(MinVerCli).Assembly.Location}/../../../../../minver-cli/bin/Release/netcoreapp2.1/minver-cli.dll {workingDirectory}")
#endif
                .WithEnvironmentVariables(environmentVariables)
                .ExecuteBufferedAsync();

            return result.ExitCode == 0 ? (result.StandardOutput.Trim(), result.StandardError) : throw new Exception(result.StandardError);
        }
    }
}
