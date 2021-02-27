using System;
using System.Collections.Generic;
using GraphQL.DataLoader;
using GraphQL.Types;
using Microsoft.EntityFrameworkCore;

namespace GraphQL.EntityFrameworkCore.Helpers.Tests.Infrastructure
{
    public class TestSchema : Schema
    {
        public TestSchema(IServiceProvider serviceProvider, TestDbContext dbContext, DifferentTestDbContext differentTestDbContext)
            : base(serviceProvider)
        {
            Query = new Query(dbContext, differentTestDbContext);
        }
    }

    public class Query : ObjectGraphType
    {
        public Query(TestDbContext dbContext, DifferentTestDbContext differentTestDbContext)
        {
            Field<ListGraphType<HumanGraphType>>()
                .Name("Humans")
                .From(dbContext.Humans)
                .Where((context) => x => true)
                .ResolveCollectionAsync();

            Connection<DroidGraphType>()
                .Name("Droids")
                .From(dbContext.Droids)
                .Where((context) => x => true)
                .ResolveAsync();

            Field<ListGraphType<DroidGraphType>>()
                .Name("MyDroids")
                .Argument<NonNullGraphType<IdGraphType>>("HumanId")
                .From(dbContext.Droids)
                .Where(context =>
                {
                    var humanId = context.GetArgument<Guid>("HumanId");
                    return x => x.OwnerId == humanId;
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

            Field<DroidGraphType, Droid>()
                .Name("Droid")
                .Argument<NonNullGraphType<IdGraphType>>("Id")
                .From(dbContext.Droids)
                .Where(context =>
                {
                    var id = context.GetArgument<Guid>("Id");
                    return x => x.Id == id;
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

            Field<ListGraphType<GalaxyGraphType>>()
                .Name("Galaxies")
                .From(differentTestDbContext.Galaxies)
                .ResolveCollectionAsync();
            
            Field<ListGraphType<HumanForceAlignmentGraphType>>()
                .Name("HumanForceAlignments")
                .From(dbContext.HumanForceAlignments)
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
                .Include(dbContext, x => x.Habitants)
                .Where((context) => (item) => item.Name != default)
                .ResolveAsync();
        }
    }

    public class HumanGraphType : ObjectGraphType<Human>
    {
        public HumanGraphType(TestDbContext dbContext)
        {
            Field(x => x.Id, type: typeof(IdGraphType));
            Field(x => x.Name);
            Field(x => x.Species);
            Field(x => x.EyeColor);
            Field<PlanetGraphType, Planet>()
                .Name("HomePlanet")
                .Include(dbContext, x => x.HomePlanet)
                .Where((context) => (item) => item.Name != default)
                .ResolveAsync();
            Field<ListGraphType<HumanGraphType>, IEnumerable<Human>>()
                .Name("Friends")
                .Include(x => x.Friends)
                .ResolveAsync();
        }
    }

    public class DroidGraphType : ObjectGraphType<Droid>
    {
        public DroidGraphType()
        {
            Field(x => x.Id, type: typeof(IdGraphType));
            Field(x => x.Name)
                .IsFilterable();
            Field(x => x.PrimaryFunction)
                .IsFilterable();
            Field<HumanGraphType, Human>()
                .Name("Owner")
                .Include(x => x.Owner)
                .ResolveAsync();
        }
    }

    public class GalaxyGraphType : ObjectGraphType<Galaxy>
    {
        public GalaxyGraphType()
        {
            Field(x => x.Id, type: typeof(IdGraphType));
            Field(x => x.Name)
                .IsFilterable();
        }
    }

    public class ForceGraphType : ObjectGraphType<Force>
    {
        public ForceGraphType()
        {
            Field(x => x.Type);
        }
    }

    public class HumanForceAlignmentGraphType : ObjectGraphType<HumanForceAlignment>
    {
        public HumanForceAlignmentGraphType()
        {
            Field(x => x.HumanId, type: typeof(IdGraphType));
            Field(x => x.Alignment);
            Field<ForceGraphType, Force>()
                .Name("Force")
                .Include(x => x.Force)
                .ResolveAsync();
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
