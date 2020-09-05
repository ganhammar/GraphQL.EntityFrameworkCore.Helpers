using System;
using System.Collections.Generic;
using System.Linq;
using GraphQL.DataLoader;
using GraphQL.EntityFrameworkCore.Helpers;
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
                .IsFilterable();
            Field(x => x.Content)
                .IsFilterable();
            Field<UserGraphType, User>()
                .Name("Editor")
                .Include(accessor, dbContext, x => x.Editor, x => x.Id)
                .ResolveAsync();
            Field<ListGraphType<TagGraphType>, IEnumerable<Tag>>()
                .Name("Tags")
                .MapsTo(x => x.PageTags)
                    .ThenTo(x => x.Page)
                .ResolveAsync(context =>
                {
                    var loader = accessor.Context.GetOrAddCollectionBatchLoader<int, Tag>(
                        "GetPageTags",
                        async (pageIds) =>
                        {
                            var tags = await dbContext.Tags
                                .Include(x => x.PageTags)
                                .Where(x => x.PageTags.Any(y => pageIds.Contains(y.PageId)))
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