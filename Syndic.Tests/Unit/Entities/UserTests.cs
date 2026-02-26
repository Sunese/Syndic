using FluentAssertions;
using Syndic.ReaderDb.Entities;

namespace Syndic.Tests.Unit.Entities;

public class UserTests
{
    [Fact]
    public void Constructor_WithValidEmail_CreatesUser()
    {
        var user = new User("alice@example.com", "github");

        user.Email.Should().Be("alice@example.com");
        user.OIDCSubject.Should().Be("github");
    }

    [Fact]
    public void Constructor_WithValidEmail_SetsCreatedAtToNow()
    {
        var before = DateTimeOffset.UtcNow;
        var user = new User("alice@example.com");
        var after = DateTimeOffset.UtcNow;

        user.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void Constructor_WithValidEmail_AssignsNewGuidId()
    {
        var user = new User("alice@example.com");

        user.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Constructor_WithoutProvider_OIDCSubjectIsNull()
    {
        var user = new User("alice@example.com");

        user.OIDCSubject.Should().BeNull();
    }

    [Fact]
    public void Constructor_InitializesEmptySubscriptionsCollection()
    {
        var user = new User("alice@example.com");

        user.Subscriptions.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_TwoUsersWithSameEmail_HaveDifferentIds()
    {
        var u1 = new User("same@example.com");
        var u2 = new User("same@example.com");

        u1.Id.Should().NotBe(u2.Id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidEmail_ThrowsArgumentException(string? email)
    {
        var act = () => new User(email!);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("email");
    }
}
