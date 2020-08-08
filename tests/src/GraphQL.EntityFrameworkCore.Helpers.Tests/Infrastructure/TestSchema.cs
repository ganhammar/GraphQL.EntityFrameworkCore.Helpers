using System;
using System.Collections.Generic;
using System.Linq;
using GraphQL.DataLoader;
using GraphQL.Types;
using GraphQL.EntityFrameworkCore.Helpers.Connection;
using GraphQL.EntityFrameworkCore.Helpers.Selectable;
using GraphQL.EntityFrameworkCore.Helpers.Filterable;
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
                .Filterable()
                .ResolveAsync(async context => await dbContext.Humans
                    .Select(context, dbContext.Model)
                    .ToListAsync());

            Connection<DroidGraphType>()
                .Name("Droids")
                .Paged()
                .ResolveAsync(async context =>
                {
                    var request = new ConnectionInput();
                    request.SetConnectionInput(context);

                    return await dbContext.Droids.ToConnection(request, dbContext.Model);
                });
            
            Field<ListGraphType<PlanetGraphType>>()
                .Name("Planets")
                .Filterable()
                .ResolveAsync(async context => await dbContext.Planets
                    .Select(context, dbContext.Model)
                    .ToListAsync());
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
                .FilterableProperty(x => x.Sector)
                .Name("StarSector");
            Field(x => x.System)
                .FilterableProperty();
            Field<ListGraphType<HumanGraphType>, IEnumerable<Human>>()
                .Name("Residents")
                .Property(x => x.Habitants)
                .ResolveAsync(context =>
                {
                    var loader = accessor.Context.GetOrAddCollectionBatchLoader<Guid, Human>(
                        "GetHabitants",
                        async (planetIds) =>
                        {
                            var humans = await dbContext.Humans
                                .Where(x => planetIds.Contains(x.HomePlanetId))
                                .Select(context, dbContext.Model)
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
                            .Select(context, dbContext.Model)
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
            Field(x => x.Name);
            Field(x => x.PrimaryFunction);
            Field<HumanGraphType, Human>()
                .Name("Owner")
                .ResolveAsync(context =>
                {
                    var loader = accessor.Context.GetOrAddBatchLoader<Guid, Human>(
                        "GetOwner",
                        async (droidIds) =>
                        {
                            var humans = await dbContext.Humans
                                .Where(x => x.Droids.Any(y => droidIds.Contains(y.Id)))
                                .Select(context, dbContext.Model)
                                .ToListAsync();
                            
                            return humans
                                .SelectMany(x => x.Droids.Where(y => droidIds.Contains(y.Id)).ToDictionary(y => y.Id, y => x))
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
