# Working with data loaded fields (navigation properties)

The helper methods can only follow data loaded properties that is loaded through navigation properties (Foreign Keys). For this reason EF Core 5.0's feature to map many-to-many relationships without mapping the join table isn't supported, the join table needs to be mapped.

## Setup

To inform the helper methods what field you want to resolve you'll have to use the `MapsTo` method or pass the property as a func to the `Include` method.

```c#
Field<PlanetGraphType, Planet>()
    .Name("HomePlanet")
    .Include(dbContext, x => x.HomePlanet)
    .Apply((query, context) => query.Where(x => true))
    .ResolveAsync();
```

Or:

```c#
Field<PlanetGraphType, Planet>()
    .Name("HomePlanet")
    .MapsTo(x => x.HomePlanet)
    .Include(dbContext)
    .Apply((query, context) => query.Where(x => true))
    .ResolveAsync();
```

## Many-to-Many relationships

In some cases, especially with many-to-many relationships, you might want to skip one level in the hierarchy and use a childs child property, that can be done using the method `ThenTo(Func)`, which can be applied after `MapsTo(Func)`. See sample project for example of how this can be used.

```c#
Field<ListGraphType<TagGraphType>, IEnumerable<Tag>>()
    .Name("Tags")
    .MapsTo(x => x.PageTags)
        .ThenTo(x => x.Page)
    .Include(dbContext)
    .ResolveAsync();
```

## Limitations

Relationships with composite keys cannot be resolved using the helper methods and would be needed to be resolved manually.

```c#
Field<ListGraphType<HumanGraphType>, IEnumerable<Human>>()
    .Name("Residents")
    .MapsTo(x => x.Habitants)
    .ResolveAsync(context =>
    {
        ...
    });
```