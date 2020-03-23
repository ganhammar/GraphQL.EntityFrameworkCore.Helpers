using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using GraphQL.Conversion;
using GraphQL.DataLoader;
using GraphQL.Execution;
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

            if (dbContext.Humans.AnyAsync().GetAwaiter().GetResult() == false)
            {
                StarWarsData.Seed(dbContext).GetAwaiter().GetResult();
            }

            services
                .AddSingleton<IDataLoaderContextAccessor, DataLoaderContextAccessor>()
                .AddSingleton<IDocumentExecutionListener, DataLoaderDocumentListener>()
                .AddSingleton<ISchema, TestSchema>()
                .AddTransient<HumanGraphType>()
                .AddTransient<DroidGraphType>()
                .AddTransient<TestDbContext>(_ => dbContext)
                .AddGraphQLEntityFrameworkCoreHelpers(dbContext);

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
            string expected,
            Inputs inputs = null,
            object root = null,
            IDictionary<string, object> userContext = null,
            CancellationToken cancellationToken = default(CancellationToken),
            int expectedErrorCount = 0,
            bool renderErrors = false)
        {
            var queryResult = CreateQueryResult(expected);
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
            ExecutionResult expectedExecutionResult,
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
                foreach (var listener in ServiceProvider.GetService<IEnumerable<IDocumentExecutionListener>>())
                {
                    x.Listeners.Add(listener);
                }
            }).GetAwaiter().GetResult();

            var renderResult = renderErrors ? runResult : new ExecutionResult { Data = runResult.Data };

            var writtenResult = Writer.WriteToStringAsync(renderResult).GetAwaiter().GetResult();
            var expectedResult = Writer.WriteToStringAsync(expectedExecutionResult).GetAwaiter().GetResult();

            writtenResult.ShouldBe(expectedResult);

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
                x.FieldNameConverter = new CamelCaseFieldNameConverter();
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
            => new ExecutionResult
            {
                Data = string.IsNullOrWhiteSpace(result) ? null : result.ToDictionary(),
                Errors = errors
            };
    }
}