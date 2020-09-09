# Filters

Add the `IsFilterable`-attribute to columns that should be filterable.

```c#
public class Human
{
    public Guid Id { get; set; }

    [IsFilterable]
    public string Name { get; set; }
}
```
Or, mark fields as filterable with the extension method `IsFilterable`. If the name of the field doesn't match the property name this method needs to be used with a `Func` targeting the matching property.

```c#
Field(x => x.Sector)
    .IsFilterable(x => x.Sector)
    .Name("StarSector");
```

Add the argument to the `FieldBuilder` using the extension method `Filtered()` and filter the list with the `IQueryable` extension method `Filter(Context, IModel)`.

```c#
Field<ListGraphType<HumanGraphType>>()
    .Name("Humans")
    .Filtered()
    .ResolveAsync(async context => await dbContext.Humans
        .Filter(context, dbContext.Model)
        .SelectFromContext(context, dbContext.Model)
        .ToListAsync());
```

## Filter input

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

### Validating the filter input

```c#
var validationResult = filterInput.Validate(IResolveFieldContext);
```