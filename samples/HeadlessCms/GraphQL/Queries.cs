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
                .FromDbContext(dbContext, x => x.Pages)
                .ResolveAsync(typeof(ConnectionInput<Page>));

            Connection<UserGraphType>()
                .Name("Users")
                .FromDbContext(dbContext, x => x.Users)
                .ResolveAsync(typeof(ConnectionInput<User>));

            Connection<TagGraphType>()
                .Name("Tags")
                .FromDbContext(dbContext, x => x.Tags)
                .ResolveAsync(typeof(ConnectionInput<Tag>));
        }
    }
}