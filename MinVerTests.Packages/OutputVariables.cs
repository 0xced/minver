using System.Reflection;
using System.Threading.Tasks;
using MinVerTests.Infra;
using Xunit;

namespace MinVerTests.Packages
{
    public static class OutputVariables
    {
        [Fact]
        public static async Task AreSet()
        {
            // arrange
            var path = MethodBase.GetCurrentMethod().GetTestDirectory();
            await Sdk.CreateProject(path);
            var envVars = ("MinVerVersionOverride".ToAltCase(), "1.2.3-alpha.4+build.5");

            // act
            var (_, @out) = await Sdk.BuildProject(path, envVars: envVars);

            // assert
            Assert.Contains("MinVer: [output] MinVerVersion=1.2.3-alpha.4+build.5", @out);
            Assert.Contains("MinVer: [output] MinVerMajor=1", @out);
            Assert.Contains("MinVer: [output] MinVerMinor=2", @out);
            Assert.Contains("MinVer: [output] MinVerPatch=3", @out);
            Assert.Contains("MinVer: [output] MinVerPreRelease=alpha.4", @out);
            Assert.Contains("MinVer: [output] MinVerBuildMetadata=build.5", @out);
            Assert.Contains("MinVer: [output] AssemblyVersion=1.0.0.0", @out);
            Assert.Contains("MinVer: [output] FileVersion=1.2.3.0", @out);
            Assert.Contains("MinVer: [output] PackageVersion=1.2.3-alpha.4+build.5", @out);
            Assert.Contains("MinVer: [output] Version=1.2.3-alpha.4+build.5", @out);
        }
    }
}
