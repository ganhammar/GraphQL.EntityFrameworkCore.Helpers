using System;
using System.Collections.Generic;
using System.Linq;
using GraphQL.DataLoader;
using GraphQL.Types;
using GraphQL.EntityFrameworkCore.Helpers;

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
                .ResolveListAsync();

            Connection<DroidGraphType>()
                .Name("Droids")
                .From(dbContext, x => x.Droids)
                .ResolveAsync(typeof(ConnectionInput));
            
            Field<ListGraphType<PlanetGraphType>>()
                .Name("Planets")
                .From(dbContext, x => x.Planets)
                .ResolveListAsync();
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
