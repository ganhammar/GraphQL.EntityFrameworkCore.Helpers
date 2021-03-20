using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using GraphQL.Conversion;
using GraphQL.DataLoader;
using GraphQL.Execution;
using GraphQL.Language.AST;
using GraphQL.SystemTextJson;
using GraphQL.Types;
using GraphQL.Validation;
using GraphQLParser.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace GraphQL.EntityFrameworkCore.Helpers.Tests.Infrastructure
{
    public partial class TestBase
    {
        public TestBase()
        {
            Executer = new DocumentExecuter();
            Writer = new DocumentWriter(indent: true);
            ServiceProvider = GetServiceProvider();
        }

        public ISchema Schema
        {
            get => ServiceProvider.GetService<ISchema>();
        }

        public readonly IServiceProvider ServiceProvider;
        public readonly IDocumentExecuter Executer;
        public readonly IDocumentWriter Writer;

        public IServiceProvider GetServiceProvider()
        {
            var services = new ServiceCollection();
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var dbContext = new TestDbContext(options);

            var differentOptions = new DbContextOptionsBuilder<DifferentTestDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var differentDbContext = new DifferentTestDbContext(differentOptions);

            if (dbContext.Humans.AnyAsync().GetAwaiter().GetResult() == false)
            {
                StarWarsData.Seed(dbContext).GetAwaiter().GetResult();
                StarWarsData.Seed(differentDbContext).GetAwaiter().GetResult();
            }

            services
                .AddGraphQLEntityFrameworkCoreHelpers<TestDbContext>()
                .AddSingleton<IDocumentExecutionListener, DataLoaderDocumentListener>()
                .AddSingleton<INameConverter, CamelCaseNameConverter>()
                .AddSingleton<ISchema, TestSchema>()
                .AddTransient<PlanetGraphType>()
                .AddTransient<HumanGraphType>()
                .AddTransient<DroidGraphType>()
                .AddTransient<HumanForceAlignmentGraphType>()
                .AddTransient<ForceGraphType>()
                .AddTransient<GalaxyGraphType>()
                .AddTransient<TestDbContext>(_ => dbContext)
                .AddTransient<DifferentTestDbContext>(_ => differentDbContext);

            return services.BuildServiceProvider();
        }

        public ExecutionResult AssertQuerySuccess(
            string query,
            string expected,
            Inputs inputs = null,
            object root = null,
            IDictionary<string, object> userContext = null,
            CancellationToken cancellationToken = default(CancellationToken),
            IEnumerable<IValidationRule> rules = null)
        {
            var queryResult = CreateQueryResult(expected);
            return AssertQuery(query, queryResult, inputs, root, userContext, cancellationToken, rules);
        }

        public ExecutionResult AssertQuerySuccess(
            string query,
            object expected,
            Inputs inputs = null,
            object root = null,
            IDictionary<string, object> userContext = null,
            CancellationToken cancellationToken = default(CancellationToken),
            IEnumerable<IValidationRule> rules = null)
        {
            var queryResult = CreateQueryResult(JsonSerializer.Serialize(expected));
            return AssertQuery(query, queryResult, inputs, root, userContext, cancellationToken, rules);
        }

        public ExecutionResult AssertQueryWithErrors(
            string query,
            string expected = default,
            Inputs inputs = null,
            object root = null,
            IDictionary<string, object> userContext = null,
            CancellationToken cancellationToken = default(CancellationToken),
            int expectedErrorCount = 0,
            bool renderErrors = false)
        {
            var queryResult = expected != default ? CreateQueryResult(expected) : default;
            return AssertQueryIgnoreErrors(
                query,
                queryResult,
                inputs,
                root,
                userContext,
                cancellationToken,
                expectedErrorCount,
                renderErrors);
        }

        public ExecutionResult AssertQueryIgnoreErrors(
            string query,
            ExecutionResult expectedExecutionResult = default,
            Inputs inputs= null,
            object root = null,
            IDictionary<string, object> userContext = null,
            CancellationToken cancellationToken = default(CancellationToken),
            int expectedErrorCount = 0,
            bool renderErrors = false)
        {
            var runResult = Executer.ExecuteAsync(x =>
            {
                x.Schema = Schema;
                x.Query = query;
                x.Root = root;
                x.Inputs = inputs;
                x.UserContext = userContext;
                x.CancellationToken = cancellationToken;
                x.RequestServices = ServiceProvider;
                foreach (var listener in ServiceProvider.GetService<IEnumerable<IDocumentExecutionListener>>())
                {
                    x.Listeners.Add(listener);
                }
            }).GetAwaiter().GetResult();

            if (expectedExecutionResult != default)
            {
                var renderResult = renderErrors ? runResult : new ExecutionResult { Data = runResult.Data };

                var writtenResult = Writer.WriteToStringAsync(renderResult).GetAwaiter().GetResult();
                var expectedResult = Writer.WriteToStringAsync(expectedExecutionResult).GetAwaiter().GetResult();

                writtenResult.ShouldBe(expectedResult);
            }

            var errors = runResult.Errors ?? new ExecutionErrors();

            errors.Count().ShouldBe(expectedErrorCount);

            return runResult;
        }

        public ExecutionResult AssertQuery(
            string query,
            ExecutionResult expectedExecutionResult,
            Inputs inputs,
            object root,
            IDictionary<string, object> userContext = null,
            CancellationToken cancellationToken = default(CancellationToken),
            IEnumerable<IValidationRule> rules = null)
        {
            var runResult = Executer.ExecuteAsync(x =>
            {
                x.Schema = Schema;
                x.Query = query;
                x.Root = root;
                x.Inputs = inputs;
                x.UserContext = userContext;
                x.CancellationToken = cancellationToken;
                x.ValidationRules = rules;
                x.RequestServices = ServiceProvider;
                foreach (var listener in ServiceProvider.GetService<IEnumerable<IDocumentExecutionListener>>())
                {
                    x.Listeners.Add(listener);
                }
            }).GetAwaiter().GetResult();

            var writtenResult = Writer.WriteToStringAsync(runResult).GetAwaiter().GetResult();
            var expectedResult = Writer.WriteToStringAsync(expectedExecutionResult).GetAwaiter().GetResult();

            string additionalInfo = null;

            if (runResult.Errors?.Any() == true)
            {
                additionalInfo = string.Join(Environment.NewLine, runResult.Errors
                    .Where(x => x.InnerException is GraphQLSyntaxErrorException)
                    .Select(x => x.InnerException.Message));
            }

            writtenResult.ShouldBe(expectedResult, additionalInfo);

            return runResult;
        }

        public static ExecutionResult CreateQueryResult(string result, ExecutionErrors errors = null)
            => result.ToExecutionResult(errors);

        protected static IResolveFieldContext<object> GetContext(string queryName = "humans", string filter = default, string[] fields = default)
        {
            if (fields == default)
            {
                fields = new[] { "name" };
            }

            var context = new ResolveFieldContext<object>();

            context.SubFields = new Dictionary<string, Field>();

            foreach(var fieldName in fields)
            {
                context.SubFields.Add(fieldName, new Field(new NameNode(fieldName), new NameNode(fieldName)));
            }

            context.FieldDefinition = new FieldType();

            context.FieldDefinition.Name = queryName;
            context.Path = new List<string> { queryName };

            var field = new Field(new NameNode(context.FieldDefinition.Name), new NameNode(context.FieldDefinition.Name));
            field.SelectionSet = new SelectionSet();

            foreach(var fieldName in fields)
            {
                field.SelectionSet.Add(new Field(new NameNode(fieldName), new NameNode(fieldName)));
            }

            if (filter != default)
            {
                field.Arguments = new Arguments();
                field.Arguments.Add(new Argument(
                    new NameNode("filter"),
                    new VariableReference(new NameNode("filter"))));
            }

            context.Operation = new Operation(new NameNode(context.FieldDefinition.Name));
            context.Operation.SelectionSet = new SelectionSet();
            context.Operation.SelectionSet.Add(field);

            if (filter == default)
            {
                return context;
            }

            var fieldsInput = new List<FilterableInputField>
            {
                new FilterableInputField
                {
                    Value = filter,
                },
            };

            var filterInput = new FilterableInput
            {
                Fields = fieldsInput,
            };

            context.Variables = new Variables();
            context.Variables.Add(new Variable
            {
                Name = "filter",
                Value = filterInput,
            });

            return context;
        }
    }
}