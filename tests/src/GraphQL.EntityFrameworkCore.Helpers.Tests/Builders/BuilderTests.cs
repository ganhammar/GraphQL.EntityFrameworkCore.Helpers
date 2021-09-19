using System;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.EntityFrameworkCore.Helpers.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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

        [Fact]
        public async Task Should_HumanWithHomeplanet_When_IncludingThemInQuery()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();
            var humans = await dbContext.Humans
                .Include(x => x.Friends)
                    .ThenInclude(x => x.HomePlanet)
                .Select(x => new
                {
                    id = x.Id,
                    name = x.Name,
                    homePlanet = new {
                        id = x.HomePlanet.Id,
                        name = x.HomePlanet.Name,
                    },
                })
                .ToListAsync();

            humans.ShouldNotBeEmpty();

            var query = $@"
                query humans {{
                    humans {{
                        id
                        name
                        homePlanet {{
                            id
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
        public async Task Should_ReturnMyDroids_When_RequestingThem()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();
            var humanId = StarWarsData.LukeId;
            var myDroids = await dbContext.Droids
                .Where(x => x.OwnerId == humanId)
                .Select(x => new
                {
                    id = x.Id,
                    name = x.Name,
                })
                .ToListAsync();

            myDroids.ShouldNotBeEmpty();

            var query = $@"
                query myDroids {{
                    myDroids(humanId: ""{humanId}"") {{
                        id
                        name
                    }}
                }}
            ";

            var expected = new
            {
                myDroids,
            };

            var result = AssertQuerySuccess(query, expected);
        }

        [Fact]
        public async Task Should_ReturnDroid_When_Requesting()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();
            var droid = await dbContext.Droids
                .Select(x => new
                {
                    id = x.Id,
                    name = x.Name,
                })
                .FirstAsync();

            var query = $@"
                query droid {{
                    droid(id: ""{droid.id}"") {{
                        id
                        name
                    }}
                }}
            ";

            var expected = new
            {
                droid,
            };

            var result = AssertQuerySuccess(query, expected);
        }

        [Fact]
        public void Should_NotReturnPlanet_When_ItDoesntExist()
        {
            var query = $@"
                query planet {{
                    planet(id: ""{Guid.NewGuid()}"") {{
                        id
                        name
                    }}
                }}
            ";

            var expected = new
            {
                planet = (object)null,
            };

            var result = AssertQuerySuccess(query, expected);
        }

        [Fact]
        public void Should_ReturnErrors_When_RequestingDroidThatDoesntExist()
        {
            var query = $@"
                query droid {{
                    droid(id: ""{Guid.NewGuid()}"") {{
                        id
                        name
                    }}
                }}
            ";

            AssertQueryWithErrors(query, expectedErrorCount: 1);
        }

        [Fact]
        public async Task Should_ResolveGalaxiesFromOtherDbContext_When_ItsRegisteredToUseOtherDbContext()
        {
            var dbContext = ServiceProvider.GetRequiredService<DifferentTestDbContext>();
            var galaxies = await dbContext.Galaxies
                .Select(x => new
                {
                    id = x.Id,
                    name = x.Name,
                })
                .ToListAsync();

            var query = $@"
                query galaxies {{
                    galaxies {{
                        id
                        name
                    }}
                }}
            ";

            var expected = new
            {
                galaxies,
            };

            var result = AssertQuerySuccess(query, expected);
        }

        [Fact]
        public async Task Should_ResolveHumanForceAlignmentWithForce_When_Requesting()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();
            var humanForceAlignments = await dbContext.HumanForceAlignments
                .Select(x => new
                {
                    force = new
                    {
                        type = x.Force.Type,
                    },
                })
                .ToListAsync();

            var query = @"
                query humanForceAlignments {
                    humanForceAlignments {
                        force {
                            type
                        }
                    }
                }
            ";

            var expected = new
            {
                humanForceAlignments,
            };

            var result = AssertQuerySuccess(query, expected);
        }

        [Fact]
        public void Should_ThrowException_When_RequestServicesIsNotSetOnProperty()
        {
            var query = @"
                query humans {
                    humans {
                        id
                        homePlanet {
                            id
                        }
                    }
                }
            ";

            var runResult = Executer.ExecuteAsync(x =>
            {
                x.Schema = Schema;
                x.Query = query;
            }).GetAwaiter().GetResult();

            var expectedError = "ExecutionOptions.RequestServices is not defined (passed to ExecuteAsync), use GraphQL Server 4.0 and on";

            Assert.NotEmpty(runResult.Errors);
            Assert.Equal(runResult.Errors.First().InnerException.Message, expectedError);
        }

        [Fact]
        public void Should_ThrowException_When_RequestServicesIsNotSetOnCollection()
        {
            var query = @"
                query humans {
                    humans {
                        id
                        friends {
                            id
                        }
                    }
                }
            ";

            var runResult = Executer.ExecuteAsync(x =>
            {
                x.Schema = Schema;
                x.Query = query;
            }).GetAwaiter().GetResult();

            var expectedError = "ExecutionOptions.RequestServices is not defined (passed to ExecuteAsync), use GraphQL Server 4.0 and on";

            Assert.NotEmpty(runResult.Errors);
            Assert.Equal(runResult.Errors.First().InnerException.Message, expectedError);
        }

        [Fact]
        public void Should_NotReturnResult_When_ValidationLogicFails()
        {
            var query = @"
                query invalidDroids {
                    invalidDroids {
                        id
                    }
                }
            ";

            AssertQueryWithErrors(query, expectedErrorCount: 1);
        }

        [Fact]
        public async void Should_ReturnHumansInOrderOfEyeColor_When_UsingFieldWithApply()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();
            var humansOrderedByEyeColor = await dbContext.Humans
                .OrderBy(x => x.EyeColor)
                .Select(x => new
                {
                    name = x.Name,
                })
                .ToListAsync();

            humansOrderedByEyeColor.ShouldNotBeEmpty();

            var query = $@"
                query humansOrderedByEyeColor {{
                    humansOrderedByEyeColor {{
                        name
                    }}
                }}
            ";

            var expected = new
            {
                humansOrderedByEyeColor,
            };

            var result = AssertQuerySuccess(query, expected);
        }
    }
}