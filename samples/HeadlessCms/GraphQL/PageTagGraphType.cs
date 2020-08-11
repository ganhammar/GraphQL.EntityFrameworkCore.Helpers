using System.Linq;
using GraphQL.DataLoader;
using GraphQL.EntityFrameworkCore.Helpers.Selectable;
using GraphQL.Types;
using HeadlessCms.Data;
using Microsoft.EntityFrameworkCore;

namespace HeadlessCms.GraphQL
{
    public class PageTagGraphType : ObjectGraphType<PageTag>
    {
        public PageTagGraphType(IDataLoaderContextAccessor accessor, CmsDbContext dbContext)
        {
            Name = "PageTag";

            Field<TagGraphType, Tag>()
                .Name("Tag")
                .ResolveAsync(context =>
                {
                    var loader = accessor.Context.GetOrAddBatchLoader<int, Tag>(
                        "GetPageTagTag",
                        async (tagIds) => await dbContext.Tags
                            .Where(x => tagIds.Contains(x.Id))
                            .SelectFromContext(context, dbContext.Model)
                            .ToDictionaryAsync(x => x.Id, x => x));

                    return loader.LoadAsync(context.Source.TagId);
                });
            Field<PageGraphType, Page>()
                .Name("Page")
                .ResolveAsync(context =>
                {
                    var loader = accessor.Context.GetOrAddBatchLoader<int, Page>(
                        "GetPageTagPage",
                        async (pageIds) => await dbContext.Pages
                            .Where(x => pageIds.Contains(x.Id))
                            .SelectFromContext(context, dbContext.Model)
                            .ToDictionaryAsync(x => x.Id, x => x));

                    return loader.LoadAsync(context.Source.PageId);
                });
        }
    }
}