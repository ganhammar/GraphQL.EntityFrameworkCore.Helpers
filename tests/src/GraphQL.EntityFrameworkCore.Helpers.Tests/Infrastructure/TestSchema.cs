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
            Field<ListGraphType<HumanGraphType>, List<Human>>()
                .Name("Humans")
                .ResolveListAsync(dbContext, x => x.Humans);

            Connection<DroidGraphType>()
                .Name("Droids")
                .ResolveConnectionAsync(dbContext, x => x.Droids, typeof(ConnectionInput));
            
            Field<ListGraphType<PlanetGraphType>>()
                .Name("Planets")
                .ResolveListAsync(dbContext, x => x.Planets);
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
                .ResolveAsync(context =>
                {
                    var loader = accessor.Context.GetOrAddCollectionBatchLoader<Guid, Human>(
                        "GetHabitants",
                        async (planetIds) =>
                        {
                            var humans = await dbContext.Humans
                                .Where(x => planetIds.Contains(x.HomePlanetId))
                                .SelectFromContext(context, dbContext.Model)
                                .ToListAsync();

                            return humans
                                .Select(x => new KeyValuePair<Guid, Human>(x.HomePlanetId, x))
                                .ToLookup(x => x.Key, x => x.Value);
                        });

                    return loader.LoadAsync(context.Source.Id);
                });
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
                .ResolveAsync(context =>
                {
                    var loader = accessor.Context.GetOrAddBatchLoader<Guid, Planet>(
                        "GetHomePlanets",
                        async (planetIds) => await dbContext.Planets
                            .Where(x => planetIds.Contains(x.Id))
                            .SelectFromContext(context, dbContext.Model)
                            .ToDictionaryAsync(x => x.Id, x => x));

                    return loader.LoadAsync(context.Source.HomePlanetId);
                });
            Field<ListGraphType<HumanGraphType>, IEnumerable<Human>>()
                .Name("Friends")
                .ResolveAsync(context =>
                {
                    var loader = accessor.Context.GetOrAddCollectionBatchLoader<Guid, Human>(
                        "GetFriends",
                        async (humanIds) =>
                        {
                            var humans = await dbContext.Humans
                                .Include(x => x.Friends)
                                .Where(x => x.Friends.Any(y => humanIds.Contains(y.Id)))
                                .Filter(context, dbContext.Model)
                                .ToListAsync();

                            return humans
                                .SelectMany(x => x.Friends.Select(y => new KeyValuePair<Guid, Human>(y.Id, x)))
                                .ToLookup(x => x.Key, x => x.Value);
                        });

                    return loader.LoadAsync(context.Source.Id);
                });
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
                .ResolveAsync(context =>
                {
                    var loader = accessor.Context.GetOrAddBatchLoader<Guid, Human>(
                        "GetOwner",
                        async (droidIds) =>
                        {
                            var humans = await dbContext.Humans
                                .Include(x => x.Droids)
                                .Where(x => x.Droids.Any(y => droidIds.Contains(y.Id)))
                                .Filter(context, dbContext.Model)
                                .ToListAsync();
                            
                            return humans
                                .SelectMany(x => x.Droids.Where(y => droidIds.Contains(y.Id)).Select(y => new KeyValuePair<Guid, Human>(y.Id, x)))
                                .ToDictionary(x => x.Key, x => x.Value);
                        });

                    return loader.LoadAsync(context.Source.Id);
                });
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
