# Select from Request

With Select from Request you avoid overfetching data by only reading the requested for columns or any navigation property (assuming that they will be used to resolve a data loaded field).

`SelectFromContext` applies `Filter` by default (`Filtered` have to be applied to the Field first).

```c#
FieldAsync<ListGraphType<HumanGraphType>>(
    "Humans",
    resolve: async context => await dbContext.Humans.SelectFromContext(context, dbContext.Model).ToListAsync());
```