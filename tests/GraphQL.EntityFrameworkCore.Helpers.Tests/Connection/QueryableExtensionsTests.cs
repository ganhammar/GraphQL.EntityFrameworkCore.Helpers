using System;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.EntityFrameworkCore.Helpers.Connection;
using GraphQL.EntityFrameworkCore.Helpers.Tests.Infrastructure;
using GraphQL.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace GraphQL.EntityFrameworkCore.Helpers.Tests.Connection
{
    public class QueryableExtensionsTests : TestBase
    {
        [Fact]
        public async Task Should_ReturnResult_When_QueryIsValid()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();
            var request = new Request
            {
                First = 10,
                IsAsc = true,
                OrderBy = new string[] { "Id" },
            };

            var result = await dbContext.Humans.ToConnection(request, dbContext.Model);

            result.TotalCount.ShouldBe(3);
            result.Items.Count.ShouldBe(3);
        }

        [Fact]
        public async Task Should_ReturnResult_When_QueryIsValidWithAfter()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();
            var request = new Request
            {
                First = 10,
                IsAsc = true,
                OrderBy = new string[] { "Name" },
                After = ConnectionCursor.ToCursor("Leia"),
            };

            var result = await dbContext.Humans.ToConnection(request, dbContext.Model);

            result.TotalCount.ShouldBe(3);
            result.Items.Count.ShouldBe(2);
        }

        [Fact]
        public async Task Should_ReturnResult_When_QueryIsValidWithBefore()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();
            var request = new Request
            {
                First = 10,
                IsAsc = true,
                OrderBy = new string[] { "Name" },
                Before = ConnectionCursor.ToCursor("Luke"),
            };

            var result = await dbContext.Humans.ToConnection(request, dbContext.Model);

            result.TotalCount.ShouldBe(3);
            result.Items.Count.ShouldBe(1);
        }

        [Fact]
        public async Task Should_ReturnResult_When_QueryIsValidWithInt()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();
            var request = new Request
            {
                First = 10,
                IsAsc = true,
                OrderBy = new string[] { "Order" },
                After = ConnectionCursor.ToCursor("1"),
            };

            var result = await dbContext.Humans.ToConnection(request, dbContext.Model);

            result.TotalCount.ShouldBe(3);
            result.Items.Count.ShouldBe(2);
            result.Items.First().Order.ShouldBe(2);
        }

        [Fact]
        public async Task Should_ReturnResult_When_QueryIsValidWithGuid()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();
            var request = new Request
            {
                First = 10,
                IsAsc = true,
                OrderBy = new string[] { "Id" },
                After = ConnectionCursor.ToCursor("68316e17-5206-4af1-83b7-790bde49b184"),
            };

            var result = await dbContext.Humans.ToConnection(request, dbContext.Model);

            result.TotalCount.ShouldBe(3);
        }

        [Fact]
        public async Task Should_ReturnResult_When_QueryIsValidWithDateTime()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();
            var request = new Request
            {
                First = 10,
                IsAsc = false,
                OrderBy = new string[] { "CreatedAt" },
                After = ConnectionCursor.ToCursor(DateTime.Now.AddMinutes(-90)
                    .ToString(ConnectionCursor.DateTimeFormatPattern)),
            };

            var result = await dbContext.Humans.ToConnection(request, dbContext.Model);

            result.TotalCount.ShouldBe(3);
            result.Items.Count.ShouldBe(2);
            result.Items.First().Name.ShouldBe("Vader");
        }

        [Fact]
        public async Task Should_ReturnResult_When_QueryIsValidWithDateTimeOffset()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();
            var request = new Request
            {
                First = 10,
                IsAsc = false,
                OrderBy = new string[] { "UpdatedAtLocalTime" },
                After = ConnectionCursor.ToCursor(DateTimeOffset.Now.AddMinutes(-90)
                    .ToString(ConnectionCursor.DateTimeOffsetFormatPattern)),
            };

            var result = await dbContext.Humans.ToConnection(request, dbContext.Model);

            result.TotalCount.ShouldBe(3);
            result.Items.Count.ShouldBe(2);
            result.Items.First().Name.ShouldBe("Vader");
        }

        [Fact]
        public async Task Should_ReturnResult_When_QueryIsValidWithNullable()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();
            var request = new Request
            {
                First = 10,
                IsAsc = true,
                OrderBy = new string[] { "UpdatedAt" },
                After = ConnectionCursor.ToCursor(DateTime.Now.AddMinutes(-1)
                    .ToString(ConnectionCursor.DateTimeFormatPattern)),
            };

            var result = await dbContext.Humans.ToConnection(request, dbContext.Model);

            result.TotalCount.ShouldBe(3);
            result.Items.Count.ShouldBe(1);
        }

        [Fact]
        public async Task Should_ReturnResult_When_QueryIsValidCombined()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();
            var request = new Request
            {
                First = 10,
                IsAsc = true,
                OrderBy = new string[] { "Name", "Order" },
                After = ConnectionCursor.ToCursor("Leia1"),
            };

            var result = await dbContext.Humans.ToConnection(request, dbContext.Model);

            result.TotalCount.ShouldBe(3);
            result.Items.Count.ShouldBe(2);
        }

        [Fact]
        public async Task Should_ReturnResult_When_FilterIsSet()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();
            var request = new Request
            {
                First = 10,
                IsAsc = true,
                OrderBy = new string[] { "Id" },
                Filter = "Leia",
            };

            var result = await dbContext.Humans.ToConnection(request, dbContext.Model);

            result.TotalCount.ShouldBe(1);
            result.Items.Count.ShouldBe(1);
        }

        [Fact]
        public void Should_BeValid_When_AfterAndBeforeIsntSet()
        {
            var request = new Request
            {
                First = 10,
                IsAsc = true,
                OrderBy = new string[] { "Id" },
            };

            var validationResult = request.IsValid<Human, Human>();

            validationResult.IsValid.ShouldBeTrue();
            validationResult.Errors.ShouldBeEmpty();
        }

        [Fact]
        public void Should_BeValid_When_AfterIsSet()
        {
            var request = new Request
            {
                First = 10,
                IsAsc = true,
                OrderBy = new string[] { "Name" },
                After = ConnectionCursor.ToCursor("Leia"),
            };

            var validationResult = request.IsValid<Human, Human>();

            validationResult.IsValid.ShouldBeTrue();
            validationResult.Errors.ShouldBeEmpty();
        }

        [Fact]
        public void Should_BeValid_When_BeforeIsSet()
        {
            var request = new Request
            {
                First = 10,
                IsAsc = true,
                OrderBy = new string[] { "Name" },
                Before = ConnectionCursor.ToCursor("Luke"),
            };

            var validationResult = request.IsValid<Human, Human>();

            validationResult.IsValid.ShouldBeTrue();
            validationResult.Errors.ShouldBeEmpty();
        }

        [Fact]
        public void Should_NotBeValid_When_FirstIsntSet()
        {
            var request = new Request
            {
                IsAsc = true,
                OrderBy = new string[] { "Name" },
            };

            var validationResult = request.IsValid<Human, Human>();

            validationResult.IsValid.ShouldBeFalse();
            validationResult.Errors.ShouldNotBeEmpty();
        }

        [Fact]
        public void Should_NotBeValid_When_BeforeAndAfterIsSet()
        {
            var request = new Request
            {
                First = 10,
                IsAsc = true,
                OrderBy = new string[] { "Name" },
                Before = ConnectionCursor.ToCursor("Luke"),
                After = ConnectionCursor.ToCursor("Leia"),
            };

            var validationResult = request.IsValid<Human, Human>();

            validationResult.IsValid.ShouldBeFalse();
            validationResult.Errors.ShouldNotBeEmpty();
        }

        [Fact]
        public void Should_NotBeValid_When_OrderByIsntSet()
        {
            var request = new Request
            {
                First = 10,
                IsAsc = true,
            };

            var validationResult = request.IsValid<Human, Human>();

            validationResult.IsValid.ShouldBeFalse();
            validationResult.Errors.ShouldNotBeEmpty();
        }

        [Fact]
        public async Task Should_CastToReturnType_When_SourceAndReturnTypeIsDifferent()
        {
            var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();
            var request = new CloneRequest
            {
                First = 10,
                OrderBy = new string[] { "Id" },
            };

            var result = await dbContext.Humans.ToConnection(request, dbContext.Model);

            result.TotalCount.ShouldBe(3);
            result.Items.Count.ShouldBe(3);

            var item = result.Items.First();

            item.ShouldBeOfType<Clone>();
            item.Name.ShouldNotBeNullOrEmpty();
            item.ClonePlanet.ShouldNotBeNullOrEmpty();
        }

        [Fact]
        public void Should_BeValid_When_SourceAndReturnTypeIsDifferent()
        {
            var request = new CloneRequest
            {
                First = 10,
                OrderBy = new string[] { "Id" },
            };

            var validationResult = request.IsValid<Human, Clone>();

            validationResult.IsValid.ShouldBeTrue();
            validationResult.Errors.ShouldBeEmpty();
        }

        [Fact]
        public async void Should_ResolveDroidsConnection_When_Requested()
        {
            var databaseContext = ServiceProvider.GetRequiredService<TestDbContext>();
            var droids = await databaseContext.Droids
                .Select(x => x.Id)
                .OrderBy(x => x)
                .ToListAsync();

            Assert.NotEmpty(droids);

            var query = @"
                query droids {
                    droids(orderBy: [""id""]) {
                        edges {
                            node {
                                id
                            }
                        }
                    }
                }
            ";

            var expected = new
            {
                droids = new
                {
                    edges = droids.Select(x => new
                    {
                        node = new
                        {
                            id = x,
                        },
                    }),
                },
            };

            AssertQuerySuccess(query, expected);
        }

        public class Request : IConnectionInput<Human>
        {
            public IResolveFieldContext<object> Context { get; set; }
            public string After { get; set; }
            public string Before { get; set; }
            public int First { get; set; }
            public bool IsAsc { get; set; }
            public string[] OrderBy { get; set; }
            public string Filter { get; set; }
        }

        public class Clone : StarWarsCharacter
        {
            [MapsFrom("HomePlanet")]
            public string ClonePlanet { get; set; }
        }

        public class CloneRequest : IConnectionInput<Clone>
        {
            public IResolveFieldContext<object> Context { get; set; }
            public string After { get; set; }
            public string Before { get; set; }
            public int First { get; set; }
            public bool IsAsc { get; set; }
            public string[] OrderBy { get; set; }
            public string Filter { get; set; }
        }
    }
}