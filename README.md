# GraphQL Helpers for EF Core

![Build Status](https://github.com/ganhammar/GraphQL.EntityFrameworkCore.Helpers/actions/workflows/Main.yml/badge.svg)

[![NuGet](https://img.shields.io/nuget/v/GraphQL.EntityFrameworkCore.Helpers)](https://www.nuget.org/packages/GraphQL.EntityFrameworkCore.Helpers)

[![Coverage Status](https://coveralls.io/repos/github/ganhammar/GraphQL.EntityFrameworkCore.Helpers/badge.svg?branch=main)](https://coveralls.io/github/ganhammar/GraphQL.EntityFrameworkCore.Helpers?branch=main)

Adds methods to resolve schema fields directly from a DbContext. See [sample project](samples/HeadlessCms) for a full setup reference.

## Getting Started

You can install the lastest version via [`NuGet`](https://www.nuget.org/packages/GraphQL.EntityFrameworkCore.Helpers/).

```
> dotnet add package GraphQL.EntityFrameworkCore.Helpers
```

And edit `Startup.cs` to register dependencies by calling method below in `ConfigureServices(IServiceCollection services)`. Passing `DbContext` as type parameter is optional, if it isn't passed here it would need to be passed when defining the schema fields.

```c#
public void ConfigureServices(IServiceCollection services)
{
    services
        .AddGraphQLEntityFrameworkCoreHelpers<AppDbContext>();
}
```

## Defining the Schema

To resolve a root query from the DbContext using the helper methods you first need to call the `From` method with the `DbSet` to be included and then call either `ResolveCollectionAsync` for a list graph or `ResolvePropertyAsync` for a non list graph.

```c#
Field<ListGraphType<HumanGraphType>>()
    .Name("Humans")
    .From(dbContext.Humans)
    .ResolveCollectionAsync();
```

You can also add connections in a similar way. With the connections the client has the option to define what property/properties should be used to order the connection. Read more about connections [here](documentation/Connections.md).

```c#
Connection<DroidGraphType>()
    .Name("Droids")
    .From(dbContext.Droids)
    .ResolveAsync();
```

The helper methods can also be used to resolve data loaded properties. Read more about data loaded fields [here](documentation/DataLoadedFields.md).

```c#
public class HumanGraphType : ObjectGraphType<Human>
{
    public HumanGraphType()
    {
        Field<PlanetGraphType, Planet>()
            .Name("HomePlanet")
            .Include(x => x.HomePlanet)
            .ResolveAsync();
    }
}
```

### Applying business logic when resolving fields

With all builders you can apply your own business logic to for instance support authorization scenarios by calling the method `Where`.

```c#
Field<HumanGraphType, Human>()
    .Name("BestFriend")
    .Argument<NonNullGraphType<IdGraphType>>("HumanId")
    .Include(x => x.Friends)
    .Where(context =>
    {
        var humanId = context.GetArgument<int>("HumanId");
        return x => x.BestFriendId == humanId;
    })
    .ResolveAsync();
```

### Validating the context

Similarly you can validate the arguments passed to a context using either `Validate` or `ValidateAsync`.

```c#
Field<PlanetGraphType, Planet>()
    .Name("HomePlanet")
    .Include(dbContext, x => x.HomePlanet)
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