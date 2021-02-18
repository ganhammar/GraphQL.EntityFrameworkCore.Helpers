# Connections

Bidirectional cursor based pagination with support for ordering, filtering and selecting based on what is requested.

With the parameter `orderBy` (array of strings, i.e `[ "name", "id" ]`) you can specify in the client what order you want the items in. The order will be ascending by default, you can change it to descending by setting the parameter `isAsc` to `false`.

It is important that each resulting cursor points at a unique row in order to be able to determine what rows to include `before` or `after` a specific row. Therefore the primary key(s) of a certain entity is automaticaly included in the `orderBy` if the asked for order by columns isn't considered unique. Columns considered unique is either the primary key, has a unique constraint or is of type `GUID`, `DATETIME` or `DATETIMEOFFSET`. If you have a column that you know is unique but doesn't meet those criterias you can use the `Unique` attribute.

`ToConnection` applies both `SelectFromContext` and `Filter` by default.

## Resolve using Helper methods

```c#
Connection<DroidGraphType>()
    .Name("Droids")
    .From(dbContext.Droids)
    .ResolveAsync();
```

## Manually Resolving The Field

```c#
Connection<DroidGraphType>()
    .Name("Droids")
    .Paged()
    .ResolveAsync(async context =>
    {
        var request = new ConnectionInput();
        request.SetConnectionInput(context);

        return await dbContext.Droids
            .ToConnection(request, dbContext.Model); // IModel is required for SelectFromContext from Request
    });
```

### Validating the request

You can validate the request outside of `ToConnection` if you want to ensure it's valid in your validation pipeline, the first generic type parameter is the `DbSet` and the second parameter is the resulting type of the request. It needs the `IModel` to determine that the `orderBy` is valid, it doesn't need a `orderBy` if there is primary key it could use instead.

```c#
var validationResult = request.Validate<Human, Clone>(dbContext.Model);
```