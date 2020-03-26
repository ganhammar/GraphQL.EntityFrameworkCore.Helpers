using System;
using GraphQL.EntityFrameworkCore.Helpers.Connection;
using GraphQL.Types;
using Shouldly;
using Xunit;

namespace GraphQL.EntityFrameworkCore.Helpers.Tests.Connection
{
    public class ConnectionCursorTests
    {
        [Fact]
        public void Should_GetCursorInCorrectFormat_When_CallingGetCursorOnNullableDateTime()
        {
            var request = new Request
            {
                OrderBy = new [] { "when" },
            };

            var model = new Source
            {
                When = DateTime.Now,
            };

            var lambda = ConnectionCursor.GetLambdaForCursor<Source, Source>(request);

            var cursor = lambda(model);

            cursor.ShouldBe(model.When.Value.ToString(ConnectionCursor.DateTimeFormatPattern));
        }

        [Fact]
        public void Should_GetCursorInCorrectFormat_When_CallingGetCursorOnNullableDateTimeOffset()
        {
            var request = new Request
            {
                OrderBy = new [] { "whenInTheWorld" },
            };

            var model = new Source
            {
                WhenInTheWorld = DateTime.Now,
            };

            var lambda = ConnectionCursor.GetLambdaForCursor<Source, Source>(request);

            var cursor = lambda(model);

            cursor.ShouldBe(model.WhenInTheWorld.Value.ToString(ConnectionCursor.DateTimeOffsetFormatPattern));
        }

        [Fact]
        public void Should_GetCursorWithDefaultDateTime_When_CallingGetCursorOnNullDateTime()
        {
            var request = new Request
            {
                OrderBy = new [] { "when" },
            };

            var model = new Source();

            var lambda = ConnectionCursor.GetLambdaForCursor<Source, Source>(request);

            var cursor = lambda(model);

            cursor.ShouldBe(default(DateTime).ToString(ConnectionCursor.DateTimeFormatPattern));
        }

        [Fact]
        public void Should_GetCursorWithDefaultInt_When_CallingGetCursorOnNullInt()
        {
            var request = new Request
            {
                OrderBy = new [] { "number" },
            };

            var model = new Source();

            var lambda = ConnectionCursor.GetLambdaForCursor<Source, Source>(request);

            var cursor = lambda(model);

            cursor.ShouldBe(default(int).ToString());
        }

        [Fact]
        public void Should_GetCursorWithDefaultFloat_When_CallingGetCursorOnNullFloat()
        {
            var request = new Request
            {
                OrderBy = new [] { "float" },
            };

            var model = new Source();

            var lambda = ConnectionCursor.GetLambdaForCursor<Source, Source>(request);

            var cursor = lambda(model);

            cursor.ShouldBe(default(float).ToString());
        }

        [Fact]
        public void Should_GetCombinedCursorWithDefault_When_HavingMultipleNullOrderBy()
        {
            var request = new Request
            {
                OrderBy = new [] { "when", "number" },
            };

            var model = new Source();

            var lambda = ConnectionCursor.GetLambdaForCursor<Source, Source>(request);

            var cursor = lambda(model);

            cursor.ShouldBe($"{default(DateTime).ToString(ConnectionCursor.DateTimeFormatPattern)}{default(int).ToString()}");
        }

        [Fact]
        public void Should_GetCombinedCursorWithFormatedValue_When_HavingMultipleOrderBy()
        {
            var request = new Request
            {
                OrderBy = new [] { "when", "number" },
            };

            var model = new Source
            {
                When = DateTime.Now,
            };

            var lambda = ConnectionCursor.GetLambdaForCursor<Source, Source>(request);

            var cursor = lambda(model);

            cursor.ShouldBe($"{model.When.Value.ToString(ConnectionCursor.DateTimeFormatPattern)}{default(int).ToString()}");
        }

        public class Source
        {
            public DateTime? When { get; set; }
            public DateTimeOffset? WhenInTheWorld { get; set; }
            public int? Number { get; set; }
            public float? Float { get; set; }
        }

        public class Request : IConnectionInput<Source>
        {
            public string After { get; set; }
            public string Before { get; set; }
            public int First { get; set; }
            public bool IsAsc { get; set; }
            public string[] OrderBy { get; set; }
            public string Filter { get; set; }
            public IResolveFieldContext<object> Context { get; set; }
        }
    }
}