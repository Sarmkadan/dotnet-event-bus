using System;
using System.Reflection;
using Xunit;
using DotnetEventBus.Utilities;

namespace DotnetEventBus.Tests;

public class ReflectionHelperTests
{
    // Dummy types for testing
    public interface IDummyInterface { }
    public class DummyImplementation : IDummyInterface { }
    public class DummyNoCtor { private DummyNoCtor() { } }

    public class DummyMethods
    {
        public int Add(int a, int b) => a + b;
        public void DoNothing() { }
    }

    [DummyTestAttribute]
    public class AttributedClass { }
    public class DummyTestAttribute : Attribute { }

    public class DummyProps
    {
        public int Id { get; set; }
        public string Name { get; set; } = "Test";
    }

    [Fact]
    public void FindImplementationsOf_ShouldReturnCorrectTypes()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var types = ReflectionHelper.FindImplementationsOf<IDummyInterface>(assembly);
        Assert.Contains(typeof(DummyImplementation), types);
    }

    [Fact]
    public void GetMethodsBySignature_ShouldMatchSignature()
    {
        var methods = ReflectionHelper.GetMethodsBySignature(
            typeof(DummyMethods),
            "Add",
            typeof(int),
            new[] { typeof(int), typeof(int) });
        Assert.Single(methods);
    }

    [Fact]
    public void TryCreateInstance_ShouldCreateInstance()
    {
        var instance = ReflectionHelper.TryCreateInstance<IDummyInterface>(typeof(DummyImplementation));
        Assert.NotNull(instance);
        Assert.IsType<DummyImplementation>(instance);
    }

    [Fact]
    public void AttributeOperations_ShouldDetectAndRetrieve()
    {
        Assert.True(ReflectionHelper.HasAttribute<DummyTestAttribute>(typeof(AttributedClass)));
        var attrs = ReflectionHelper.GetCustomAttributes<DummyTestAttribute>(typeof(AttributedClass));
        Assert.Single(attrs);
    }

    [Fact]
    public void PropertyOperations_ShouldGetSetAndList()
    {
        var obj = new DummyProps();
        ReflectionHelper.SetPropertyValue(obj, "Id", 42);
        Assert.Equal(42, ReflectionHelper.GetPropertyValue(obj, "Id"));

        var dict = ReflectionHelper.GetAllPropertyValues(obj);
        Assert.True(dict.ContainsKey("Name"));
    }

    [Fact]
    public void InvokeMethod_ShouldCallMethod()
    {
        var obj = new DummyMethods();
        var result = ReflectionHelper.InvokeMethod(obj, "Add", 1, 2);
        Assert.Equal(3, result);
    }

    [Fact]
    public void InvokeMethod_ShouldThrowOnMissingMethod()
    {
        var obj = new DummyMethods();
        Assert.Throws<MethodAccessException>(() => ReflectionHelper.InvokeMethod(obj, "NonExistentMethod"));
    }
}
