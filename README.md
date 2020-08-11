# GraphQL Helpers for EF Core - Connections, Selection from Request and Filtering

Helpers to add bidirectional cursor based paginaton to your endpoints with the `IQueryable` extension method `ToConnection(IConnectionInput<T> request, IModel model)`. The method expects a request that implements `IConnectionInput<T>` as input. Parameters is added to the `ConnectionBuilder` using the extension method `Paged(defaultPageSize)`.

The `SelectFromContext(IResolveFieldContext context, IModel model)` extension methods helps you to avoid overfetching data by only selecting the fields requested. Foreign key fields is included by default as the value might be used for data loaded fields.

With the `Filter(IResolveFieldContext context, IModel model)` extension method you can filter a list of items on specific properties, including any requested data loaded fields. What fields that should be filterable is determined by the `FilterableAttribute` or the `FieldBuilder` extension method `FilterableProperty`.

## Getting Started

```
dotnet add package GraphQL.EntityFrameworkCore.Helpers
```

And register GraphTypes to DI:

```c#
services
    .AddGraphQLEntityFrameworkCoreHelpers();
```

### Examples

#### Connection

Bidirectional cursor based pagination with support for ordering, filtering and selecting based on what is requested.

With the parameter `orderBy` (array of strings, i.e `[ "name", "id" ]`) you can specify in the client what order you want the items in. The order will be ascending by default, you can change it to descending by setting the parameter `isAsc` to `false`.

It is important that each resulting cursor points at a unique row in order to be able to determine what rows to include `before` or `after` a specific row. Therefore the primary key(s) of a certain entity is automaticaly included in the `orderBy` if the asked for order by columns isn't considered unique. Columns considered unique is either the primary key, has a unique constraint or is of type `GUID`, `DATETIME` or `DATETIMEOFFSET`. If you have column that you know are unique but doesn't meet those criterias you can use the `Unique` attribute.

`ToConnection` applies both `SelectFromContext` and `Filter` by default.

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

##### Validating the request

You can validate the request outside of `ToConnection` if you want to ensure it's valid in your validation pipeline, the first generic type parameter is the `DbSet` and the second parameter is the resulting type of the request. It needs the `IModel` to determine that the `orderBy` is valid, it doesn't need a `orderBy` if there is primary key it could use instead.

```c#
var validationResult = request.Validate<Human, Clone>(dbContext.Model);
```

#### Select from Request

`SelectFromContext` applies both `Filter` by default (`Filterable` have to be applied to the Field first).

```c#
FieldAsync<ListGraphType<HumanGraphType>>(
    "Humans",
    resolve: async context => await dbContext.Humans.SelectFromContext(context, dbContext.Model).ToListAsync());
```

#### Filter

Add the `Filterable`-attribute to columns that should be filterable.

```c#
public class Human
{
    public Guid Id { get; set; }

    [Filterable]
    public string Name { get; set; }
}
```
Or, mark fields as filterable with the extension method `FilterableProperty`. If the name of the field doesn't match the property name this method needs to be used with a `Func` targeting the matching property.

```c#
Field(x => x.Sector)
    .FilterableProperty(x => x.Sector)
    .Name("StarSector");
```

Add the argument to the `FieldBuilder` using the extension method `Filterable()` and filter the list with the `IQueryable` extension method `Filter(context)`.

```c#
Field<ListGraphType<HumanGraphType>>()
    .Name("Humans")
    .Filterable()
    .ResolveAsync(async context => await dbContext.Humans
        .Filter(context, dbContext.Model)
        .SelectFromContext(context, dbContext.Model)
        .ToListAsync());
```

##### Filter input

The simplest way to filter a list is by filtering all filterable properties to see if they contain a search term (using Like).

```javascript
{
    "filterInput": {
        "mode": "shallow", // Default (Not required)
        "fields": [
            {
                "target": "all", // Default (Not required)
                "value": "search term",
                "operator": "or", // Default (Not required)
                "valueOperator": "like" // Default (Not required)
            }
        ]
    }
}
```

The input can apply to all requested data loaded properties as well as the main query or just the main query. This is determined by the specified mode, `shallow` to only appply to main or `deep` to apply to all filterable fields.

All fields that is using the `or`-operator is combined into one where-clause where one of them needs to be true, fields that is using the `and`-operator is separated into it's own where-clauses.

The search term can be compared to the field value using the operators: `like`, `notlike`, `equal` or `notequal`.

Below is a more complex query where the main query   is filtered to only include humans that come from the planet Tatooine and has a blue eye color, to also apply this to the data loaded property _homePlanet_ the mode needs to be changed to `deep`.

```graphql
query humans($filterInput: FilterInput) {
    humans(filter: $filterInput) {
        id
        name
        homePlanet {
            id
            name
        }
    }
}
```

```javascript
{
    "filterInput": {
        "mode": "Shallow",
        "fields": [
            {
                "target": "eyeColor",
                "value": "blue",
                "operator": "And"
            },
            {
                "target": "homePlanet",
                "fields": [
                    {
                        "target": "name",
                        "value": "tatooine",
                        "operator": "And"
                    }
                ]
            }
        ]
    }
}
```

##### Validating the filter input

```c#
var validationResult = filterInput.Validate(IResolveFieldContext);
```

#### Working with data loaded fields (navigation properties)

The helper methods can only follow data loaded properties that is loaded through navigation properties (Foreign Keys). If the data loaded field isn't named the same in the schema as the property in the model the `FieldBuilder` extension method `Property(Func)` has to be applied.

```c#
Field<ListGraphType<HumanGraphType>, IEnumerable<Human>>()
    .Name("Residents")
    .Property(x => x.Habitants)
    .ResolveAsync(context =>
    {
        ...
    });
```

### Known issues

The Schema First approach haven't been tested and will likely have some issues configuring. 