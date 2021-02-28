# Working with data loaded fields

To resolve data loaded fields with the helper methods you call the method `Include` on the field with the property to resolve from the base type. In below example the base type is `Human` which has a property called `HomePlanet` that we want to make available in the schema as a data loaded field.

```c#
Field<PlanetGraphType, Planet>()
    .Name("HomePlanet")
    .Include(x => x.HomePlanet)
    .Apply((query, context) => query.Where(x => true))
    .ResolveAsync();
```

If the `DbContext` isn't passed when registering the helper methods in `Startup` or if you want to resolve the field from a different context you pass the `DbContext` as a argument:

```c#
Field<PlanetGraphType, Planet>()
    .Name("HomePlanet")
    .Include(dbContext, x => x.HomePlanet)
    .Apply((query, context) => query.Where(x => true))
    .ResolveAsync();
```