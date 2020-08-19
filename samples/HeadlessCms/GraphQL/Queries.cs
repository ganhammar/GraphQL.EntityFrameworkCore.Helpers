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
                .Paged()
                .ResolveAsync(async context =>
                {
                    var request = new ConnectionInput<Page>();
                    request.SetConnectionInput(context);

                    return await dbContext.Pages.ToConnection(request, dbContext.Model);
                });

            Connection<UserGraphType>()
                .Name("Users")
                .Paged()
                .ResolveAsync(async context =>
                {
                    var request = new ConnectionInput<User>();
                    request.SetConnectionInput(context);

                    return await dbContext.Users.ToConnection(request, dbContext.Model);
                });

            Connection<TagGraphType>()
                .Name("Tags")
                .Paged()
                .ResolveAsync(async context =>
                {
                    var request = new ConnectionInput<Tag>();
                    request.SetConnectionInput(context);

                    return await dbContext.Tags.ToConnection(request, dbContext.Model);
                });
        }
    }
}