using System.Collections.Generic;
using GraphQL.EntityFrameworkCore.Helpers;
using GraphQL.Types;
using HeadlessCms.Data;
using Microsoft.EntityFrameworkCore;

namespace HeadlessCms.GraphQL
{
    public class TagGraphType : ObjectGraphType<Tag>
    {
        public TagGraphType()
        {
            Name = "Tag";

            Field(x => x.Id);
            Field(x => x.Value)
                .IsFilterable();
            Field<ListGraphType<PageGraphType>, IEnumerable<Page>>()
                .Name("Pages")
                .MapsTo(x => x.Pages)
                .Include()
                .ResolveAsync();
        }
    }
}