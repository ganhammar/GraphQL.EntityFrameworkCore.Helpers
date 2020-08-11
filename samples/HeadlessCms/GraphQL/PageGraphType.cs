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
    public class PageGraphType : ObjectGraphType<Page>
    {
        public PageGraphType(IDataLoaderContextAccessor accessor, CmsDbContext dbContext)
        {
            Name = "Page";

            Field(x => x.Id);
            Field(x => x.Title)
                .FilterableProperty();
            Field(x => x.Content)
                .FilterableProperty();
            Field<UserGraphType, User>()
                .Name("Editor")
                .ResolveAsync(context =>
                {
                    var loader = accessor.Context.GetOrAddBatchLoader<int, User>(
                        "GetPageEditors",
                        async (userIds) => await dbContext.Users
                            .Where(x => userIds.Contains(x.Id))
                            .SelectFromContext(context, dbContext.Model)
                            .ToDictionaryAsync(x => x.Id, x => x));

                    return loader.LoadAsync(context.Source.EditorId);
                });
            Field<ListGraphType<PageTagGraphType>, IEnumerable<PageTag>>()
                .Name("PageTags")
                .ResolveAsync(context =>
                {
                    var loader = accessor.Context.GetOrAddCollectionBatchLoader<int, PageTag>(
                        "GetPagePageTags",
                        async (pageIds) =>
                        {
                            var pageTags = await dbContext.PageTags
                                .Where(x => pageIds.Contains(x.PageId))
                                .Filter(context, dbContext.Model)
                                .ToListAsync();

                            return pageTags
                                .Select(x => new KeyValuePair<int, PageTag>(x.PageId, x))
                                .ToLookup(x => x.Key, x => x.Value);
                        });

                    return loader.LoadAsync(context.Source.Id);
                });
        }
    }
}