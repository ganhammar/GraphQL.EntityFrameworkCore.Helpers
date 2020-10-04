using System.Collections.Generic;
using GraphQL.DataLoader;
using GraphQL.EntityFrameworkCore.Helpers;
using GraphQL.Types;
using HeadlessCms.Data;
using Microsoft.EntityFrameworkCore;

namespace HeadlessCms.GraphQL
{
    public class UserGraphType : ObjectGraphType<User>
    {
        public UserGraphType(IDataLoaderContextAccessor accessor, CmsDbContext dbContext)
        {
            Name = "User";

            Field(x => x.Id);
            Field(x => x.Name)
                .IsFilterable();
            Field(x => x.Email)
                .IsFilterable();
            Field<ListGraphType<PageGraphType>, IEnumerable<Page>>()
                .Name("Pages")
                .Include(dbContext, x => x.Pages)
                .ResolveAsync();
        }
    }
}