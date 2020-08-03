using GraphQL.EntityFrameworkCore.Helpers.Tests.Infrastructure;
using GraphQL.SystemTextJson;
using Xunit;

namespace GraphQL.EntityFrameworkCore.Helpers.Tests.Filterable
{
    public class FilterableInputExtensionsTest : TestBase
    {
        [Fact]
        public void Should_BeValid_When_SimpleInputIsValid()
        {
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
                        ""fields"": [
                            {{
                                ""value"": ""test""
                            }}
                        ]
                    }}
                }}
            ".ToInputs();

            var result = AssertQueryWithErrors(query, inputs: inputs, expectedErrorCount: 0);
        }

        [Fact]
        public void Should_BeValid_When_SpecifingTargetField()
        {
            var query = $@"
                query getHumans($filterInput: FilterInput) {{
                    humans(filter: $filterInput) {{
                        name
                    }}
                }}
            ";

            var inputs = $@"
                {{
                    ""filterInput"": {{
                        ""fields"": [
                            {{
                                ""target"": ""name"",
                                ""value"": ""test""
                            }}
                        ]
                    }}
                }}
            ".ToInputs();

            var result = AssertQueryWithErrors(query, inputs: inputs, expectedErrorCount: 0);
        }

        [Fact]
        public void Should_BeValid_When_IncludingDataLoadedProperty()
        {
            var query = $@"
                query getHumans($filterInput: FilterInput) {{
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
                                    }}
                                ]
                            }}
                        ]
                    }}
                }}
            ".ToInputs();

            var result = AssertQueryWithErrors(query, inputs: inputs, expectedErrorCount: 0);
        }

        [Fact]
        public void Should_NotBeValid_When_FilteringOnPropertyThatIsntInQuery()
        {
            var query = $@"
                query getHumans($filterInput: FilterInput) {{
                    humans(filter: $filterInput) {{
                        id
                    }}
                }}
            ";

            var inputs = $@"
                {{
                    ""filterInput"": {{
                        ""fields"": [
                            {{
                                ""target"": ""name"",
                                ""value"": ""luke""
                            }}
                        ]
                    }}
                }}
            ".ToInputs();

            var result = AssertQueryWithErrors(query, inputs: inputs, expectedErrorCount: 1);
        }

        [Fact]
        public void Should_NotBeValid_When_FilteringOnPropertyWithoutValue()
        {
            var query = $@"
                query getHumans($filterInput: FilterInput) {{
                    humans(filter: $filterInput) {{
                        name
                    }}
                }}
            ";

            var inputs = $@"
                {{
                    ""filterInput"": {{
                        ""fields"": [
                            {{
                                ""target"": ""name""
                            }}
                        ]
                    }}
                }}
            ".ToInputs();

            var result = AssertQueryWithErrors(query, inputs: inputs, expectedErrorCount: 1);
        }

        [Fact]
        public void Should_NotBeValid_When_FilteringNonIncludedDataLoadedProperty()
        {
            var query = $@"
                query getHumans($filterInput: FilterInput) {{
                    humans(filter: $filterInput) {{
                        name
                        friends {{
                            id
                        }}
                    }}
                }}
            ";

            var inputs = $@"
                {{
                    ""filterInput"": {{
                        ""fields"": [
                            {{
                                ""target"": ""friends"",
                                ""fields"": [
                                    {{
                                        ""target"": ""name"",
                                        ""value"": ""test""
                                    }}
                                ]
                            }}
                        ]
                    }}
                }}
            ".ToInputs();

            var result = AssertQueryWithErrors(query, inputs: inputs, expectedErrorCount: 1);
        }
    }
}