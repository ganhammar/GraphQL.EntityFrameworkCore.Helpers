using System.Collections.Generic;
using System.Linq;
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
        public async Task Should_ReturnItems_When_QueryingForEyeColor()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();

            var context = new ResolveFieldContext<object>();

            context.Arguments = new Dictionary<string, object>();
            context.Arguments.Add("filter", "Blue");

            context.SubFields = new Dictionary<string, Field>();
            context.SubFields.Add("name", new Field(new NameNode("name"), new NameNode("Name")));
            context.SubFields.Add("eyeColor", new Field(new NameNode("eyeColor"), new NameNode("EyeColor")));

            var result = await dbContext.Humans.Filter(context, dbContext.Model).ToListAsync();

            result.Count.ShouldBe(2);
        }

        [Fact]
        public async Task Should_NotReturnItems_When_NotQueryingFilterableField()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();

            var result = await dbContext.Humans.Filter(GetContext("Blue"), dbContext.Model).ToListAsync();

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

        [Fact]
        public async Task Should_ReturnItems_When_FilteringOnDataLoadedProperty()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();
            var homePlanetName = "tatooine";
            var humans = await dbContext.Humans
                .Include(x => x.HomePlanet)
                .Include(x => x.Friends)
                .Where(x => EF.Functions.Like(x.HomePlanet.Name, homePlanetName))
                .Select(x => new
                {
                    id = x.Id,
                    name = x.Name,
                    homePlanet = new
                    {
                        name = x.HomePlanet.Name,
                    },
                    friends = x.Friends.Select(y => new
                    {
                        name = y.Name,
                    }),
                })
                .ToListAsync();

            humans.Count.ShouldBe(2);

            var query = $@"
                query humans {{
                    humans(filter: ""{homePlanetName}"") {{
                        id
                        name
                        homePlanet {{
                            name
                        }}
                        friends {{
                            name
                        }}
                    }}
                }}
            ";

            var expected = new
            {
                humans,
            };

            var result = AssertQuerySuccess(query, expected);
        }

        [Fact]
        public async Task Should_ReturnLukeAndLeia_When_FilteringOnLeiaWithFriends()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();
            var name = "leia";
            var humans = await dbContext.Humans
                .Include(x => x.Friends)
                .Where(x => EF.Functions.Like(x.Name, name) || x.Friends.Any(y => EF.Functions.Like(y.Name, name)))
                .Select(x => new
                {
                    id = x.Id,
                    name = x.Name,
                    friends = x.Friends.Select(y => new
                    {
                        name = y.Name,
                    }),
                })
                .ToListAsync();

            humans.Count.ShouldBe(2);

            var query = $@"
                query humans {{
                    humans(filter: ""{name}"") {{
                        id
                        name
                        friends {{
                            name
                        }}
                    }}
                }}
            ";

            var expected = new
            {
                humans,
            };

            var result = AssertQuerySuccess(query, expected);
        }

        [Fact]
        public async Task Should_ReturnVader_When_FilteringOnAnakinWithFriends()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();
            var name = "anakin";
            var humans = await dbContext.Humans
                .Include(x => x.Friends)
                .Where(x => EF.Functions.Like(x.Name, name) || x.Friends.Any(y => EF.Functions.Like(y.Name, name)))
                .Select(x => new
                {
                    id = x.Id,
                    name = x.Name,
                    friends = x.Friends.Select(y => new
                    {
                        name = y.Name,
                    }),
                })
                .ToListAsync();

            humans.Count.ShouldBe(1);

            var query = $@"
                query humans {{
                    humans(filter: ""{name}"") {{
                        id
                        name
                        friends {{
                            name
                        }}
                    }}
                }}
            ";

            var expected = new
            {
                humans,
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