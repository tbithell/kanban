using FluentAssertions;
using Kanban.Domain;

namespace Kanban.Tests.Unit.Domain;

public class VerifyTests
{
    [Fact]
    public void IsNotNull_WhenNull_ThrowsArgumentNullException()
    {
        string? value = null;
        var act = () => Verify.That(value!).IsNotNull();
        act.Should().Throw<ArgumentNullException>().WithParameterName("value");
    }

    [Fact]
    public void IsNotNull_WhenNotNull_DoesNotThrow()
    {
        var act = () => Verify.That("hello").IsNotNull();
        act.Should().NotThrow();
    }

    [Fact]
    public void IsNotDefault_WhenGuidEmpty_ThrowsArgumentException()
    {
        var act = () => Verify.That(Guid.Empty).IsNotDefault();
        act.Should().Throw<ArgumentException>().WithParameterName("value");
    }

    [Fact]
    public void IsNotDefault_WhenZeroInt_ThrowsArgumentException()
    {
        var act = () => Verify.That(0).IsNotDefault();
        act.Should().Throw<ArgumentException>().WithParameterName("value");
    }

    [Fact]
    public void IsNotDefault_WhenValidGuid_DoesNotThrow()
    {
        var act = () => Verify.That(Guid.NewGuid()).IsNotDefault();
        act.Should().NotThrow();
    }

    [Fact]
    public void IsNotEmpty_String_WhenEmpty_ThrowsArgumentException()
    {
        var act = () => Verify.That(string.Empty).IsNotNull().IsNotEmpty();
        act.Should().Throw<ArgumentException>().WithParameterName("value");
    }

    [Fact]
    public void IsNotEmpty_String_WhenNotEmpty_DoesNotThrow()
    {
        var act = () => Verify.That("hello").IsNotNull().IsNotEmpty();
        act.Should().NotThrow();
    }

    [Fact]
    public void HasMaxLength_WhenExceeds_ThrowsArgumentException()
    {
        var act = () => Verify.That("toolong").IsNotNull().HasMaxLength(3);
        act.Should().Throw<ArgumentException>().WithParameterName("value");
    }

    [Fact]
    public void HasMaxLength_WhenWithinLimit_DoesNotThrow()
    {
        var act = () => Verify.That("ok").IsNotNull().HasMaxLength(10);
        act.Should().NotThrow();
    }

    [Fact]
    public void IsPositive_WhenZero_ThrowsArgumentException()
    {
        var act = () => Verify.That(0).IsPositive();
        act.Should().Throw<ArgumentException>().WithParameterName("value");
    }

    [Fact]
    public void IsPositive_WhenNegative_ThrowsArgumentException()
    {
        var act = () => Verify.That(-1).IsPositive();
        act.Should().Throw<ArgumentException>().WithParameterName("value");
    }

    [Fact]
    public void IsPositive_WhenPositive_DoesNotThrow()
    {
        var act = () => Verify.That(1).IsPositive();
        act.Should().NotThrow();
    }

    [Fact]
    public void IsNonNegative_WhenNegative_ThrowsArgumentException()
    {
        var act = () => Verify.That(-1).IsNonNegative();
        act.Should().Throw<ArgumentException>().WithParameterName("value");
    }

    [Fact]
    public void IsNonNegative_WhenZero_DoesNotThrow()
    {
        var act = () => Verify.That(0).IsNonNegative();
        act.Should().NotThrow();
    }

    [Fact]
    public void IsGreaterThan_WhenEqual_ThrowsArgumentException()
    {
        var act = () => Verify.That(5).IsGreaterThan(5);
        act.Should().Throw<ArgumentException>().WithParameterName("value");
    }

    [Fact]
    public void IsGreaterThan_WhenGreater_DoesNotThrow()
    {
        var act = () => Verify.That(6).IsGreaterThan(5);
        act.Should().NotThrow();
    }

    [Fact]
    public void IsInRange_WhenBelowMin_ThrowsArgumentException()
    {
        var act = () => Verify.That(1).IsInRange(5, 10);
        act.Should().Throw<ArgumentException>().WithParameterName("value");
    }

    [Fact]
    public void IsInRange_WhenAboveMax_ThrowsArgumentException()
    {
        var act = () => Verify.That(11).IsInRange(5, 10);
        act.Should().Throw<ArgumentException>().WithParameterName("value");
    }

    [Fact]
    public void IsInRange_WhenWithinRange_DoesNotThrow()
    {
        var act = () => Verify.That(7).IsInRange(5, 10);
        act.Should().NotThrow();
    }

    [Fact]
    public void IsNotEmpty_Collection_WhenEmpty_ThrowsArgumentException()
    {
        var empty = Array.Empty<int>();
        var act = () => Verify.That(empty).IsNotEmptyCollection();
        act.Should().Throw<ArgumentException>().WithParameterName("value");
    }

    [Fact]
    public void IsNotEmpty_Collection_WhenHasItems_DoesNotThrow()
    {
        var items = new[] { 1, 2, 3 };
        var act = () => Verify.That(items).IsNotEmptyCollection();
        act.Should().NotThrow();
    }
}
