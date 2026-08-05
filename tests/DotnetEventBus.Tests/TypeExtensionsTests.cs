using DotnetEventBus.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace DotnetEventBus.Tests
{
    public class TypeExtensionsTests
    {
        [Fact]
        public void GetFriendlyName_HappyPath_ReturnsExpectedName()
        {
            // Arrange
            var type = typeof(string);

            // Act
            var friendlyName = type.GetFriendlyName();

            // Assert
            Assert.Equal("String", friendlyName);
        }

        [Fact]
        public void IsAssignableFromNullable_HappyPath_ReturnsTrue()
        {
            // Arrange
            var type = typeof(string);
            var otherType = typeof(string);

            // Act
            var isAssignable = type.IsAssignableFromNullable(otherType);

            // Assert
            Assert.True(isAssignable);
        }

        [Fact]
        public void Implements_HappyPath_ReturnsTrue()
        {
            // Arrange
            var type = typeof(string);
            var interfaceType = typeof(IComparable);

            // Act
            var implements = type.Implements<IComparable>();

            // Assert
            Assert.True(implements);
        }

        [Fact]
        public void IsNullableType_HappyPath_ReturnsTrue()
        {
            // Arrange
            var type = typeof(string);

            // Act
            var isNullable = type.IsNullableType();

            // Assert
            Assert.True(isNullable);
        }

        [Fact]
        public void GetAllInterfaces_HappyPath_ReturnsExpectedInterfaces()
        {
            // Arrange
            var type = typeof(string);

            // Act
            var interfaces = type.GetAllInterfaces();

            // Assert
            Assert.Contains(typeof(IComparable), interfaces);
        }

        [Fact]
        public void IsInstantiable_HappyPath_ReturnsTrue()
        {
            // Arrange
            var type = typeof(string);

            // Act
            var isInstantiable = type.IsInstantiable();

            // Assert
            Assert.True(isInstantiable);
        }

        [Fact]
        public void GetFullTypeNameWithGenerics_HappyPath_ReturnsExpectedName()
        {
            // Arrange
            var type = typeof(List<string>);

            // Act
            var fullName = type.GetFullTypeNameWithGenerics();

            // Assert
            Assert.Equal("System.Collections.Generic.List`1[[System.String]]", fullName);
        }

        [Fact]
        public void InheritsFrom_HappyPath_ReturnsTrue()
        {
            // Arrange
            var type = typeof(string);
            var baseType = typeof(object);

            // Act
            var inherits = type.InheritsFrom(baseType);

            // Assert
            Assert.True(inherits);
        }

        [Fact]
        public void GetAllPublicProperties_HappyPath_ReturnsExpectedProperties()
        {
            // Arrange
            var type = typeof(string);

            // Act
            var properties = type.GetAllPublicProperties();

            // Assert
            Assert.Contains(typeof(string).GetProperty("Length"), properties);
        }

        [Fact]
        public void GetFriendlyName_NullInput_ThrowsArgumentNullException()
        {
            // Act and Assert
            Assert.Throws<ArgumentNullException>(() => ((Type)null).GetFriendlyName());
        }

        [Fact]
        public void IsAssignableFromNullable_NullInput_ThrowsArgumentNullException()
        {
            // Act and Assert
            Assert.Throws<ArgumentNullException>(() => ((Type)null).IsAssignableFromNullable(typeof(string)));
        }

        [Fact]
        public void Implements_NullInput_ThrowsArgumentNullException()
        {
            // Act and Assert
            Assert.Throws<ArgumentNullException>(() => ((Type)null).Implements<IComparable>());
        }
    }
}
