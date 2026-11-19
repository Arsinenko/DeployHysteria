using DeployHysteria.Core.Models.Hysteria;
using FluentAssertions;
using Xunit;

namespace DeployHysteria.Core.Tests;

public class HysteriaConfigUserManagementTests
{
    [Fact]
    public void GetUsers_WhenSinglePassword_ShouldReturnDefaultUser()
    {
        var config = new HysteriaConfig
        {
            Auth = AuthConfig.FromSinglePassword("pass123")
        };

        var users = config.GetUsers();

        users.Should().ContainSingle();
        users[0].Username.Should().Be("default");
        users[0].Password.Should().Be("pass123");
    }

    [Fact]
    public void AddOrUpdateUser_WhenStartingFromSinglePassword_ShouldUpgradeToUserpass()
    {
        var config = new HysteriaConfig
        {
            Auth = AuthConfig.FromSinglePassword("pass123")
        };

        config.AddOrUpdateUser("alice", "alicepass");

        config.Auth.Type.Should().Be("userpass");
        var users = config.GetUsers();
        users.Should().HaveCount(2);
        users.Should().Contain(u => u.Username == "default" && u.Password == "pass123");
        users.Should().Contain(u => u.Username == "alice" && u.Password == "alicepass");
    }

    [Fact]
    public void AddOrUpdateUser_ExistingUser_ShouldUpdatePassword()
    {
        var config = new HysteriaConfig
        {
            Auth = new AuthConfig
            {
                Type = "userpass",
                Userpass = new Dictionary<string, string> { ["bob"] = "oldpass" }
            }
        };

        config.AddOrUpdateUser("bob", "newpass");

        config.Auth.Userpass!["bob"].Should().Be("newpass");
    }

    [Fact]
    public void RemoveUser_ExistingUser_ShouldReturnTrueAndRemove()
    {
        var config = new HysteriaConfig
        {
            Auth = new AuthConfig
            {
                Type = "userpass",
                Userpass = new Dictionary<string, string>
                {
                    ["alice"] = "pass1",
                    ["bob"] = "pass2"
                }
            }
        };

        var removed = config.RemoveUser("alice");

        removed.Should().BeTrue();
        config.Auth.Userpass.Should().NotContainKey("alice");
        config.Auth.Userpass.Should().ContainKey("bob");
    }

    [Fact]
    public void RemoveUser_NonExistingUser_ShouldReturnFalse()
    {
        var config = new HysteriaConfig
        {
            Auth = new AuthConfig
            {
                Type = "userpass",
                Userpass = new Dictionary<string, string> { ["bob"] = "pass2" }
            }
        };

        var removed = config.RemoveUser("nonexistent");

        removed.Should().BeFalse();
    }
}
