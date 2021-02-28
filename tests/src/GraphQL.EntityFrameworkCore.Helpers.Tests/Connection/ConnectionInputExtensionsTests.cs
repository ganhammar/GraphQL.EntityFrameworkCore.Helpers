using System.Linq;
using GraphQL.EntityFrameworkCore.Helpers.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GraphQL.EntityFrameworkCore.Helpers.Tests.Connection
{
    public class ConnectionInputExtensionsTests : TestBase
    {
        [Fact]
        public void Should_BeValid_When_InputMatchesContract()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();
            var input = new ConnectionInput<Human>
            {
                First = 10,
            };

            Assert.True(input.Validate(dbContext.Model).IsValid);
        }

        [Fact]
        public void Should_NotBeValid_When_OrderByIsntValid()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();
            var input = new ConnectionInput<Human>
            {
                First = 10,
                OrderBy = new[] { "SomeNonExistingColumn" },
            };

            var result = input.Validate(dbContext.Model);

            Assert.False(result.IsValid);
            Assert.NotEmpty(result.Failures.Where(x => x.FieldName == "OrderBy"));
        }
    }
}