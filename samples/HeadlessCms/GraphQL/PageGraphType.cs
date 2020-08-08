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
                            .Select(context, dbContext.Model)
                            .ToDictionaryAsync(x => x.Id, x => x));

                    return loader.LoadAsync(context.Source.EditorId);
                });
            Field<ListGraphType<TagGraphType>, IEnumerable<Tag>>()
                .Name("Tags")
                .ResolveAsync(context =>
                {
                    var loader = accessor.Context.GetOrAddCollectionBatchLoader<int, Tag>(
                        "GetPageTags",
                        async (tagIds) =>
                        {
                            var tags = await dbContext.Tags
                                .Include(x => x.PageTags)
                                .Where(x => x.PageTags.Any(y => tagIds.Contains(y.TagId)))
                                .Filter(context, dbContext.Model)
                                .ToListAsync();

                            return tags
                                .SelectMany(x => x.PageTags.Select(y => new KeyValuePair<int, Tag>(y.PageId, x)))
                                .ToLookup(x => x.Key, x => x.Value);
                        });

                    return loader.LoadAsync(context.Source.Id);
                });
        }
    }
}