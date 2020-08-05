using System;
using System.Collections.Generic;
using System.Linq;
using GraphQL.DataLoader;
using GraphQL.EntityFrameworkCore.Helpers.Filterable;
using GraphQL.EntityFrameworkCore.Helpers.Selectable;
using GraphQL.Types;
using HeadlessCms.Data;
using Microsoft.EntityFrameworkCore;

namespace HeadlessCms.GraphQL
{
    public class UserGraphType : ObjectGraphType<User>
    {
        public UserGraphType(IDataLoaderContextAccessor accessor, CmsDbContext dbContext)
        {
            Name = "User";

            Field(x => x.Id);
            Field(x => x.Name)
                .Filterable();
            Field(x => x.Email)
                .Filterable();
            Field<ListGraphType<PageGraphType>, IEnumerable<Page>>()
                .Name("Pages")
                .ResolveAsync(context =>
                {
                    var loader = accessor.Context.GetOrAddCollectionBatchLoader<Guid, Page>(
                        "GetUserPages",
                        async (userIds) =>
                        {
                            var pages = await dbContext.Pages
                                .Where(x => userIds.Contains(x.EditorId))
                                .Select(context, dbContext.Model)
                                .ToListAsync();

                            return pages
                                .Select(x => new KeyValuePair<Guid, Page>(x.EditorId, x))
                                .ToLookup(x => x.Key, x => x.Value);
                        });

                    return loader.LoadAsync(context.Source.Id);
                });
        }
    }
}