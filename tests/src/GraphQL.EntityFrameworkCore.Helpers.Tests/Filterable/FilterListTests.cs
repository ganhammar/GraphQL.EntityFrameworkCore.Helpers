using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.EntityFrameworkCore.Helpers;
using GraphQL.EntityFrameworkCore.Helpers.Tests.Infrastructure;
using GraphQL.SystemTextJson;
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

            var result = await dbContext.Humans.Filter(GetContext("humans", "Leia", new[] { "name" }), dbContext.Model).ToListAsync();

            result.Count.ShouldBe(1);
        }

        [Fact]
        public async Task Should_ReturnTwo_When_FilteringOnL()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();

            var result = await dbContext.Humans.Filter(GetContext("humans", "L", new[] { "name" }), dbContext.Model).ToListAsync();

            result.Count.ShouldBe(2);
        }

        [Fact]
        public async Task Should_NotReturnItems_When_FilterHasNoMatches()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();

            var result = await dbContext.Humans.Filter(GetContext("humans", "Jar Jar Binks", new[] { "name" }), dbContext.Model).ToListAsync();

            result.ShouldBeEmpty();
        }

        [Fact]
        public async Task Should_ReturnItems_When_QueryingForEyeColor()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();

            var result = await dbContext.Humans.Filter(GetContext("humans", "Blue", new[] { "name", "eyeColor" }), dbContext.Model).ToListAsync();

            result.Count.ShouldBe(2);
        }

        [Fact]
        public async Task Should_NotReturnItems_When_NotQueryingFilterableField()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();

            var result = await dbContext.Humans.Filter(GetContext("humans", "Blue", new[] { "name" }), dbContext.Model).ToListAsync();

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
                    homePlanet = x.HomePlanet.Name.Equals(homePlanetName, StringComparison.InvariantCultureIgnoreCase) ? new
                    {
                        name = x.HomePlanet.Name,
                    } : default(object),
                    friends = x.Friends
                        .Where(y => y.HomePlanet.Name.Equals(homePlanetName, StringComparison.InvariantCultureIgnoreCase))
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
                    residents = x.Habitants
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
                        residents {{
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

        [Fact]
        public async Task Should_Return_When_FilteringOnStarSector()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();
            var sectorName = "alderaan";
            var planets = await dbContext.Planets
                .Where(x => EF.Functions.Like(x.Sector, sectorName))
                .Select(x => new
                {
                    starSector = x.Sector,
                })
                .ToListAsync();

            planets.Count.ShouldBe(1);

            var query = $@"
                query planets($filterInput: FilterInput) {{
                    planets(filter: $filterInput) {{
                        starSector
                    }}
                }}
            ";

            var inputs = $@"
                {{
                    ""filterInput"": {{
                        ""fields"": [
                            {{
                                ""target"": ""starSector"",
                                ""value"": ""{sectorName}""
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
        public async Task Should_Return_When_FilteringOnStarSectorAndAll()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();
            var sectorName = "alderaan";
            var planets = await dbContext.Planets
                .Where(x => EF.Functions.Like(x.Sector, sectorName))
                .Select(x => new
                {
                    id = x.Id,
                    starSector = x.Sector,
                })
                .ToListAsync();

            planets.Count.ShouldBe(1);

            var query = $@"
                query planets($filterInput: FilterInput) {{
                    planets(filter: $filterInput) {{
                        id
                        starSector
                    }}
                }}
            ";

            var inputs = $@"
                {{
                    ""filterInput"": {{
                        ""fields"": [
                            {{
                                ""target"": ""All"",
                                ""value"": ""xxyyzz""
                            }},
                            {{
                                ""target"": ""starSector"",
                                ""value"": ""{sectorName}""
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
        public void Should_NotReturn_When_FilteringOnStarSectorAndAllWithAndOperator()
        {
            var sectorName = "alderaan";

            var query = $@"
                query planets($filterInput: FilterInput) {{
                    planets(filter: $filterInput) {{
                        name
                        starSector
                    }}
                }}
            ";

            var inputs = $@"
                {{
                    ""filterInput"": {{
                        ""fields"": [
                            {{
                                ""target"": ""All"",
                                ""value"": ""xxyyzz"",
                                ""operator"": ""And""
                            }},
                            {{
                                ""target"": ""starSector"",
                                ""value"": ""{sectorName}""
                            }}
                        ]
                    }}
                }}
            ".ToInputs();

            var expected = @"{
                ""planets"": []
            }";

            var result = AssertQuerySuccess(query, expected, inputs);
        }

        [Fact]
        public async Task Should_ReturnOnlyLuke_When_FilteringOnStarSectorAndName()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();
            var sectorName = "arkanis";
            var humanName = "luke";
            var humans = await dbContext.Humans
                .Include(x => x.HomePlanet)
                .Where(x => EF.Functions.Like(x.HomePlanet.Sector, sectorName))
                .Where(x => EF.Functions.Like(x.Name, humanName))
                .Select(x => new
                {
                    name = x.Name,
                    homePlanet = new
                    {
                        starSector = x.HomePlanet.Sector,
                    },
                })
                .ToListAsync();

            humans.Count().ShouldBe(1);

            var query = $@"
                query humans($filterInput: FilterInput) {{
                    humans(filter: $filterInput) {{
                        name
                        homePlanet {{
                            starSector
                        }}
                    }}
                }}
            ";

            var inputs = $@"
                {{
                    ""filterInput"": {{
                        ""fields"": [
                            {{
                                ""target"": ""name"",
                                ""value"": ""{humanName}"",
                                ""operator"": ""And""
                            }},
                            {{
                                ""target"": ""homePlanet"",
                                ""fields"": [
                                    {{
                                        ""target"": ""starSector"",
                                        ""value"": ""{sectorName}"",
                                        ""operator"": ""And""
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

        [Fact]
        public async Task Should_ReturnLukeAndAnakin_When_FilteringOnStarSectorOrName()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();
            var sectorName = "arkanis";
            var humanName = "luke";
            var humans = await dbContext.Humans
                .Include(x => x.HomePlanet)
                .Where(x => EF.Functions.Like(x.HomePlanet.Sector, sectorName) || EF.Functions.Like(x.Name, humanName))
                .Select(x => new
                {
                    name = x.Name,
                    homePlanet = new
                    {
                        starSector = x.HomePlanet.Sector,
                    },
                })
                .ToListAsync();

            humans.Count().ShouldBe(2);

            var query = $@"
                query humans($filterInput: FilterInput) {{
                    humans(filter: $filterInput) {{
                        name
                        homePlanet {{
                            starSector
                        }}
                    }}
                }}
            ";

            var inputs = $@"
                {{
                    ""filterInput"": {{
                        ""fields"": [
                            {{
                                ""target"": ""name"",
                                ""value"": ""{humanName}"",
                                ""operator"": ""Or""
                            }},
                            {{
                                ""target"": ""homePlanet"",
                                ""fields"": [
                                    {{
                                        ""target"": ""starSector"",
                                        ""value"": ""{sectorName}"",
                                        ""operator"": ""Or""
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

        [Fact]
        public async Task Should_FilterFriends_When_FilteringDeep()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();
            var humanName = "luke";
            var humans = await dbContext.Humans
                .Include(x => x.Friends)
                .Where(x => EF.Functions.Like(x.Name, humanName) || x.Friends.Any(y => EF.Functions.Like(y.Name, humanName)))
                .Select(x => new
                {
                    name = x.Name,
                    friends = x.Friends
                        .Where(y => EF.Functions.Like(y.Name, humanName))
                        .Select(y => new
                        {
                            name = y.Name,
                        }),
                })
                .ToListAsync();

            humans.Count().ShouldBe(2);
            humans.First(x => x.name.Equals(humanName, StringComparison.InvariantCultureIgnoreCase)).friends.ShouldBeEmpty();
            humans.First(x => x.name.Equals(humanName, StringComparison.InvariantCultureIgnoreCase) == false).friends.Count().ShouldBe(1);

            var query = $@"
                query humans($filterInput: FilterInput) {{
                    humans(filter: $filterInput) {{
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
                        ""mode"": ""Deep"",
                        ""fields"": [
                            {{
                                ""target"": ""name"",
                                ""value"": ""{humanName}""
                            }},
                            {{
                                ""target"": ""friends"",
                                ""fields"": [
                                    {{
                                        ""target"": ""name"",
                                        ""value"": ""{humanName}""
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

        [Fact]
        public async Task Should_NotFilterFriends_When_FilteringShallow()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();
            var humanName = "luke";
            var humans = await dbContext.Humans
                .Include(x => x.Friends)
                .Where(x => EF.Functions.Like(x.Name, humanName) || x.Friends.Any(y => EF.Functions.Like(y.Name, humanName)))
                .Select(x => new
                {
                    name = x.Name,
                    friends = x.Friends
                        .Select(y => new
                        {
                            name = y.Name,
                        }),
                })
                .ToListAsync();

            humans.Count().ShouldBe(2);
            humans.First(x => x.name.Equals(humanName, StringComparison.InvariantCultureIgnoreCase)).friends.Count().ShouldBe(1);
            humans.First(x => x.name.Equals(humanName, StringComparison.InvariantCultureIgnoreCase) == false).friends.Count().ShouldBe(1);

            var query = $@"
                query humans($filterInput: FilterInput) {{
                    humans(filter: $filterInput) {{
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
                        ""mode"": ""Shallow"",
                        ""fields"": [
                            {{
                                ""target"": ""name"",
                                ""value"": ""{humanName}""
                            }},
                            {{
                                ""target"": ""friends"",
                                ""fields"": [
                                    {{
                                        ""target"": ""name"",
                                        ""value"": ""{humanName}""
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

        [Fact]
        public async Task Should_FilterDataLoadedProperties_When_FilteringDeepWithDifferentNameDeep()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();
            var humans = await dbContext.Humans
                .Include(x => x.Friends)
                    .ThenInclude(x => x.HomePlanet)
                .Where(x => EF.Functions.Like(x.Name, "luke") || // All three filters applies
                    x.Friends.Any(y => EF.Functions.Like(y.Name, "leia")) ||
                    x.Friends.Any(y => EF.Functions.Like(y.HomePlanet.Name, "tatooine")))
                .Select(x => new
                {
                    name = x.Name,
                    friends = x.Friends
                        .Where(y => EF.Functions.Like(y.Name, "leia") || // The two child filters applies
                            EF.Functions.Like(y.HomePlanet.Name, "tatooine"))
                        .Select(y => new
                        {
                            name = y.Name,
                            homePlanet = EF.Functions.Like(y.HomePlanet.Name, "tatooine") ? new // Only the last filter applies
                            {
                                name = y.HomePlanet.Name,
                            } : default(object),
                        }),
                })
                .ToListAsync();

            var query = $@"
                query humans($filterInput: FilterInput) {{
                    humans(filter: $filterInput) {{
                        name
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
                        ""mode"": ""Deep"",
                        ""fields"": [
                            {{
                                ""target"": ""name"",
                                ""value"": ""luke""
                            }},
                            {{
                                ""target"": ""friends"",
                                ""fields"": [
                                    {{
                                        ""target"": ""name"",
                                        ""value"": ""leia""
                                    }},
                                    {{
                                        ""target"": ""homePlanet"",
                                        ""fields"": [
                                            {{
                                                ""target"": ""name"",
                                                ""value"": ""tatooine""
                                            }}
                                        ]
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

        [Fact]
        public async Task Should_BeAbleToFilter_When_UsingEqualOperator()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();
            var humanName = "luke";
            var humans = await dbContext.Humans
                .Where(x => EF.Functions.Like(x.Name, humanName))
                .Select(x => new
                {
                    name = x.Name,
                })
                .ToListAsync();

            humans.Count.ShouldBe(1);

            var query = $@"
                query humans($filterInput: FilterInput) {{
                    humans(filter: $filterInput) {{
                        name
                    }}
                }}
            ";

            var inputs = $@"
                {{
                    ""filterInput"": {{
                        ""mode"": ""Deep"",
                        ""fields"": [
                            {{
                                ""target"": ""name"",
                                ""value"": ""{humanName}"",
                                ""valueOperator"": ""Equal""
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
        public async Task Should_NotReturnAny_When_FilteringOnLukWithEqual()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();
            var humanName = "luk";
            var humans = await dbContext.Humans
                .Where(x => EF.Functions.Like(x.Name, humanName))
                .Select(x => new
                {
                    name = x.Name,
                })
                .ToListAsync();

            humans.ShouldBeEmpty();

            var query = $@"
                query humans($filterInput: FilterInput) {{
                    humans(filter: $filterInput) {{
                        name
                    }}
                }}
            ";

            var inputs = $@"
                {{
                    ""filterInput"": {{
                        ""mode"": ""Deep"",
                        ""fields"": [
                            {{
                                ""target"": ""name"",
                                ""value"": ""{humanName}"",
                                ""valueOperator"": ""Equal""
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
        public async Task Should_NotReturnLuke_When_FilteringOnNotEqualLuke()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();
            var humanName = "luke";
            var humans = await dbContext.Humans
                .Where(x => EF.Functions.Like(x.Name, humanName) == false)
                .Select(x => new
                {
                    name = x.Name,
                })
                .ToListAsync();

            humans.Count().ShouldBe(2);

            var query = $@"
                query humans($filterInput: FilterInput) {{
                    humans(filter: $filterInput) {{
                        name
                    }}
                }}
            ";

            var inputs = $@"
                {{
                    ""filterInput"": {{
                        ""mode"": ""Deep"",
                        ""fields"": [
                            {{
                                ""target"": ""name"",
                                ""value"": ""{humanName}"",
                                ""valueOperator"": ""notequal""
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
        public async Task Should_NotReturnLukeOrLeia_When_FilteringOnNotLikeL()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();
            var humanName = "l";
            var humans = await dbContext.Humans
                .Where(x => EF.Functions.Like(x.Name, $"%{humanName}%") == false)
                .Select(x => new
                {
                    name = x.Name,
                })
                .ToListAsync();

            humans.Count().ShouldBe(1);
            humans.First().name.ShouldBe("Anakin");

            var query = $@"
                query humans($filterInput: FilterInput) {{
                    humans(filter: $filterInput) {{
                        name
                    }}
                }}
            ";

            var inputs = $@"
                {{
                    ""filterInput"": {{
                        ""mode"": ""Deep"",
                        ""fields"": [
                            {{
                                ""target"": ""name"",
                                ""value"": ""{humanName}"",
                                ""valueOperator"": ""notlike""
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
        public async Task Should_NotReturnAny_When_FilteringOnEqualTatooineButNotEqualBlueEyes()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();
            var planetName = "tatooine";
            var eyeColor = "blue";
            var humans = await dbContext.Humans
                .Include(x => x.HomePlanet)
                .Where(x => EF.Functions.Like(x.HomePlanet.Name, planetName))
                .Where(x => EF.Functions.Like(x.EyeColor, eyeColor) == false)
                .Select(x => new
                {
                    name = x.Name,
                    eyeColor = x.EyeColor,
                    homePlanet = new {
                        name = x.HomePlanet.Name,
                    },
                })
                .ToListAsync();

            humans.ShouldBeEmpty();

            var query = $@"
                query humans($filterInput: FilterInput) {{
                    humans(filter: $filterInput) {{
                        name
                        eyeColor
                        homePlanet {{
                            name
                        }}
                    }}
                }}
            ";

            var inputs = $@"
                {{
                    ""filterInput"": {{
                        ""mode"": ""Deep"",
                        ""fields"": [
                            {{
                                ""target"": ""eyeColor"",
                                ""value"": ""{eyeColor}"",
                                ""valueOperator"": ""notequal"",
                                ""operator"": ""and""
                            }},
                            {{
                                ""target"": ""homePlanet"",
                                ""fields"": [
                                    {{
                                        ""target"": ""name"",
                                        ""value"": ""{planetName}"",
                                        ""valueOperator"": ""equal"",
                                        ""operator"": ""and""
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

        [Fact]
        public async Task Should_ReturnAll_When_FilteringOnBlueOrBrownEyeColorUsingEquals()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();
            var humans = await dbContext.Humans
                .Select(x => new
                {
                    name = x.Name,
                    eyeColor = x.EyeColor,
                })
                .ToListAsync();

            humans.Count().ShouldBe(3);

            var query = $@"
                query humans($filterInput: FilterInput) {{
                    humans(filter: $filterInput) {{
                        name
                        eyeColor
                    }}
                }}
            ";

            var inputs = $@"
                {{
                    ""filterInput"": {{
                        ""mode"": ""Deep"",
                        ""fields"": [
                            {{
                                ""target"": ""eyeColor"",
                                ""value"": ""blue"",
                                ""valueOperator"": ""equal"",
                                ""operator"": ""or""
                            }},
                            {{
                                ""target"": ""eyeColor"",
                                ""value"": ""brown"",
                                ""valueOperator"": ""equal"",
                                ""operator"": ""or""
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
        public async Task Should_ApplyFilterToConnection_When_FilteringDroids()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();
            var primaryFunction = "proto";
            var droids = await dbContext.Droids
                .Where(x => EF.Functions.Like(x.PrimaryFunction, $"%{primaryFunction}%"))
                .ToListAsync();

            droids.Count().ShouldBe(1);

            var query = $@"
                query droids($filterInput: FilterInput) {{
                    droids(filter: $filterInput) {{
                        edges {{
                            node {{
                                name
                                primaryFunction
                            }}
                        }}
                    }}
                }}
            ";

            var inputs = $@"
                {{
                    ""filterInput"": {{
                        ""fields"": [
                            {{
                                ""target"": ""primaryFunction"",
                                ""value"": ""{primaryFunction}""
                            }}
                        ]
                    }}
                }}
            ".ToInputs();

            var expected = new
            {
                droids = new
                {
                    edges = droids.Select(x => new
                    {
                        node = new
                        {
                            name = x.Name,
                            primaryFunction = x.PrimaryFunction,
                        },
                    }),
                },
            };

            var result = AssertQuerySuccess(query, expected, inputs);
        }

        [Fact]
        public async Task Should_ApplyFilterToDataLoadedPropertyOnConnection_When_FilteringDroids()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();
            var humanName = "luke";
            var droids = await dbContext.Droids
                .Include(x => x.Owner)
                .Where(x => EF.Functions.Like(x.Owner.Name, $"%{humanName}%"))
                .OrderBy(x => x.Id)
                .ToListAsync();

            droids.Count().ShouldBe(2);

            var query = $@"
                query droids($filterInput: FilterInput) {{
                    droids(filter: $filterInput) {{
                        edges {{
                            node {{
                                name
                                owner {{
                                    name
                                }}
                            }}
                        }}
                    }}
                }}
            ";

            var inputs = $@"
                {{
                    ""filterInput"": {{
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
                droids = new
                {
                    edges = droids.Select(x => new
                    {
                        node = new
                        {
                            name = x.Name,
                            owner = new
                            {
                                name = x.Owner.Name,
                            },
                        },
                    }),
                },
            };

            var result = AssertQuerySuccess(query, expected, inputs);
        }

        [Fact]
        public void Should_NotBeValid_When_FilterableInputsDoesHaveAnyFields()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();

            var result = new FilterableInput().Validate(GetContext());

            result.IsValid.ShouldBeFalse();
        }

        [Fact]
        public void Should_NotBeValid_When_FieldDoesntHaveTarget()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();

            var result = new FilterableInput
            {
                Fields = new List<FilterableInputField>
                {
                    new FilterableInputField
                    {
                        Target = default,
                        Value = "Something",
                    },
                },
            }.Validate(GetContext());

            result.IsValid.ShouldBeFalse();
        }

        [Fact]
        public async Task Should_ApplyFilterToFieldsInFragment_When_FilteringDroidsWithFragmentInQuery()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();
            var humanName = "luke";
            var droids = await dbContext.Droids
                .Include(x => x.Owner)
                .Where(x => EF.Functions.Like(x.Owner.Name, $"%{humanName}%"))
                .OrderBy(x => x.Id)
                .ToListAsync();

            droids.Count().ShouldBe(2);

            var query = $@"
                fragment droidParts on Droid {{
                    name
                    owner {{
                        name
                    }}
                }}
                query droids($filterInput: FilterInput) {{
                    droids(filter: $filterInput) {{
                        edges {{
                            node {{
                                ...droidParts
                            }}
                        }}
                    }}
                }}
            ";

            var inputs = $@"
                {{
                    ""filterInput"": {{
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
                droids = new
                {
                    edges = droids.Select(x => new
                    {
                        node = new
                        {
                            name = x.Name,
                            owner = new
                            {
                                name = x.Owner.Name,
                            },
                        },
                    }),
                },
            };

            var result = AssertQuerySuccess(query, expected, inputs);
        }

        [Fact]
        public async Task Should_ApplyFilterToFieldsInFragment_When_FilteringDroidsWithFragmentInQueryAndTarget()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();
            var humanName = "luke";
            var droids = await dbContext.Droids
                .Include(x => x.Owner)
                .Where(x => EF.Functions.Like(x.Owner.Name, $"%{humanName}%"))
                .OrderBy(x => x.Id)
                .ToListAsync();

            droids.Count().ShouldBe(2);

            var query = $@"
                fragment droidParts on Droid {{
                    name
                    owner {{
                        name
                    }}
                }}
                query droids($filterInput: FilterInput) {{
                    droids(filter: $filterInput) {{
                        edges {{
                            node {{
                                ...droidParts
                            }}
                        }}
                    }}
                }}
            ";

            var inputs = $@"
                {{
                    ""filterInput"": {{
                        ""fields"": [
                            {{
                                ""target"": ""owner"",
                                ""fields"": [
                                    {{
                                        ""target"": ""name"",
                                        ""value"": ""{humanName}""
                                    }}
                                ]
                            }}
                        ]
                    }}
                }}
            ".ToInputs();

            var expected = new
            {
                droids = new
                {
                    edges = droids.Select(x => new
                    {
                        node = new
                        {
                            name = x.Name,
                            owner = new
                            {
                                name = x.Owner.Name,
                            },
                        },
                    }),
                },
            };

            var result = AssertQuerySuccess(query, expected, inputs);
        }

        [Fact]
        public async Task Should_ApplyFilterToFieldsInFragment_When_FilteringHumansWithFragmentInQueryAndTarget()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();
            var eyeColor = "Blue";
            var humans = await dbContext.Humans
                .Where(x => EF.Functions.Like(x.EyeColor, $"%{eyeColor}%"))
                .OrderBy(x => x.Id)
                .ToListAsync();

            humans.Count().ShouldBe(2);

            var query = $@"
                fragment humanParts on Human {{
                    name
                    eyeColor
                }}
                query humans($filterInput: FilterInput) {{
                    humans(filter: $filterInput) {{
                        ...humanParts
                    }}
                }}
            ";

            var inputs = $@"
                {{
                    ""filterInput"": {{
                        ""fields"": [
                            {{
                                ""target"": ""eyeColor"",
                                ""value"": ""{eyeColor}""
                            }}
                        ]
                    }}
                }}
            ".ToInputs();

            var expected = new
            {
                humans = humans.Select(x => new
                {
                    name = x.Name,
                    eyeColor,
                }),
            };

            var result = AssertQuerySuccess(query, expected, inputs);
        }

        [Fact]
        public async Task Should_ApplyFilterToFieldsInFragment_When_FilteringPaginatedHumansWithFragmentInQueryAndTarget()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();
            var eyeColor = "Brown";
            var paginatedHumans = await dbContext.Humans
                .Where(x => EF.Functions.Like(x.EyeColor, $"%{eyeColor}%"))
                .OrderBy(x => x.Id)
                .ToListAsync();

            paginatedHumans.Count().ShouldBe(1);

            var query = $@"
                fragment humanParts on Human {{
                    name
                    eyeColor
                }}
                query paginatedHumans($filterInput: FilterInput) {{
                    paginatedHumans(filter: $filterInput) {{
                        edges {{
                            node {{
                                ...humanParts
                            }}
                        }}
                    }}
                }}
            ";

            var inputs = $@"
                {{
                    ""filterInput"": {{
                        ""fields"": [
                            {{
                                ""target"": ""eyeColor"",
                                ""value"": ""{eyeColor}""
                            }}
                        ]
                    }}
                }}
            ".ToInputs();

            var expected = new
            {
                paginatedHumans = new
                {
                    edges = paginatedHumans.Select(x => new
                    {
                        node = new
                        {
                            name = x.Name,
                            eyeColor,
                        }
                    }),
                },
            };

            var result = AssertQuerySuccess(query, expected, inputs);
        }

        [Fact]
        public async Task Should_ApplyFilterToFieldsInFragment_When_FragmentIsOneLevelDown()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();
            var humanName = "luke";
            var droids = await dbContext.Droids
                .Include(x => x.Owner)
                .Where(x => EF.Functions.Like(x.Owner.Name, $"%{humanName}%"))
                .OrderBy(x => x.Id)
                .ToListAsync();

            droids.Count().ShouldBe(2);

            var query = $@"
                fragment humanParts on Human {{
                    name
                }}
                query droids($filterInput: FilterInput) {{
                    droids(filter: $filterInput) {{
                        edges {{
                            node {{
                                name
                                owner {{
                                    ...humanParts
                                }}
                            }}
                        }}
                    }}
                }}
            ";

            var inputs = $@"
                {{
                    ""filterInput"": {{
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
                droids = new
                {
                    edges = droids.Select(x => new
                    {
                        node = new
                        {
                            name = x.Name,
                            owner = new
                            {
                                name = x.Owner.Name,
                            },
                        },
                    }),
                },
            };

            var result = AssertQuerySuccess(query, expected, inputs);
        }

        [Fact]
        public async Task Should_RemovesDuplicates_When_FieldIsInFragmentAndQuery()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();
            var humanName = "luke";
            var droids = await dbContext.Droids
                .Include(x => x.Owner)
                .Where(x => EF.Functions.Like(x.Owner.Name, $"%{humanName}%"))
                .OrderBy(x => x.Id)
                .ToListAsync();

            droids.Count().ShouldBe(2);

            var query = $@"
                fragment humanParts on Human {{
                    name
                    __typename
                }}
                query droids($filterInput: FilterInput) {{
                    droids(filter: $filterInput) {{
                        edges {{
                            node {{
                                name
                                owner {{
                                    ...humanParts
                                    __typename
                                }}
                            }}
                        }}
                    }}
                }}
            ";

            var inputs = $@"
                {{
                    ""filterInput"": {{
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
                droids = new
                {
                    edges = droids.Select(x => new
                    {
                        node = new
                        {
                            name = x.Name,
                            owner = new
                            {
                                name = x.Owner.Name,
                                __typename = "Human",
                            },
                        },
                    }),
                },
            };

            var result = AssertQuerySuccess(query, expected, inputs);
        }
    }
}