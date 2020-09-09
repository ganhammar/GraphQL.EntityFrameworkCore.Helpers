# GraphQL Helpers for EF Core

Adds methods to resolve schema fields directly from dbContext.

## Getting Started

```
dotnet add package GraphQL.EntityFrameworkCore.Helpers
```

And register GraphTypes to DI:

```c#
services
    .AddGraphQLEntityFrameworkCoreHelpers();
```

### Defining Schemas

Fields:

```c#
Field<ListGraphType<HumanGraphType>>()
    .Name("Humans")
    .From(dbContext, x => x.Humans)
    .ResolveCollectionAsync();
```

[Connections](documentation/Connections.md):

```c#
Connection<DroidGraphType>()
    .Name("Droids")
    .From(dbContext, x => x.Droids)
    .ResolveAsync(typeof(ConnectionInput));
```

Add data loaded fields to graphs:

```c#
Field<PlanetGraphType, Planet>()
    .Name("HomePlanet")
    .Include(dataLoaderAccessor, dbContext, x => x.HomePlanet)
    .ResolveAsync();
```