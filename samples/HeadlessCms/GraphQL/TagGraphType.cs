using System;
using System.Collections.Generic;
using System.Linq;
using GraphQL.DataLoader;
using GraphQL.EntityFrameworkCore.Helpers.Filterable;
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
                .FilterableProperty();
            Field<ListGraphType<PageTagGraphType>, IEnumerable<PageTag>>()
                .Name("PageTags")
                .ResolveAsync(context =>
                {
                    var loader = accessor.Context.GetOrAddCollectionBatchLoader<int, PageTag>(
                        "GetTagPageTags",
                        async (tagIds) =>
                        {
                            var pageTags = await dbContext.PageTags
                                .Where(x => tagIds.Contains(x.TagId))
                                .Filter(context, dbContext.Model)
                                .ToListAsync();

                            return pageTags
                                .Select(x => new KeyValuePair<int, PageTag>(x.TagId, x))
                                .ToLookup(x => x.Key, x => x.Value);
                        });

                    return loader.LoadAsync(context.Source.Id);
                });
        }
    }
}