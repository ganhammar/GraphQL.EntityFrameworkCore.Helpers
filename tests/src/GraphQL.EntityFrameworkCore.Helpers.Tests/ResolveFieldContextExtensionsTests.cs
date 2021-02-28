using System;
using Xunit;

namespace GraphQL.EntityFrameworkCore.Helpers.Tests
{
    public class ResolveFieldContextExtensionsTests
    {
        [Fact]
        public void Should_ThrowException_When_TryingToGetServiceWhenRequestServicesIsNotSet()
        {
            var context = new ResolveFieldContext<object>();

            Assert.Throws<Exception>(() => context.GetService<ResolveFieldContextExtensionsTests>());
            Assert.Throws<Exception>(() => context.GetService(typeof(ResolveFieldContextExtensionsTests)));
        }
    }
}