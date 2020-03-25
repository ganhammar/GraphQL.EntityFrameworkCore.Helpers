using GraphQL.Builders;
using GraphQL.Types;

namespace GraphQL.EntityFrameworkCore.Helpers
{
    public static class FieldBuilderExtensions
    {
        public static FieldBuilder<TSourceType, TReturnType> Filterable<TSourceType, TReturnType>(this FieldBuilder<TSourceType, TReturnType> builder)
        {
            builder.Argument<StringGraphType>("filter", "String to filter the list by");

            return builder;
        }
    }
}