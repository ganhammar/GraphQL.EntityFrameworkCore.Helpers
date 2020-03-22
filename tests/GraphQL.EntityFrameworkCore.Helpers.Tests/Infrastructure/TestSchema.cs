using System;
using System.Collections.Generic;
using System.Linq;
using GraphQL.DataLoader;
using GraphQL.Types;
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
            FieldAsync<ListGraphType<HumanGraphType>>(
                "Humans",
                resolve: async context => await dbContext.Humans.Select(context).ToListAsync());
        }
    }

    public class HumanGraphType : ObjectGraphType<Human>
    {
        public HumanGraphType(IDataLoaderContextAccessor accessor, TestDbContext dbContext)
        {
            Field(x => x.Id, type: typeof(IdGraphType));
            Field(x => x.Name);
            Field(x => x.HomePlanet);
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
                                .Select(context)
                                .ToListAsync();
                            
                            return humans
                                .SelectMany(x => x.Droids.Where(y => droidIds.Contains(y.Id)).ToDictionary(y => y.Id, y => x))
                                .ToDictionary(x => x.Key, x => x.Value);
                        });

                    return loader.LoadAsync(context.Source.Id);
                });
        }
    }
}
