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
                .ResolveConnectionAsync(dbContext, x => x.Pages, typeof(ConnectionInput<Page>));

            Connection<UserGraphType>()
                .Name("Users")
                .ResolveConnectionAsync(dbContext, x => x.Users, typeof(ConnectionInput<User>));

            Connection<TagGraphType>()
                .Name("Tags")
                .ResolveConnectionAsync(dbContext, x => x.Tags, typeof(ConnectionInput<Tag>));
        }
    }
}