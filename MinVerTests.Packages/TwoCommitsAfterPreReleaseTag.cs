using System.Reflection;
using System.Threading.Tasks;
using MinVerTests.Infra;
using Xunit;

namespace MinVerTests.Packages
{
    public static class TwoCommitsAfterPreReleaseTag
    {
        [Fact]
        public static async Task HasHeightTwo()
        {
            // arrange
            var path = MethodBase.GetCurrentMethod().GetTestDirectory();
            await Sdk.CreateProject(path);

            await Git.Init(path);
            await Git.Commit(path);
            await Git.Tag(path, "1.2.3-alpha.4");
            await Git.Commit(path);
            await Git.Commit(path);

            var expected = Package.WithVersion(1, 2, 3, new[] { "alpha", "4" }, 2);

            // act
            var (sdkActual, _) = await Sdk.BuildProject(path);
            var (cliActual, _) = await MinVerCli.Run(path);

            // assert
            Assert.Equal(expected, sdkActual);
            Assert.Equal(expected.Version, cliActual);
        }
    }
}
