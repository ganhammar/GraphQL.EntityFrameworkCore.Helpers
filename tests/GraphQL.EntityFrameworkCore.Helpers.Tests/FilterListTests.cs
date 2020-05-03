using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL.EntityFrameworkCore.Helpers.Tests.Infrastructure;
using GraphQL.Language.AST;
using GraphQL.Types;
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

            var result = await dbContext.Humans.Filter(GetContext("Leia"), dbContext.Model).ToListAsync();

            result.Count.ShouldBe(1);
        }

        [Fact]
        public async Task Should_ReturnTwo_When_FilteringOnL()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();

            var result = await dbContext.Humans.Filter(GetContext("L"), dbContext.Model).ToListAsync();

            result.Count.ShouldBe(2);
        }

        [Fact]
        public async Task Should_NotReturnItems_When_FilterHasNoMatches()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();

            var result = await dbContext.Humans.Filter(GetContext("Jar Jar Binks"), dbContext.Model).ToListAsync();

            result.ShouldBeEmpty();
        }

        [Fact]
        public async Task Should_ReturnItems_When_NotQueryingForHomePlanet()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();

            var context = new ResolveFieldContext<object>();

            context.Arguments = new Dictionary<string, object>();
            context.Arguments.Add("filter", "Tatooine");

            context.SubFields = new Dictionary<string, Field>();
            context.SubFields.Add("name", new Field(new NameNode("name"), new NameNode("Name")));
            context.SubFields.Add("homePlanet", new Field(new NameNode("homePlanet"), new NameNode("HomePlanet")));

            var result = await dbContext.Humans.Filter(context, dbContext.Model).ToListAsync();

            result.Count.ShouldBe(2);
        }

        [Fact]
        public async Task Should_NotReturnItems_When_NotQueryingFilterableField()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();

            var result = await dbContext.Humans.Filter(GetContext("Tatooine"), dbContext.Model).ToListAsync();

            result.ShouldBeEmpty();
        }

        [Fact]
        public async Task Should_FilterQueryForSpecific_When_FilterIsOnlyOneMatch()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();
            var human = await dbContext.Humans.FirstOrDefaultAsync();

            human.ShouldNotBeNull();

            var query = $@"
                query humans {{
                    humans(filter: ""{human.Name}"") {{
                        id
                        name
                    }}
                }}
            ";

            var expected = new
            {
                humans = new []
                {
                    new
                    {
                        id = human.Id,
                        name = human.Name,
                    },
                },
            };

            var result = AssertQuerySuccess(query, expected);
        }

        private static IResolveFieldContext<object> GetContext(string filter)
        {
            var context = new ResolveFieldContext<object>();

            context.Arguments = new Dictionary<string, object>();
            context.Arguments.Add("filter", filter);

            context.SubFields = new Dictionary<string, Field>();
            context.SubFields.Add("name", new Field(new NameNode("name"), new NameNode("Name")));

            return context;
        }
    }
}