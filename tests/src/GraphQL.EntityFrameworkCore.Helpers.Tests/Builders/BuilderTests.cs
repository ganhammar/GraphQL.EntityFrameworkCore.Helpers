using System.Linq;
using System.Threading.Tasks;
using GraphQL.EntityFrameworkCore.Helpers.Tests.Infrastructure;
using GraphQL.Utilities;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace GraphQL.EntityFrameworkCore.Helpers.Tests.Builders
{
    public class BuilderTests : TestBase
    {
        [Fact]
        public async Task Should_ReturnResidents_When_IncludingThemInQuery()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();
            var planets = await dbContext.Planets
                .Include(x => x.Habitants)
                .Select(x => new
                {
                    id = x.Id,
                    residents = x.Habitants.Select(y => new
                    {
                        id = y.Id,
                    }),
                })
                .ToListAsync();

            planets.ShouldNotBeEmpty();

            var query = $@"
                query planets {{
                    planets {{
                        id
                        residents {{
                            id
                        }}
                    }}
                }}
            ";

            var expected = new
            {
                planets,
            };

            var result = AssertQuerySuccess(query, expected);
        }

        [Fact]
        public async Task Should_ReturnFriends_When_IncludingThemInQuery()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();
            var humans = await dbContext.Humans
                .Include(x => x.Friends)
                .Select(x => new
                {
                    id = x.Id,
                    friends = x.Friends.Select(y => new
                    {
                        id = y.Id,
                    }),
                })
                .ToListAsync();

            humans.ShouldNotBeEmpty();

            var query = $@"
                query humans {{
                    humans {{
                        id
                        friends {{
                            id
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
        public async Task Should_ReturnFriendsWithHomePlanet_When_IncludingThemInQuery()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();
            var humans = await dbContext.Humans
                .Include(x => x.Friends)
                    .ThenInclude(x => x.HomePlanet)
                .Select(x => new
                {
                    id = x.Id,
                    name = x.Name,
                    friends = x.Friends.Select(y => new
                    {
                        id = y.Id,
                        name = y.Name,
                        homePlanet = new {
                            id = y.HomePlanet.Id,
                            name = y.HomePlanet.Name,
                        },
                    }),
                })
                .ToListAsync();

            humans.ShouldNotBeEmpty();

            var query = $@"
                query humans {{
                    humans {{
                        id
                        name
                        friends {{
                            id
                            name
                            homePlanet {{
                                id
                                name
                            }}
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
    }
}