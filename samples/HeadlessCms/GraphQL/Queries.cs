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
                .From(dbContext.Pages)
                .ResolveAsync();

            Connection<UserGraphType>()
                .Name("Users")
                .From(dbContext.Users)
                .ResolveAsync();

            Connection<TagGraphType>()
                .Name("Tags")
                .From(dbContext.Tags)
                .ResolveAsync();
        }
    }
}