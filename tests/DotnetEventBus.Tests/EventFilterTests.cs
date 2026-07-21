#nullable enable

using FluentAssertions;
using Xunit;
using DotnetEventBus.Advanced;

namespace DotnetEventBus.Tests;

/// <summary>
/// Test event class used for filtering tests.
/// Contains properties that can be filtered on including order ID, amount, status, and region.
/// </summary>
public sealed class TestFilterEvent
{
	/// <summary>
	/// Gets or sets the order identifier used for filtering.
	/// </summary>
	public int OrderId { get; set; }

	/// <summary>
	/// Gets or sets the monetary amount associated with the event.
	/// </summary>
	public decimal Amount { get; set; }

	/// <summary>
	/// Gets or sets the status of the event (e.g., "Pending", "Completed").
	/// </summary>
	public string Status { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the optional region where the event occurred.
	/// Can be null for events without a specific region.
	/// </summary>
	public string? Region { get; set; }
}

public sealed class EventFilterTests
{
	[Fact]
	public void Where_WithMatchingPredicate_ShouldMatch()
	{
		// Arrange
		var filter = new EventFilter<TestFilterEvent>()
			.Where(e => e.OrderId > 100);

		var eventMatch = new TestFilterEvent { OrderId = 150 };
		var eventNoMatch = new TestFilterEvent { OrderId = 50 };

		// Act & Assert
		filter.Matches(eventMatch).Should().BeTrue();
		filter.Matches(eventNoMatch).Should().BeFalse();
	}

	[Fact]
	public void Where_WithMultiplePredicates_AllMustMatch()
	{
		// Arrange
		var filter = new EventFilter<TestFilterEvent>()
			.Where(e => e.OrderId > 100)
			.Where(e => e.Amount > 50);

		var bothMatch = new TestFilterEvent { OrderId = 150, Amount = 100 };
		var onlyFirstMatches = new TestFilterEvent { OrderId = 150, Amount = 20 };
		var neitherMatches = new TestFilterEvent { OrderId = 50, Amount = 20 };

		// Act & Assert
		filter.Matches(bothMatch).Should().BeTrue();
		filter.Matches(onlyFirstMatches).Should().BeFalse();
		filter.Matches(neitherMatches).Should().BeFalse();
	}

