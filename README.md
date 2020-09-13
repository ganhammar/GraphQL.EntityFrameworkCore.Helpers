# GraphQL Helpers for EF Core

Adds methods to resolve schema fields directly from a DbContext.

## Getting Started

```
dotnet add package GraphQL.EntityFrameworkCore.Helpers
```

And register GraphTypes to DI:

```c#
services
    .AddGraphQLEntityFrameworkCoreHelpers();
```

## Defining the Schema

To resolve a root query from a DbContext using the helper methods you first need to define from what DbContext and what DbSet and then call either the `ResolveCollectionAsync` method or the `ResolvePropertyAsync` method.

```c#
Field<ListGraphType<HumanGraphType>>()
    .Name("Humans")
    .From(dbContext, x => x.Humans)
    .ResolveCollectionAsync();
```

You can also add connections in a similar way. With the connections the client has the option to define what property/properties should be used to order the connection with. Read more about connections [here](documentation/Connections.md).

```c#
Connection<DroidGraphType>()
    .Name("Droids")
    .From(dbContext, x => x.Droids)
    .ResolveAsync();
```

The helper methods can also be used to resolve data loaded properties. Read more about data loaded fields [here](documentation/DataLoadedFields.md).

```c#
Field<PlanetGraphType, Planet>()
    .Name("HomePlanet")
    .Include(dataLoaderAccessor, dbContext, x => x.HomePlanet)
    .ResolveAsync();
```

### Applying business logic when resolving fields

With all builders you can apply your own business logic to for instance support multi-tenant solutions using the method `Apply`.

```c#
Field<PlanetGraphType, Planet>()
    .Name("HomePlanet")
    .Include(dataLoaderAccessor, dbContext, x => x.HomePlanet)
    .Apply((query, context) =>
    {
        var id = context.GetArgument<int>("id");
        return query.Where(x => x.Id == id);
    })
    .ResolveAsync();
```

### Validating the context

Similarly you can validate the arguments passed to a context using either the `Validate` method or the `ValidateAsync` method.

```c#
Field<PlanetGraphType, Planet>()
    .Name("HomePlanet")
    .Include(dataLoaderAccessor, dbContext, x => x.HomePlanet)
    .ValidateAsync(async context =>
    {
        var result = new ValidationResult();
        var hasAccess = await UserValidator.HasAccessTo(context.GetArgument<int>("id"));

        if (hasAccess == false)
        {
            result.failures.Add(new ValidationFailure("id", "No access"));
        }

        return result;
    }))
    .Apply((query, context) => query.Where(x => x.Id == context.GetArgument<int>("id")))
    .ResolveAsync();
```

## Filters

All collection fields can be filtered by either applying a string to all filterable properties using `Or` or applying specific rules for specific properties. Read more about filters [here](documentation/Filters.md).

## Avoiding Over-Fetching

All helper methods tries to limit the amount data fetched from data store by looking at what was requested, read more about this [here](documentation/SelectFromRequest.md).