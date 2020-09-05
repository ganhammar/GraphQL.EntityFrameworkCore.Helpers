using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace HeadlessCms.Tests
{
    public class GraphQLTests : TestBase
    {
        [Fact]
        public async Task Should_ReturnIntrospection_When_QueryingIntrospection()
        {
            var client = await GetTestClient();

            var payload = JsonSerializer.Serialize(new {
                query = "query schema {\n  __schema {\n    types {\n      name\n    }\n  } \n}",
                operationName = "schema",
            });
            var operation = new StringContent(payload, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("/graphql", operation);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);

            var content = await response.Content.ReadAsStringAsync();

            content.ShouldNotBeEmpty();
        }

        [Fact]
        public async Task Should_ReturnPages_When_QueryingForPages()
        {
            var client = await GetTestClient();

            var payload = JsonSerializer.Serialize(new {
                query = "query pages {\n  pages {\n    edges {\n      node {\n        id\n      }\n    }\n    pageInfo {\n      hasPreviousPage\n      hasNextPage\n    }\n    totalCount\n  }}",
                operationName = "pages",
            });

            var expected = new
            {
                pages = new
                {
                    edges = new[]
                    {
                        new
                        {
                            node = new
                            {
                                id = 1,
                            },
                        },
                    },
                    pageInfo = new
                    {
                        hasPreviousPage = false,
                        hasNextPage = false,
                    },
                    totalCount = 1,
                },
            };

            await AssertExpected(payload, expected);
        }

        [Fact]
        public async Task Should_ReturnUsers_When_QueryingForUsers()
        {
            var client = await GetTestClient();

            var payload = JsonSerializer.Serialize(new {
                query = "query users {\n  users {\n    edges {\n      node {\n        id\n      }\n    }\n    pageInfo {\n      hasPreviousPage\n      hasNextPage\n    }\n    totalCount\n  }}",
                operationName = "users",
            });

            var expected = new
            {
                users = new
                {
                    edges = new[]
                    {
                        new {
                            node = new
                            {
                                id = 1,
                            },
                        },
                    },
                    pageInfo = new
                    {
                        hasPreviousPage = false,
                        hasNextPage = false,
                    },
                    totalCount = 1,
                },
            };

            await AssertExpected(payload, expected);
        }

        [Fact]
        public async Task Should_ReturnTags_When_QueryingForTags()
        {
            var client = await GetTestClient();

            var payload = JsonSerializer.Serialize(new {
                query = "query tags {\n  tags {\n    edges {\n      node {\n        id\n      }\n    }\n    pageInfo {\n      hasPreviousPage\n      hasNextPage\n    }\n    totalCount\n  }}",
                operationName = "tags",
            });

            var expected = new
            {
                tags = new
                {
                    edges = new[]
                    {
                        new
                        {
                            node = new
                            {
                                id = 1,
                            },
                        },
                        new
                        {
                            node = new
                            {
                                id = 2,
                            },
                        },
                        new
                        {
                            node = new
                            {
                                id = 3,
                            },
                        },
                    },
                    pageInfo = new
                    {
                        hasPreviousPage = false,
                        hasNextPage = false,
                    },
                    totalCount = 3,
                },
            };

            await AssertExpected(payload, expected);
        }

        [Fact]
        public async Task Should_ReturnUsersWithPagesAndTags_When_QueryingNestedProperties()
        {
            var payload = JsonSerializer.Serialize(new {
                query = "query users {\n  users {\n    edges {\n      node {\n        id\n        pages {\n          id\n         tags {\n              id\n              }\n        }\n      }\n    }\n  }\n}",
                operationName = "users",
            });

            var expected = new
            {
                users = new
                {
                    edges = new[]
                    {
                        new
                        {
                            node = new
                            {
                                id = 1,
                                pages = new []
                                {
                                    new {
                                        id = 1,
                                        tags = new[]
                                        {
                                            new
                                            {
                                                id = 1,
                                            },
                                            new
                                            {
                                                id = 2,
                                            },
                                            new
                                            {
                                                id = 3,
                                            },
                                        },
                                    },
                                },
                            },
                        },
                    },
                },
            };

            await AssertExpected(payload, expected);
        }

        [Fact]
        public async Task Should_ReturnPagesWithSpecificTags_When_FilteringDeep()
        {
            var payload = JsonSerializer.Serialize(new
            {
                query = "query pages($filter: FilterInput) {\n  pages(filter: $filter) {\n    edges {\n      node {\n        id\n        tags {\n          value\n        }\n      }\n    }\n  }}",
                variables = new
                {
                    filter = new
                    {
                        mode = "deep",
                        fields = new[]
                        {
                            new
                            {
                                target = "tags",
                                fields = new []
                                {
                                    new
                                    {
                                        target = "value",
                                        value = "lorem"
                                    },
                                    new
                                    {
                                        target = "value",
                                        value = "ipsum"
                                    },
                                },
                            },
                        },
                    },
                },
                operationName = "pages"
            });

            var expected = new
            {
                pages = new {
                    edges = new[]
                    {
                        new
                        {
                            node = new
                            {
                                id = 1,
                                tags = new[]
                                {
                                    new
                                    {
                                        value = "Lorem",
                                    },
                                    new
                                    {
                                        value = "Ipsum",
                                    },
                                },
                            },
                        },
                    },
                },
            };

            await AssertExpected(payload, expected);
        }

        [Fact]
        public async Task Should_ReturnPagesWithEditor_When_Requesting()
        {
            var payload = JsonSerializer.Serialize(new
            {
                query = "query pages {\n  pages {\n    edges {\n      node {\n        id\n        editor {\n          id\n        }\n      }\n    }\n  }}",
                operationName = "pages"
            });

            var expected = new
            {
                pages = new {
                    edges = new[]
                    {
                        new
                        {
                            node = new
                            {
                                id = 1,
                                editor = new
                                {
                                    id = 1,
                                },
                            },
                        },
                    },
                },
            };

            await AssertExpected(payload, expected);
        }

        private async Task AssertExpected(string payload, object expected)
        {
            var client = await GetTestClient();
            var operation = new StringContent(payload, Encoding.UTF8, "application/json");
            var response = await client.PostAsync("/graphql", operation);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);

            var content = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            var data = content.RootElement.GetProperty("data");

            data.ShouldNotBeNull();
            data.ToString().Equals(
                JsonDocument.Parse(JsonSerializer.Serialize(expected)).RootElement.ToString()).ShouldBeTrue();
        }
    }
}