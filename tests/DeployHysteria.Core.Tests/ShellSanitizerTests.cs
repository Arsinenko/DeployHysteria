using DeployHysteria.Core.Security;
using FluentAssertions;
using Xunit;

namespace DeployHysteria.Core.Tests;

public class ShellSanitizerTests
{
    [Theory]
    [InlineData("hello", "'hello'")]
    [InlineData("hello world", "'hello world'")]
    [InlineData("it's", @"'it'\''s'")]
    [InlineData("$(rm -rf /)", @"'$(rm -rf /)'")]
    [InlineData("test; touch /tmp/pwned", @"'test; touch /tmp/pwned'")]
    [InlineData("var=$((1+1))", @"'var=$((1+1))'")]
    [InlineData("", "''")]
    [InlineData(null, "''")]
    public void Escape_ShouldCorrectlyEscapeArgumentsForPosixShell(string? input, string expected)
    {
        var result = ShellSanitizer.Escape(input);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("/opt/hysteria2", true)]
    [InlineData("config.yaml", true)]
    [InlineData("../secret", false)]
    [InlineData("/opt/hysteria/../etc", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("file\0name", false)]
    public void IsSafePath_ShouldDetectUnsafePaths(string? path, bool expected)
    {
        var result = ShellSanitizer.IsSafePath(path!);
        result.Should().Be(expected);
    }
}
