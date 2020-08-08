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
                query = @"query pages {\n  
                    pages {\n    edges {\n      node {\n        id\n      }\n      cursor\n    }\n    
                    pageInfo {\n      hasPreviousPage\n      hasNextPage\n      startCursor\n      endCursor\n    }\n  }\n    
                    totalCount\n}",
                operationName = "pages",
            });
            var operation = new StringContent(payload, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("/graphql", operation);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);

            var content = await response.Content.ReadAsStringAsync();

            content.ShouldNotBeEmpty();
        }

        [Fact]
        public async Task Should_ReturnUsers_When_QueryingForUsers()
        {
            var client = await GetTestClient();

            var payload = JsonSerializer.Serialize(new {
                query = @"query users {\n  
                    users {\n    edges {\n      node {\n        id\n      }\n      cursor\n    }\n    
                    pageInfo {\n      hasPreviousPage\n      hasNextPage\n      startCursor\n      endCursor\n    }\n  }\n    
                    totalCount\n}",
                operationName = "users",
            });
            var operation = new StringContent(payload, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("/graphql", operation);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);

            var content = await response.Content.ReadAsStringAsync();

            content.ShouldNotBeEmpty();
        }

        [Fact]
        public async Task Should_ReturnTags_When_QueryingForTags()
        {
            var client = await GetTestClient();

            var payload = JsonSerializer.Serialize(new {
                query = @"query tags {\n  
                    tags {\n    edges {\n      node {\n        id\n      }\n      cursor\n    }\n    
                    pageInfo {\n      hasPreviousPage\n      hasNextPage\n      startCursor\n      endCursor\n    }\n  }\n    
                    totalCount\n}",
                operationName = "tags",
            });
            var operation = new StringContent(payload, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("/graphql", operation);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);

            var content = await response.Content.ReadAsStringAsync();

            content.ShouldNotBeEmpty();
        }

        [Fact]
        public async Task Should_ReturnUsersWithPagesAndTags_When_QueryingNestedProperties()
        {
            var client = await GetTestClient();

            var payload = JsonSerializer.Serialize(new {
                query = @"query users {\n  
                    users {\n    edges {\n      node {\n        id\n        
                        pages {\n          id\n          
                            tags {\n            id\n          }\n        }\n      }\n    }\n  }\n}",
                operationName = "users",
            });
            var operation = new StringContent(payload, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("/graphql", operation);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);

            var content = await response.Content.ReadAsStringAsync();

            content.ShouldNotBeEmpty();
        }
    }
}