using System;
using System.Collections.Generic;
using System.Linq;
using GraphQL.DataLoader;
using GraphQL.Types;
using GraphQL.EntityFrameworkCore.Helpers.Connection;
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
                    .Filter(context, dbContext.Model)
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
        }
    }

    public class PlanetGraphType : ObjectGraphType<Planet>
    {
        public PlanetGraphType()
        {
            Field(x => x.Id, type: typeof(IdGraphType));
            Field(x => x.Name);
            Field(x => x.Region);
            Field(x => x.Sector);
            Field(x => x.System);
        }
    }

    public class HumanGraphType : ObjectGraphType<Human>
    {
        public HumanGraphType(IDataLoaderContextAccessor accessor, TestDbContext dbContext)
        {
            Field(x => x.Id, type: typeof(IdGraphType));
            Field(x => x.Name);
            Field(x => x.HomePlanet, type: typeof(PlanetGraphType));
            Field(x => x.Species);
            Field(x => x.EyeColor);
            Field<ListGraphType<HumanGraphType>, IEnumerable<Human>>()
                .Name("Friends")
                .ResolveAsync(context =>
                {
                    var loader = accessor.Context.GetOrAddCollectionBatchLoader<Guid, Human>(
                        "GetFriends",
                        async (humanIds) =>
                        {
                            var humans = await dbContext.Humans
                                .Where(x => humanIds.Contains(x.Id))
                                .Include(x => x.Friends)
                                .ToListAsync();

                            return humans
                                .SelectMany(x => x.Friends.Select(y => new KeyValuePair<Guid, Human>(x.Id, y)))
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
