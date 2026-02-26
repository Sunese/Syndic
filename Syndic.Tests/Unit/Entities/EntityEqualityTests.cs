using FluentAssertions;
using Syndic.ReaderDb.Entities;

namespace Syndic.Tests.Unit.Entities;

/// <summary>
/// Verifies the DDD entity equality semantics defined in <see cref="Entity"/>:
/// identity is determined by Id, not by reference.
/// </summary>
public class EntityEqualityTests
{
    // User is a concrete Entity subclass — use it as a representative sample.

    [Fact]
    public void Entity_EqualsItself_ByReference()
    {
        var user = new User("a@example.com");

        user.Equals(user).Should().BeTrue();
    }

    [Fact]
    public void TwoEntities_WithDifferentIds_AreNotEqual()
    {
        var u1 = new User("a@example.com");
        var u2 = new User("b@example.com");

        u1.Equals(u2).Should().BeFalse();
    }

    [Fact]
    public void Entity_IsNotEqualToNull()
    {
        var user = new User("a@example.com");

        user.Equals(null).Should().BeFalse();
    }

    [Fact]
    public void Entity_IsNotEqualToNonEntityObject()
    {
        var user = new User("a@example.com");

        user.Equals("some string").Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_IsBasedOnId()
    {
        var user = new User("a@example.com");

        user.GetHashCode().Should().Be(user.Id.GetHashCode());
    }

    [Fact]
    public void GetHashCode_DifferentEntities_DifferentHashCodes()
    {
        var u1 = new User("a@example.com");
        var u2 = new User("b@example.com");

        // Hash codes can technically collide, but for distinct GUIDs they won't
        u1.GetHashCode().Should().NotBe(u2.GetHashCode());
    }
}
