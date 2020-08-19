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
    public class TagGraphType : ObjectGraphType<Tag>
    {
        public TagGraphType(IDataLoaderContextAccessor accessor, CmsDbContext dbContext)
        {
            Name = "Tag";

            Field(x => x.Id);
            Field(x => x.Value)
                .IsFilterable();
            Field<ListGraphType<PageGraphType>, IEnumerable<Page>>()
                .Name("Pages")
                .MapsTo(x => x.PageTags)
                    .ThenTo(x => x.Page)
                .ResolveAsync(context =>
                {
                    var loader = accessor.Context.GetOrAddCollectionBatchLoader<int, Page>(
                        "GetTagPages",
                        async (tagIds) =>
                        {
                            var pages = await dbContext.Pages
                                .Include(x => x.PageTags)
                                .Where(x => x.PageTags.Any(y => tagIds.Contains(y.TagId)))
                                .Filter(context, dbContext.Model)
                                .ToListAsync();

                            return pages
                                .SelectMany(x => x.PageTags.Select(y => new KeyValuePair<int, Page>(y.TagId, x)))
                                .ToLookup(x => x.Key, x => x.Value);
                        });

                    return loader.LoadAsync(context.Source.Id);
                });
        }
    }
}