using System.Reflection;
using System.Threading.Tasks;
using MinVerTests.Infra;
using Xunit;

namespace MinVerTests.Packages
{
    public static class CustomDefaultPreReleasePhase
    {
        [Fact]
        public static async Task HasCustomDefaultPreReleasePhase()
        {
            // arrange
            var path = MethodBase.GetCurrentMethod().GetTestDirectory();
            await Sdk.CreateProject(path);

            await Git.Init(path);
            await Git.Commit(path);
            await Git.Tag(path, "1.2.3");
            await Git.Commit(path);

            var envVars = ("MinVerDefaultPreReleasePhase".ToAltCase(), "preview");

            var expected = Package.WithVersion(1, 2, 4, new[] { "preview", "0" }, 1);

            // act
            var (sdkActual, _) = await Sdk.BuildProject(path, envVars: envVars);
            var (cliActual, _) = await MinVerCli.Run(path, envVars: envVars);

            // assert
            Assert.Equal(expected, sdkActual);
            Assert.Equal(expected.Version, cliActual);
        }
    }
}
