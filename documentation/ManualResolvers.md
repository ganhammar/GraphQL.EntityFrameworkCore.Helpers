## Working with data loaded fields (navigation properties)

The helper methods can only follow data loaded properties that is loaded through navigation properties (Foreign Keys). If the data loaded field isn't named the same in the schema as the property in the model the `FieldBuilder` extension method `MapsTo(Func)` has to be applied.

```c#
Field<ListGraphType<HumanGraphType>, IEnumerable<Human>>()
    .Name("Residents")
    .MapsTo(x => x.Habitants)
    .ResolveAsync(context =>
    {
        ...
    });
```

In some cases, especially with many-to-many relationships, you might want to skip one level in the hierarchy and use a childs child property, that can be done using the method `ThenTo(Func)`, which can be applied after `MapsTo(Func)`. See sample project for example of this.