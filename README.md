# GraphQL Helpers for EF Core - Connections, Selection from Request and More

Helpers to add bidirectional cursor based paginaton to your endpoints with the `IQueryable` extension method `AsConnection`. The method expects a request that implements `IConnectionInput<T>` as input. Parameters is added to the `ConnectionBuilder` using the extension method `Paged(defaultPageSize)`.

The `Select(IResolveFieldContext context)` extension methods helps you to avoid overfetching data by only selecting the fields requested. Foreign key fields are included by default as the value might be used for data loaded fields. `AsConnection` is using this method per default.

## Getting Started

```
dotnet add package GraphQL.EntityFrameworkCore.Helpers
```

### Configure

```c#
public void ConfigureServices(IServiceCollection services)
{
    services.AddGraphQLEntityFrameworkCoreHelpers(dbContext);
}
```

### Examples

#### Connection
```c#
Connection<DroidGraphType>()
    .Name("Droids")
    .Paged()
    .ResolveAsync(async context =>
    {
        var request = new ConnectionInput();
        request.SetConnectionInput(context);

        return await dbContext.Droids.AsConnection(request);
    });
```

#### Select from Request

```c#
FieldAsync<ListGraphType<HumanGraphType>>(
    "Humans",
    resolve: async context => await dbContext.Humans.Select(context).ToListAsync());
```