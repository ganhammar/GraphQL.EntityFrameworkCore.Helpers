using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.EntityFrameworkCore.Helpers.Filterable;
using GraphQL.EntityFrameworkCore.Helpers.Tests.Infrastructure;
using GraphQL.Language.AST;
using GraphQL.SystemTextJson;
using GraphQL.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace GraphQL.EntityFrameworkCore.Helpers.Tests.Filterable
{
    public class FilterListTests : TestBase
    {
        [Fact]
        public async Task Should_FilterForSpecific_When_FilterIsOnlyOneMatch()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();

            var result = await dbContext.Humans.Filter(GetContext("Leia", new[] { "name" }), dbContext.Model).ToListAsync();

            result.Count.ShouldBe(1);
        }

        [Fact]
        public async Task Should_ReturnTwo_When_FilteringOnL()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();

            var result = await dbContext.Humans.Filter(GetContext("L", new[] { "name" }), dbContext.Model).ToListAsync();

            result.Count.ShouldBe(2);
        }

        [Fact]
        public async Task Should_NotReturnItems_When_FilterHasNoMatches()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();

            var result = await dbContext.Humans.Filter(GetContext("Jar Jar Binks", new[] { "name" }), dbContext.Model).ToListAsync();

            result.ShouldBeEmpty();
        }

        [Fact]
        public async Task Should_ReturnItems_When_QueryingForEyeColor()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();

            var result = await dbContext.Humans.Filter(GetContext("Blue", new[] { "name", "eyeColor" }), dbContext.Model).ToListAsync();

            result.Count.ShouldBe(2);
        }

        [Fact]
        public async Task Should_NotReturnItems_When_NotQueryingFilterableField()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();

            var result = await dbContext.Humans.Filter(GetContext("Blue", new[] { "name" }), dbContext.Model).ToListAsync();

            result.ShouldBeEmpty();
        }

        [Fact]
        public async Task Should_FilterQueryForSpecific_When_FilterIsOnlyOneMatch()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();
            var human = await dbContext.Humans.FirstOrDefaultAsync();

            human.ShouldNotBeNull();

            var query = $@"
                query humans($filterInput: FilterInput) {{
                    humans(filter: $filterInput) {{
                        id
                        name
                    }}
                }}
            ";

            var inputs = $@"
                {{
                    ""filterInput"": {{
                        ""fields"": [
                            {{
                                ""value"": ""{human.Name}""
                            }}
                        ]
                    }}
                }}
            ".ToInputs();

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

            var result = AssertQuerySuccess(query, expected, inputs);
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
                query humans($filterInput: FilterInput) {{
                    humans(filter: $filterInput) {{
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

            var inputs = $@"
                {{
                    ""filterInput"": {{
                        ""fields"": [
                            {{
                                ""value"": ""{homePlanetName}""
                            }}
                        ]
                    }}
                }}
            ".ToInputs();

            var expected = new
            {
                humans,
            };

            var result = AssertQuerySuccess(query, expected, inputs);
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
                query humans($filterInput: FilterInput) {{
                    humans(filter: $filterInput) {{
                        id
                        name
                        friends {{
                            name
                        }}
                    }}
                }}
            ";

            var inputs = $@"
                {{
                    ""filterInput"": {{
                        ""fields"": [
                            {{
                                ""value"": ""{name}""
                            }}
                        ]
                    }}
                }}
            ".ToInputs();

            var expected = new
            {
                humans,
            };

            var result = AssertQuerySuccess(query, expected, inputs);
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
                query humans($filterInput: FilterInput) {{
                    humans(filter: $filterInput) {{
                        id
                        name
                        friends {{
                            name
                        }}
                    }}
                }}
            ";

            var inputs = $@"
                {{
                    ""filterInput"": {{
                        ""mode"": ""deep"",
                        ""fields"": [
                            {{
                                ""value"": ""{name}""
                            }}
                        ]
                    }}
                }}
            ".ToInputs();

            var expected = new
            {
                humans,
            };

            var result = AssertQuerySuccess(query, expected, inputs);
        }

        [Fact]
        public async Task Should_ReturnLukeAndLeia_When_FilteringOnAlderaanWithFriends()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();
            var homePlanetName = "alderaan";
            var humans = await dbContext.Humans
                .Include(x => x.HomePlanet)
                .Include(x => x.Friends)
                    .ThenInclude(x => x.HomePlanet)
                .Where(x => EF.Functions.Like(x.HomePlanet.Name, homePlanetName) ||
                    x.Friends.Any(y => EF.Functions.Like(y.HomePlanet.Name, homePlanetName)))
                .Select(x => new
                {
                    id = x.Id,
                    name = x.Name,
                    homePlanet = new
                    {
                        name = x.HomePlanet.Name,
                    },
                    friends = x.Friends
                        .Where(y => y.HomePlanet.Name.ToLower() == homePlanetName)
                        .Select(y => new
                        {
                            name = y.Name,
                            homePlanet = new
                            {
                                name = y.HomePlanet.Name,
                            },
                        }),
                })
                .ToListAsync();

            humans.Count.ShouldBe(2);

            var query = $@"
                query humans($filterInput: FilterInput) {{
                    humans(filter: $filterInput) {{
                        id
                        name
                        homePlanet {{
                            name
                        }}
                        friends {{
                            name
                            homePlanet {{
                                name
                            }}
                        }}
                    }}
                }}
            ";

            var inputs = $@"
                {{
                    ""filterInput"": {{
                        ""mode"": ""deep"",
                        ""fields"": [
                            {{
                                ""value"": ""{homePlanetName}""
                            }}
                        ]
                    }}
                }}
            ".ToInputs();

            var expected = new
            {
                humans,
            };

            var result = AssertQuerySuccess(query, expected, inputs);
        }

        [Fact]
        public async Task Should_ReturnLukeAndLeia_When_FilteringOnLeiaWithHabitantsAndFriends()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();
            var humanName = "leia";
            var planets = await dbContext.Planets
                .Include(x => x.Habitants)
                    .ThenInclude(x => x.Friends)
                .Where(x => x.Habitants.Any(y => EF.Functions.Like(y.Name, humanName)) ||
                    x.Habitants.Any(y => y.Friends.Any(z => EF.Functions.Like(z.Name, humanName))))
                .Select(x => new
                {
                    id = x.Id,
                    name = x.Name,
                    habitants = x.Habitants
                        .Where(y => EF.Functions.Like(y.Name, humanName) ||
                            y.Friends.Any(z => EF.Functions.Like(z.Name, humanName)))
                        .Select(y => new
                        {
                            name = y.Name,
                            friends = y.Friends
                                .Where(z => EF.Functions.Like(z.Name, humanName))
                                .Select(z => new
                                {
                                    name = z.Name,
                                }),
                        }),
                })
                .ToListAsync();

            planets.Count.ShouldBe(2);

            var query = $@"
                query planets($filterInput: FilterInput) {{
                    planets(filter: $filterInput) {{
                        id
                        name
                        habitants {{
                            name
                            friends {{
                                name
                            }}
                        }}
                    }}
                }}
            ";

            var inputs = $@"
                {{
                    ""filterInput"": {{
                        ""mode"": ""deep"",
                        ""fields"": [
                            {{
                                ""value"": ""{humanName}""
                            }}
                        ]
                    }}
                }}
            ".ToInputs();

            var expected = new
            {
                planets,
            };

            var result = AssertQuerySuccess(query, expected, inputs);
        }

        [Fact]
        public async Task Should_ReturnLeia_When_FilteringOnNameOnly()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();
            var humanName = "leia";
            var humans = await dbContext.Humans
                .Include(x => x.HomePlanet)
                .Include(x => x.Friends)
                .Where(x => EF.Functions.Like(x.Name, humanName))
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

            humans.Count.ShouldBe(1);

            var query = $@"
                query humans($filterInput: FilterInput) {{
                    humans(filter: $filterInput) {{
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

            var inputs = $@"
                {{
                    ""filterInput"": {{
                        ""mode"": ""shallow"",
                        ""fields"": [
                            {{
                                ""target"": ""name"",
                                ""value"": ""{humanName}""
                            }}
                        ]
                    }}
                }}
            ".ToInputs();

            var expected = new
            {
                humans,
            };

            var result = AssertQuerySuccess(query, expected, inputs);
        }

        [Fact]
        public async Task Should_Leia_When_FilteringOnHomePlanetNameOnly()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();
            var homePlanetName = "alderaan";
            var humans = await dbContext.Humans
                .Include(x => x.HomePlanet)
                .Where(x => EF.Functions.Like(x.HomePlanet.Name, homePlanetName))
                .Select(x => new
                {
                    id = x.Id,
                    name = x.Name,
                    homePlanet = new
                    {
                        name = x.HomePlanet.Name,
                    },
                })
                .ToListAsync();

            humans.Count.ShouldBe(1);

            var query = $@"
                query humans($filterInput: FilterInput) {{
                    humans(filter: $filterInput) {{
                        id
                        name
                        homePlanet {{
                            name
                        }}
                    }}
                }}
            ";

            var inputs = $@"
                {{
                    ""filterInput"": {{
                        ""mode"": ""shallow"",
                        ""fields"": [
                            {{
                                ""target"": ""homePlanet"",
                                ""fields"": [
                                    {{
                                        ""target"": ""name"",
                                        ""value"": ""{homePlanetName}""
                                    }}
                                ]
                            }}
                        ]
                    }}
                }}
            ".ToInputs();

            var expected = new
            {
                humans,
            };

            var result = AssertQuerySuccess(query, expected, inputs);
        }

        private static IResolveFieldContext<object> GetContext(string filter, string[] fields)
        {
            var context = new ResolveFieldContext<object>();

            var fieldsInput = new Dictionary<string, object>();
            fieldsInput.Add("value", filter);

            var filterInput = new Dictionary<string, object>();
            filterInput.Add("fields", new List<Dictionary<string, object>> { fieldsInput });

            context.Variables = new Variables();
            context.Variables.Add(new Variable
            {
                Name = "filter",
                Value = filterInput,
            });

            context.SubFields = new Dictionary<string, Field>();

            foreach(var fieldName in fields)
            {
                context.SubFields.Add(fieldName, new Field(new NameNode(fieldName), new NameNode(fieldName)));
            }

            context.FieldName = "humans";

            var field = new Field(new NameNode(context.FieldName), new NameNode(context.FieldName));
            field.SelectionSet = new SelectionSet();

            foreach(var fieldName in fields)
            {
                field.SelectionSet.Add(new Field(new NameNode(fieldName), new NameNode(fieldName)));
            }

            field.Arguments = new Arguments();
            field.Arguments.Add(new Argument(new NameNode("filter"))
            {
                Value = new VariableReference(new NameNode("filter")),
            });

            context.Operation = new Operation(new NameNode(context.FieldName));
            context.Operation.SelectionSet = new SelectionSet();
            context.Operation.SelectionSet.Add(field);

            return context;
        }
    }
}