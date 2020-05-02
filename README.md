# GraphQL Helpers for EF Core - Connections, Selection from Request and More

Helpers to add bidirectional cursor based paginaton to your endpoints with the `IQueryable` extension method `ToConnection`. The method expects a request that implements `IConnectionInput<T>` as input. Parameters is added to the `ConnectionBuilder` using the extension method `Paged(defaultPageSize)`.

The `Select(IResolveFieldContext context)` extension methods helps you to avoid overfetching data by only selecting the fields requested. Foreign key fields are included by default as the value might be used for data loaded fields (requires `DbContext.IModel`). `ToConnection` is using this method per default.

With the `Filter(context)` extension method you can filter a list of items based on a search string. What columns should be filterable is determined by the `FilterableAttribute`. The filter parameter is applied `Filterable` extension.

## Getting Started

```
dotnet add package GraphQL.EntityFrameworkCore.Helpers
```

### Examples

#### Connection

Bidirectional cursor based pagination with support for ordering, filtering and selecting based on what is requested.

With the parameter `orderBy` (array of strings, i.e `[ "name", "id" ]`) you can specify in the client what order you want the items in. The order will be ascending by default, you can change it to descending by setting the parameter `isAsc` to `false`.

It is important that each resulting cursor points at a unique row in order to be able to determine what rows to include `before` or `after` a specific row. Therefore the primary key(s) of a certain entity is automaticaly included in the `orderBy` if the asked for order by columns isn't considered unique. Columns considered unique is either the primary key, has a unique constraint or is of type `GUID`, `DATETIME` or `DATETIMEOFFSET`. If you have column that you know are unique but doesn't meet those criterias you can use the `Unique` attribute.

```c#
Connection<DroidGraphType>()
    .Name("Droids")
    .Paged()
    .ResolveAsync(async context =>
    {
        var request = new ConnectionInput();
        request.SetConnectionInput(context);

        return await dbContext.Droids
            .ToConnection(request, dbContext.Model); // IModel is required for Select from Request
    });
```


##### Validating the request

You can validate the request outside of `ToConnection` if you want to ensure it's valid in your validation pipeline, the first generic type parameter is the `DbSet` and the second parameter is the resulting type of the request. It needs the `IModel` to determine that the `orderBy` is valid, it doesn't need a `orderBy` if there is primary key it could use instead.

```c#
var validationResult = request.Validate<Human, Clone>(dbContext.Model);
```

#### Select from Request

```c#
FieldAsync<ListGraphType<HumanGraphType>>(
    "Humans",
    resolve: async context => await dbContext.Humans.Select(context, dbContext.Model).ToListAsync());
```

#### Filter

Add `FilterableAttribute` columns that should be filterable.

```c#
public class Human
{
    public Guid Id { get; set; }

    [Filterable]
    public string Name { get; set; }
}
```
Add the argument to the `FieldBuilder` using the extension method `Filterable()` and filter the list with the `IQueryable` extension method `Filter(context)`.

```c#
Field<ListGraphType<HumanGraphType>>()
    .Name("Humans")
    .Filterable()
    .ResolveAsync(async context => await dbContext.Humans
        .Filter(context)
        .Select(context, dbContext.Model)
        .ToListAsync());
```