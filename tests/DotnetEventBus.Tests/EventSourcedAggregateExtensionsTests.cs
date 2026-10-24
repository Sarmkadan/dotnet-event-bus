#nullable enable

using FluentAssertions;
using Xunit;
using DotnetEventBus.Advanced;

namespace DotnetEventBus.Tests;

public sealed class EventSourcedAggregateExtensionsTests
{
    private sealed record NameChanged(string Name);

    private sealed class TestAggregate : EventSourcedAggregate
    {
        public string Name { get; private set; } = string.Empty;

        public void ChangeName(string name) => RaiseEvent(new NameChanged(name));

        private void Apply(NameChanged e) => Name = e.Name;
    }

    [Fact]
    public void HasUncommittedEvents_NoEvents_ReturnsFalse()
    {
        var aggregate = new TestAggregate();

        aggregate.HasUncommittedEvents().Should().BeFalse();
    }

    [Fact]
    public void HasUncommittedEvents_AfterRaisingEvent_ReturnsTrue()
    {
        var aggregate = new TestAggregate();
        aggregate.ChangeName("test");

        aggregate.HasUncommittedEvents().Should().BeTrue();
    }

    [Fact]
    public void HasUncommittedEvents_NullAggregate_ThrowsArgumentNullException()
    {
        EventSourcedAggregate aggregate = null!;

        var act = () => aggregate.HasUncommittedEvents();

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void DeepClone_ClonesStateAndEvents()
    {
        var aggregate = new TestAggregate();
        aggregate.ChangeName("Alice");

        var clone = aggregate.DeepClone();

        clone.Should().NotBeSameAs(aggregate);
        clone.Should().BeOfType<TestAggregate>();
        clone.UncommittedEvents.Should().HaveCount(1);
        ((TestAggregate)clone).Name.Should().Be("Alice");
    }

    [Fact]
    public void DeepClone_CopiesId()
    {
        var aggregate = new TestAggregate();
        aggregate.ChangeName("Bob");
        // Id is set via protected setter; use reflection like the extension does
        typeof(EventSourcedAggregate).GetProperty("Id")!.SetValue(aggregate, "agg-123");

        var clone = aggregate.DeepClone();

        clone.Id.Should().Be("agg-123");
    }

    [Fact]
    public void DeepClone_NullAggregate_ThrowsArgumentNullException()
    {
        EventSourcedAggregate aggregate = null!;

        var act = () => aggregate.DeepClone();

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RehydrateFromEvents_AppliesEventsToAggregate()
    {
        var aggregate = new TestAggregate();
        var events = new object[] { new NameChanged("Eve") };

        var result = aggregate.RehydrateFromEvents(events);

        result.Should().BeSameAs(aggregate);
        ((TestAggregate)result).Name.Should().Be("Eve");
        result.Version.Should().Be(1);
        // LoadFromHistory does not add to uncommitted
        result.UncommittedEvents.Should().BeEmpty();
    }

    [Fact]
    public void RehydrateFromEvents_NullAggregate_ThrowsArgumentNullException()
    {
        EventSourcedAggregate aggregate = null!;

        var act = () => aggregate.RehydrateFromEvents(Array.Empty<object>());

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RehydrateFromEvents_NullEvents_ThrowsArgumentNullException()
    {
        var aggregate = new TestAggregate();

        var act = () => aggregate.RehydrateFromEvents(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
