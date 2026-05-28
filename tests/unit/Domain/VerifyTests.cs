using FluentAssertions;
using Kanban.Domain;

namespace Kanban.Tests.Unit.Domain;

public class VerifyTests
{
    [Fact]
    public void IsNotNull_WhenNull_ThrowsArgumentNullException()
    {
        string nullStr = null!;
        var act = () => Verify.That(nullStr).IsNotNull();
        act.Should().Throw<ArgumentNullException>().WithParameterName("nullStr");
    }

    [Fact]
    public void IsNotNull_WhenNotNull_DoesNotThrow()
    {
        string greeting = "hello";
        var act = () => Verify.That(greeting).IsNotNull();
        act.Should().NotThrow();
    }

    [Fact]
    public void IsNotDefault_WhenGuidEmpty_ThrowsArgumentException()
    {
        Guid id = Guid.Empty;
        var act = () => Verify.That(id).IsNotDefault();
        act.Should().Throw<ArgumentException>().WithParameterName("id");
    }

    [Fact]
    public void IsNotDefault_WhenZeroInt_ThrowsArgumentException()
    {
        int count = 0;
        var act = () => Verify.That(count).IsNotDefault();
        act.Should().Throw<ArgumentException>().WithParameterName("count");
    }

    [Fact]
    public void IsNotDefault_WhenValidGuid_DoesNotThrow()
    {
        Guid id = Guid.NewGuid();
        var act = () => Verify.That(id).IsNotDefault();
        act.Should().NotThrow();
    }

    [Fact]
    public void IsNotEmpty_String_WhenEmpty_ThrowsArgumentException()
    {
        string email = string.Empty;
        var act = () => Verify.That(email).IsNotNull().IsNotEmpty();
        act.Should().Throw<ArgumentException>().WithParameterName("email");
    }

    [Fact]
    public void IsNotEmpty_String_WhenNotEmpty_DoesNotThrow()
    {
        string email = "user@example.com";
        var act = () => Verify.That(email).IsNotNull().IsNotEmpty();
        act.Should().NotThrow();
    }

    [Fact]
    public void HasMaxLength_WhenExceeds_ThrowsArgumentException()
    {
        string name = "toolong";
        var act = () => Verify.That(name).IsNotNull().HasMaxLength(3);
        act.Should().Throw<ArgumentException>().WithParameterName("name");
    }

    [Fact]
    public void HasMaxLength_WhenWithinLimit_DoesNotThrow()
    {
        string name = "ok";
        var act = () => Verify.That(name).IsNotNull().HasMaxLength(10);
        act.Should().NotThrow();
    }

    [Fact]
    public void IsPositive_WhenZero_ThrowsArgumentException()
    {
        int position = 0;
        var act = () => Verify.That(position).IsPositive();
        act.Should().Throw<ArgumentException>().WithParameterName("position");
    }

    [Fact]
    public void IsPositive_WhenNegative_ThrowsArgumentException()
    {
        int position = -1;
        var act = () => Verify.That(position).IsPositive();
        act.Should().Throw<ArgumentException>().WithParameterName("position");
    }

    [Fact]
    public void IsPositive_WhenPositive_DoesNotThrow()
    {
        int position = 1;
        var act = () => Verify.That(position).IsPositive();
        act.Should().NotThrow();
    }

    [Fact]
    public void IsNonNegative_WhenNegative_ThrowsArgumentException()
    {
        int index = -1;
        var act = () => Verify.That(index).IsNonNegative();
        act.Should().Throw<ArgumentException>().WithParameterName("index");
    }

    [Fact]
    public void IsNonNegative_WhenZero_DoesNotThrow()
    {
        int index = 0;
        var act = () => Verify.That(index).IsNonNegative();
        act.Should().NotThrow();
    }

    [Fact]
    public void IsGreaterThan_WhenEqual_ThrowsArgumentException()
    {
        int count = 5;
        var act = () => Verify.That(count).IsGreaterThan(5);
        act.Should().Throw<ArgumentException>().WithParameterName("count");
    }

    [Fact]
    public void IsGreaterThan_WhenGreater_DoesNotThrow()
    {
        int count = 6;
        var act = () => Verify.That(count).IsGreaterThan(5);
        act.Should().NotThrow();
    }

    [Fact]
    public void IsInRange_WhenBelowMin_ThrowsArgumentException()
    {
        int length = 1;
        var act = () => Verify.That(length).IsInRange(5, 10);
        act.Should().Throw<ArgumentException>().WithParameterName("length");
    }

    [Fact]
    public void IsInRange_WhenAboveMax_ThrowsArgumentException()
    {
        int length = 11;
        var act = () => Verify.That(length).IsInRange(5, 10);
        act.Should().Throw<ArgumentException>().WithParameterName("length");
    }

    [Fact]
    public void IsInRange_WhenWithinRange_DoesNotThrow()
    {
        int length = 7;
        var act = () => Verify.That(length).IsInRange(5, 10);
        act.Should().NotThrow();
    }

    [Fact]
    public void IsNotEmptyCollection_WhenEmpty_ThrowsArgumentException()
    {
        int[] items = Array.Empty<int>();
        var act = () => Verify.That(items).IsNotEmptyCollection();
        act.Should().Throw<ArgumentException>().WithParameterName("items");
    }

    [Fact]
    public void IsNotEmptyCollection_WhenHasItems_DoesNotThrow()
    {
        int[] items = [1, 2, 3];
        var act = () => Verify.That(items).IsNotEmptyCollection();
        act.Should().NotThrow();
    }
}
