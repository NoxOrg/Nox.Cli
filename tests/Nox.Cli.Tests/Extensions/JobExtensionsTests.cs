using AutoFixture;
using Nox.Cli.Actions;
using Nox.Cli.Tests.FixtureConfig;
using Nox.Cli.Extensions;
using FluentAssertions;

namespace Nox.Cli.Tests.Extensions
{
    public class JobExtensionsTests
    {
        [Theory]
        [AutoMoqData]
        public void WhenCloneJobShouldSucceed(IFixture fixture)
        {
            // Arrange
            var noxJob = fixture.Create<NoxJob>();
            string expectedId = "testId";

            // Act
            var clonedJob = noxJob.Clone(expectedId);

            // Assert
            clonedJob.Id.Should().EndWith(expectedId);
        }
    }
}