	[Fact]
	public void Where_WithNullPredicate_ShouldThrowArgumentNullException()
	{
		// Arrange
		var filter = new EventFilter<TestFilterEvent>();

		// Act & Assert
		filter.Invoking(f => f.Where(null!))
			.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void WhereProperty_WithMatchingValue_ShouldMatch()
	{
		// Arrange
		var filter = new EventFilter<TestFilterEvent>()
			.WhereProperty(e => e.Status, "Pending");

		var match = new TestFilterEvent { Status = "Pending" };
		var noMatch = new TestFilterEvent { Status = "Completed" };

		// Act & Assert
		filter.Matches(match).Should().BeTrue();
		filter.Matches(noMatch).Should().BeFalse();
	}

	[Fact]
	public void WhereProperty_WithNumericValue_ShouldMatch()
	{
		// Arrange
		var filter = new EventFilter<TestFilterEvent>()
			.WhereProperty(e => e.OrderId, 123);

		var match = new TestFilterEvent { OrderId = 123 };
		var noMatch = new TestFilterEvent { OrderId = 456 };

		// Act & Assert
		filter.Matches(match).Should().BeTrue();
		filter.Matches(noMatch).Should().BeFalse();
	}

	[Fact]
	public void WhereProperty_WithNullProperty_ShouldNotMatch()
	{
		// Arrange
		var filter = new EventFilter<TestFilterEvent>()
			.WhereProperty(e => e.Region, "US");

		var nullRegion = new TestFilterEvent { Region = null };
		var matchingRegion = new TestFilterEvent { Region = "US" };

		// Act & Assert
		filter.Matches(nullRegion).Should().BeFalse();
		filter.Matches(matchingRegion).Should().BeTrue();
	}

	[Fact]
	public void WherePropertyInRange_WithValueInRange_ShouldMatch()
	{
		// Arrange
		var filter = new EventFilter<TestFilterEvent>()
			.WherePropertyInRange(e => e.Amount, 50, 150);

		var inRange = new TestFilterEvent { Amount = 100 };
		var belowRange = new TestFilterEvent { Amount = 25 };
		var aboveRange = new TestFilterEvent { Amount = 200 };

		// Act & Assert
		filter.Matches(inRange).Should().BeTrue();
		filter.Matches(belowRange).Should().BeFalse();
		filter.Matches(aboveRange).Should().BeFalse();
	}

	[Fact]
	public void WherePropertyInRange_WithBoundaryValues_ShouldInclude()
	{
		// Arrange
		var filter = new EventFilter<TestFilterEvent>()
			.WherePropertyInRange(e => e.Amount, 50, 150);

		var atMin = new TestFilterEvent { Amount = 50 };
		var atMax = new TestFilterEvent { Amount = 150 };

		// Act & Assert
		filter.Matches(atMin).Should().BeTrue();
		filter.Matches(atMax).Should().BeTrue();
	}

	[Fact]
	public void WherePropertyContains_WithCaseInsensitiveMatch_ShouldMatch()
	{
		// Arrange
		var filter = new EventFilter<TestFilterEvent>()
			.WherePropertyContains(e => e.Status, "pending");

		var lowerCase = new TestFilterEvent { Status = "Pending" };
		var upperCase = new TestFilterEvent { Status = "PENDING" };
		var noMatch = new TestFilterEvent { Status = "Completed" };

		// Act & Assert
		filter.Matches(lowerCase).Should().BeTrue();
		filter.Matches(upperCase).Should().BeTrue();
		filter.Matches(noMatch).Should().BeFalse();
	}

	[Fact]
	public void WherePropertyContains_WithPartialMatch_ShouldMatch()
	{
		// Arrange
		var filter = new EventFilter<TestFilterEvent>()
			.WherePropertyContains(e => e.Status, "ing");

		var match = new TestFilterEvent { Status = "Pending" };
		var noMatch = new TestFilterEvent { Status = "Completed" };

		// Act & Assert
		filter.Matches(match).Should().BeTrue();
		filter.Matches(noMatch).Should().BeFalse();
	}

	[Fact]
	public void WherePropertyContains_WithNullProperty_ShouldNotThrow()
	{
		// Arrange
		var filter = new EventFilter<TestFilterEvent>()
			.WherePropertyContains(e => e.Region, "US");

		var nullRegion = new TestFilterEvent { Region = null };

		// Act & Assert - Should handle null gracefully
		filter.Matches(nullRegion).Should().BeFalse();
	}

	[Fact]
	public void Not_ShouldInvertPredicate()
	{
		// Arrange
		var filter = new EventFilter<TestFilterEvent>()
			.Not(e => e.Status == "Completed");

		var pending = new TestFilterEvent { Status = "Pending" };
		var completed = new TestFilterEvent { Status = "Completed" };

		// Act & Assert
		filter.Matches(pending).Should().BeTrue();
		filter.Matches(completed).Should().BeFalse();
	}

	[Fact]
	public void Complex_FilterCombination_AllPredicatesMustMatch()
	{
		// Arrange
		var filter = new EventFilter<TestFilterEvent>()
			.Where(e => e.OrderId > 100)
			.WhereProperty(e => e.Status, "Pending")
			.WherePropertyInRange(e => e.Amount, 50, 500);

		var allMatch = new TestFilterEvent { OrderId = 150, Status = "Pending", Amount = 100 };
		var failsOrderId = new TestFilterEvent { OrderId = 50, Status = "Pending", Amount = 100 };
		var failsStatus = new TestFilterEvent { OrderId = 150, Status = "Completed", Amount = 100 };
		var failsAmount = new TestFilterEvent { OrderId = 150, Status = "Pending", Amount = 600 };

		// Act & Assert
		filter.Matches(allMatch).Should().BeTrue();
		filter.Matches(failsOrderId).Should().BeFalse();
		filter.Matches(failsStatus).Should().BeFalse();
		filter.Matches(failsAmount).Should().BeFalse();
	}

	[Fact]
	public void Where_ShouldAllowChaining()
	{
		// Arrange & Act
		var filter = new EventFilter<TestFilterEvent>()
			.Where(e => e.OrderId > 0)
			.Where(e => e.Amount > 0)
			.Where(e => e.Status != "");

		// Assert - Just verify it chains without error
		var testEvent = new TestFilterEvent { OrderId = 1, Amount = 1, Status = "OK" };
		filter.Matches(testEvent).Should().BeTrue();
	}

	[Fact]
	public void Matches_WithEmptyFilterSet_ShouldMatchAll()
	{
		// Arrange
		var filter = new EventFilter<TestFilterEvent>();

		var anyEvent = new TestFilterEvent { OrderId = 0, Amount = 0, Status = "" };

		// Act & Assert
		filter.Matches(anyEvent).Should().BeTrue();
	}
}
