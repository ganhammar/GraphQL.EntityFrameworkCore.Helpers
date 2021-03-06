using System.Linq;
using System.Threading.Tasks;
using GraphQL.EntityFrameworkCore.Helpers;
using GraphQL.EntityFrameworkCore.Helpers.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace GraphQL.EntityFrameworkCore.Helpers.Tests
{
    public class SelectFromFieldsTests : TestBase
    {
        [Fact]
        public async Task Should_ReturnHumans_When_ExecutingQuery()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();
            var humans = await dbContext.Humans.ToListAsync();

            humans.ShouldNotBeNull();

            var query = @"
                query humans {
                    humans {
                        id
                        name
                    }
                }
            ";

            var expected = new
            {
                humans = humans.Select(x => new
                {
                    id = x.Id,
                    name = x.Name,
                }),
            };

            var result = AssertQuerySuccess(query, expected);
        }

        [Fact]
        public async Task Should_ReturnHumansWithFriends_When_ExecutingQuery()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();
            var humans = await dbContext.Humans.Include(x => x.Friends).ToListAsync();

            humans.ShouldNotBeNull();

            var query = @"
                query humans {
                    humans {
                        id
                        name
                        friends {
                            id
                        }
                    }
                }
            ";

            var expected = new
            {
                humans = humans.Select(x => new
                {
                    id = x.Id,
                    name = x.Name,
                    friends = x.Friends.Select(y => new
                    {
                        id = y.Id,
                    }),
                }),
            };

            var result = AssertQuerySuccess(query, expected);
        }

        [Fact]
        public async Task Should_ReturnPlanetsWithHabitants_When_FieldsHasBeenRenamed()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();
            var planets = await dbContext.Planets
                .Include(x => x.Habitants)
                .Select(x => new
                {
                    id = x.Id,
                    name = x.Name,
                    residents = x.Habitants.Select(y => new
                    {
                        id = y.Id,
                    }),
                })
                .ToListAsync();

            planets.Count().ShouldBe(2);
            planets.First().residents.ShouldNotBeEmpty();

            var query = @"
                query planets {
                    planets {
                        id
                        name
                        residents {
                            id
                        }
                    }
                }
            ";

            var expected = new
            {
                planets,
            };

            var result = AssertQuerySuccess(query, expected);
        }
        
        [Fact]
        public async Task Should_ReturnHumansWithOnlyRequestedFields_When_SelectingFromFieldContext()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();

            var resolveFieldContext = GetContext(fields: new[] { "id" });

            var result = await dbContext.Humans.SelectFromContext(resolveFieldContext, dbContext.Model).ToListAsync();

            result.ShouldNotBeNull();

            var human = result.First();

            human.Id.ShouldNotBe(default);
            human.Name.ShouldBeNullOrEmpty();
        }

        [Fact]
        public async Task Should_ReturnDroidsWithOnlyRequestedFields_When_SelectingFromFieldContext()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();

            var resolveFieldContext = GetContext(fields: new[] { "id" });

            var result = await dbContext.Droids.SelectFromContext(resolveFieldContext, dbContext.Model).ToListAsync();

            result.ShouldNotBeNull();

            var droid = result.First();

            droid.Id.ShouldNotBe(default);
            droid.Name.ShouldBeNullOrEmpty();
        }

        [Fact]
        public async Task Should_NotDoAnySpecificSelects_When_ContextIsntPassed()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();

            var result = await dbContext.Droids.SelectFromContext(default, dbContext.Model).ToListAsync();

            result.ShouldNotBeNull();

            var droid = result.First();

            droid.Id.ShouldNotBe(default);
            droid.Name.ShouldNotBe(default);
        }

        [Fact]
        public async Task Should_ReturnSelectedFields_When_UsingFragments()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();
            var planets = await dbContext.Planets
                .Include(x => x.Habitants)
                .Select(x => new
                {
                    id = x.Id,
                    name = x.Name,
                    residents = x.Habitants.Select(y => new
                    {
                        id = y.Id,
                    }),
                })
                .ToListAsync();

            planets.Count().ShouldBe(2);
            planets.First().residents.ShouldNotBeEmpty();

            var query = @"
                fragment planetParts on Planet {
                    id
                    name
                    residents {
                        id
                    }
                }
                query planets {
                    planets {
                        ...planetParts
                    }
                }
            ";

            var expected = new
            {
                planets,
            };

            var result = AssertQuerySuccess(query, expected);
        }
    }
}
