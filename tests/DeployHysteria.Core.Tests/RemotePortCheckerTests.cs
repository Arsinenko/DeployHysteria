using DeployHysteria.Core.Models.Connection;
using DeployHysteria.Core.Network;
using DeployHysteria.Core.Ssh;
using FluentAssertions;
using Moq;
using Xunit;

namespace DeployHysteria.Core.Tests;

public class RemotePortCheckerTests
{
    [Fact]
    public async Task CheckPortAvailability_WhenPortFree_ShouldReturnAvailable()
    {
        var mockSsh = new Mock<ISshClient>();
        mockSsh.Setup(s => s.ExecuteCommandAsync(It.Is<string>(cmd => cmd.Contains(":443")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommandExecutionResult(0, "", ""));

        var checker = new RemotePortChecker();
        var result = await checker.CheckPortAvailabilityAsync(mockSsh.Object, 443);

        result.IsAvailable.Should().BeTrue();
        result.OccupiedBy.Should().BeNull();
    }

    [Fact]
    public async Task CheckPortAvailability_WhenPortInUse_ShouldReturnUnavailableWithDetails()
    {
        const string mockProcess = "tcp LISTEN 0 512 0.0.0.0:443 0.0.0.0:* users:((\"nginx\",pid=1234,fd=6))";
        var mockSsh = new Mock<ISshClient>();
        mockSsh.Setup(s => s.ExecuteCommandAsync(It.Is<string>(cmd => cmd.Contains(":443")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommandExecutionResult(0, mockProcess, ""));

        var checker = new RemotePortChecker();
        var result = await checker.CheckPortAvailabilityAsync(mockSsh.Object, 443);

        result.IsAvailable.Should().BeFalse();
        result.OccupiedBy.Should().Contain("nginx");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(65536)]
    public async Task CheckPortAvailability_InvalidPort_ShouldThrow(int port)
    {
        var mockSsh = new Mock<ISshClient>();
        var checker = new RemotePortChecker();

        var act = async () => await checker.CheckPortAvailabilityAsync(mockSsh.Object, port);
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }
}
