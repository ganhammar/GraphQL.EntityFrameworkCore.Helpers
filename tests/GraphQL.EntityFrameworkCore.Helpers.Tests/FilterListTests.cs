using System.Threading.Tasks;
using GraphQL.EntityFrameworkCore.Helpers.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace GraphQL.EntityFrameworkCore.Helpers
{
    public class FilterListTests : TestBase
    {
        [Fact]
        public async Task Should_FilterForSpecific_When_FilterIsOnlyOneMatch()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();

            var result = await dbContext.Humans.Filter("Leia").ToListAsync();

            result.Count.ShouldBe(1);
        }

        [Fact]
        public async Task Should_ReturnTwo_When_FilteringOnL()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();

            var result = await dbContext.Humans.Filter("L").ToListAsync();

            result.Count.ShouldBe(2);
        }

        [Fact]
        public async Task Should_NotReturnItems_When_FilterHasNoMatches()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();

            var result = await dbContext.Humans.Filter("Jar Jar Binks").ToListAsync();

            result.ShouldBeEmpty();
        }
    }
}