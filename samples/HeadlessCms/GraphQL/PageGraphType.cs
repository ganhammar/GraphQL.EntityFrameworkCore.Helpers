using System.Collections.Generic;
using GraphQL.EntityFrameworkCore.Helpers;
using GraphQL.Types;
using HeadlessCms.Data;
using Microsoft.EntityFrameworkCore;

namespace HeadlessCms.GraphQL
{
    public class PageGraphType : ObjectGraphType<Page>
    {
        public PageGraphType()
        {
            Name = "Page";

            Field(x => x.Id);
            Field(x => x.Title)
                .IsFilterable();
            Field(x => x.Content)
                .IsFilterable();
            Field<UserGraphType, User>()
                .Name("Editor")
                .Include(x => x.Editor)
                .ResolveAsync();
            Field<ListGraphType<TagGraphType>, IEnumerable<Tag>>()
                .Name("Tags")
                .MapsTo(x => x.Tags)
                .Include()
                .ResolveAsync();
        }
    }
}