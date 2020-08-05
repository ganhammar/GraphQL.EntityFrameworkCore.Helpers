using System;
using GraphQL.Types;
using HeadlessCms.Data;

namespace HeadlessCms.GraphQL
{
    public class CmsSchema : Schema
    {
        public CmsSchema(IServiceProvider serviceProvider, CmsDbContext dbContext)
            : base(serviceProvider)
        {
            Query = new Queries(dbContext);
        }
    }
}