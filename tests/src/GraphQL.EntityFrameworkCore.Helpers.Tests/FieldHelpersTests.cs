using System;
using System.Linq.Expressions;
using GraphQL.EntityFrameworkCore.Helpers.Tests.Infrastructure;
using Xunit;

namespace GraphQL.EntityFrameworkCore.Helpers.Tests
{
    public class FieldHelpersTests
    {
        [Fact]
        public void Should_ReturnProperty_When_PropertyExists()
        {
            Expression<Func<Human, Guid>> accessor = item => item.Id;

            var property = FieldHelpers.GetPropertyInfo(accessor);

            Assert.NotNull(property);
        }

        [Fact]
        public void Should_Throw_When_AccessorIsMethod()
        {
            Expression<Func<Human, Func<string>>> accessor = item => item.Method;

            Assert.Throws<ArgumentException>(() => FieldHelpers.GetPropertyInfo(accessor));
        }

        [Fact]
        public void Should_Throw_When_AccessorIsField()
        {
            Expression<Func<Human, string>> accessor = item => item.Field;

            Assert.Throws<ArgumentException>(() => FieldHelpers.GetPropertyInfo(accessor));
        }

        [Fact]
        public void Should_ReturnPropertyName_When_PropertyNotInMap()
        {
            var type = typeof(Human);
            var property = "Id";
            Assert.Equal(type.GetProperty(property).Name, FieldHelpers.GetSchemaName(type, property));
        }
    }
}