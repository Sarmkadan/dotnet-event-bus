[TestClass]
public class PredicateSubscriptionBuilderValidationTests
{
    [Test]
    public void NullPredicate_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new PredicateSubscriptionBuilder();
        // Act
        Assert.Throws<ArgumentNullException>(() => builder.WithPredicate(null));
    }

    [Test]
    public void AlwaysFalsePredicate_SubscriptionRegistersButHandlerNeverFires()
    {
        // Arrange
        var builder = new PredicateSubscriptionBuilder();
        var subscription = builder.WithPredicate(x => false);
        // Assert
        Assert.IsNotNull(subscription);
        Assert.IsFalse(subscription.HandlerInvoked);
    }

    [Test]
    public void DisposedClosurePredicate_ThrowsException()
    {
        // Arrange
        var builder = new PredicateSubscriptionBuilder();
        var closure = new Action(() => { });
        closure.Dispose();
        // Act and Assert
        Assert.Throws<Exception>(() => builder.WithPredicate(x => closure()));
    }

    [Test]
    public void TwoMutuallyExclusivePredicates_SubscriptionRegistersIndependently()
    {
        // Arrange
        var builder = new PredicateSubscriptionBuilder();
        var subscription1 = builder.WithPredicate(x => x == "event1");
        var subscription2 = builder.WithPredicate(x => x == "event2");
        // Assert
        Assert.IsNotNull(subscription1);
        Assert.IsNotNull(subscription2);
        Assert.IsTrue(subscription1.HandlerInvoked);
        Assert.IsTrue(subscription2.HandlerInvoked);
    }
}
