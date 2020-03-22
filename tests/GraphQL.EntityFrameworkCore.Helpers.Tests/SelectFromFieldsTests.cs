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
        public async Task Should_ReturnHumansWithOnlyRequestedFields_When_SelectingFromFieldContext()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();
            var selection = new Dictionary<string, Field>();

            selection.Add("id", new Field(new NameNode("id"), new NameNode("Id")));

            var resolveFieldContext = new ResolveFieldContext<object>();
            resolveFieldContext.SubFields = selection;

            var result = await dbContext.Humans.Select(resolveFieldContext).ToListAsync();

            result.ShouldNotBeNull();

            var human = result.First();

            human.Id.ShouldNotBeNull();
            human.Name.ShouldBeNullOrEmpty();
        }

        [Fact]
        public async Task Should_ReturnDroidsWithOnlyRequestedFieldsAndForeignKeys_When_SelectingFromFieldContext()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();
            var selection = new Dictionary<string, Field>();

            selection.Add("id", new Field(new NameNode("id"), new NameNode("Id")));

            var resolveFieldContext = new ResolveFieldContext<object>();
            resolveFieldContext.SubFields = selection;

            var result = await dbContext.Droids.Select(resolveFieldContext).ToListAsync();

            result.ShouldNotBeNull();

            var droid = result.First();

            droid.Id.ShouldNotBeNull();
            droid.Name.ShouldBeNullOrEmpty();
            droid.OwnerId.ShouldNotBeNull();
        }
    }
}
