# Select from Request

`SelectFromContext` applies `Filter` by default (`Filtered` have to be applied to the Field first).

```c#
FieldAsync<ListGraphType<HumanGraphType>>(
    "Humans",
    resolve: async context => await dbContext.Humans.SelectFromContext(context, dbContext.Model).ToListAsync());
```