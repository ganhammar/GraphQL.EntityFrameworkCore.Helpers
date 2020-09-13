using GraphQL.EntityFrameworkCore.Helpers;
using GraphQL.Types;
using HeadlessCms.Data;

namespace HeadlessCms.GraphQL
{
    public class Queries : ObjectGraphType
    {
        public Queries(CmsDbContext dbContext)
        {
            Connection<PageGraphType>()
                .Name("Pages")
                .From(dbContext, x => x.Pages)
                .ResolveAsync();

            Connection<UserGraphType>()
                .Name("Users")
                .From(dbContext, x => x.Users)
                .ResolveAsync();

            Connection<TagGraphType>()
                .Name("Tags")
                .From(dbContext, x => x.Tags)
                .ResolveAsync();
        }
    }
}