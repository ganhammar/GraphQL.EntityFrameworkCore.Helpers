using GraphQL.Builders;

namespace GraphQL.EntityFrameworkCore.Helpers.Filterable
{
    public static class FieldBuilderExtensions
    {
        public static FieldBuilder<TSourceType, TReturnType> Filterable<TSourceType, TReturnType>(this FieldBuilder<TSourceType, TReturnType> builder)
        {
            builder.Argument<FilterableInputGraphType>("filter");

            return builder;
        }
    }
}