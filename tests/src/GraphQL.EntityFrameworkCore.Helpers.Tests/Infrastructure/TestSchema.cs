using System;
using System.Collections.Generic;
using System.Linq;
using GraphQL.DataLoader;
using GraphQL.Types;
using GraphQL.EntityFrameworkCore.Helpers;
using Microsoft.EntityFrameworkCore;

namespace GraphQL.EntityFrameworkCore.Helpers.Tests.Infrastructure
{
    public class TestSchema : Schema
    {
        public TestSchema(IServiceProvider serviceProvider, TestDbContext dbContext)
            : base(serviceProvider)
        {
            Query = new Query(dbContext);
        }
    }

    public class Query : ObjectGraphType
    {
        public Query(TestDbContext dbContext)
        {
            Field<ListGraphType<HumanGraphType>>()
                .Name("Humans")
                .From(dbContext, x => x.Humans)
                .Apply((query, context) => query.Where(x => true))
                .ResolveCollectionAsync();

            Connection<DroidGraphType>()
                .Name("Droids")
                .From(dbContext, x => x.Droids)
                .Apply((query, context) => query.Where(x => true))
                .ResolveAsync();

            Field<ListGraphType<DroidGraphType>>()
                .Name("MyDroids")
                .Argument<NonNullGraphType<IdGraphType>>("HumanId")
                .From(dbContext, x => x.Droids)
                .Apply((query, context) =>
                {
                    var humanId = context.GetArgument<Guid>("HumanId");
                    return query.Where(x => x.OwnerId == humanId);
                })
                .ValidateAsync(async context =>
                {
                    var result = new ValidationResult();
                    var humanId = context.GetArgument<Guid>("HumanId");
                    var exists = await dbContext.Humans.AnyAsync(x => x.Id == humanId);

                    if (exists == false)
                    {
                        result.Failures.Add(new ValidationFailure("HumanId", $"No Human found with the Id '{humanId}'"));
                    }

                    return result;
                })
                .ResolveCollectionAsync();

            Field<DroidGraphType>()
                .Name("Droid")
                .Argument<NonNullGraphType<IdGraphType>>("Id")
                .From(dbContext, x => x.Droids)
                .Apply((query, context) =>
                {
                    var id = context.GetArgument<Guid>("Id");
                    return query.Where(x => x.Id == id);
                })
                .ValidateAsync(async context =>
                {
                    var result = new ValidationResult();
                    var id = context.GetArgument<Guid>("id");
                    var exists = await dbContext.Droids.AnyAsync(x => x.Id == id);

                    if (exists == false)
                    {
                        result.Failures.Add(new ValidationFailure("Id", $"No Droid found with the Id '{id}'"));
                    }

                    return result;
                })
                .ResolvePropertyAsync();
            
            Field<ListGraphType<PlanetGraphType>>()
                .Name("Planets")
                .From(dbContext, x => x.Planets)
                .ResolveCollectionAsync();
        }
    }

    public class PlanetGraphType : ObjectGraphType<Planet>
    {
        public PlanetGraphType(IDataLoaderContextAccessor accessor, TestDbContext dbContext)
        {
            Field(x => x.Id, type: typeof(IdGraphType));
            Field(x => x.Name);
            Field(x => x.Region);
            Field(x => x.Sector)
                .IsFilterable(x => x.Sector)
                .Name("StarSector");
            Field(x => x.System)
                .IsFilterable();
            Field<ListGraphType<HumanGraphType>, IEnumerable<Human>>()
                .Name("Residents")
                .MapsTo(x => x.Habitants)
                .Include(accessor, dbContext)
                .Apply((query, context) => query.Where(x => true))
                .ResolveAsync();
        }
    }

    public class HumanGraphType : ObjectGraphType<Human>
    {
        public HumanGraphType(IDataLoaderContextAccessor accessor, TestDbContext dbContext)
        {
            Field(x => x.Id, type: typeof(IdGraphType));
            Field(x => x.Name);
            Field(x => x.Species);
            Field(x => x.EyeColor);
            Field<PlanetGraphType, Planet>()
                .Name("HomePlanet")
                .Include(accessor, dbContext, x => x.HomePlanet)
                .Apply((query, context) => query.Where(x => true))
                .ResolveAsync();
            Field<ListGraphType<HumanGraphType>, IEnumerable<Human>>()
                .Name("Friends")
                .Include(accessor, dbContext, x => x.Friends)
                .ResolveAsync();
        }
    }

    public class DroidGraphType : ObjectGraphType<Droid>
    {
        public DroidGraphType(IDataLoaderContextAccessor accessor, TestDbContext dbContext)
        {
            Field(x => x.Id, type: typeof(IdGraphType));
            Field(x => x.Name)
                .IsFilterable();
            Field(x => x.PrimaryFunction)
                .IsFilterable();
            Field<HumanGraphType, Human>()
                .Name("Owner")
                .Include(accessor, dbContext, x => x.Owner);
        }
    }

    public class ConnectionInput : IConnectionInput<Droid>
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
