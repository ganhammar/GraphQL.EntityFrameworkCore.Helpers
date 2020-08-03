using System;
using System.Collections.Generic;
using System.Linq;
using GraphQL.Language.AST;
using GraphQL.Types;

namespace GraphQL.EntityFrameworkCore.Helpers.Filterable
{
    public static class FilterableInputExtensions
    {
        public static IEnumerable<FilterableInputField> GetApplicableFilterFields(this FilterableInput input, IResolveFieldContext<object> context)
        {
            var filterFields = input.Fields;

            if (context.Path.Count() > 1)
            {
                foreach(var path in context.Path.Skip(1))
                {
                    if (int.TryParse(path, out _) == false)
                    {
                        filterFields = filterFields.FirstOrDefault(x => x.Target.Equals(path, StringComparison.InvariantCultureIgnoreCase))?.Fields;

                        if (filterFields == default)
                        {
                            break;
                        }
                    }
                }
            }

            if (filterFields == default)
            {
                filterFields = input.Fields.Where(x => x.Target == "All");
            }

            return filterFields;
        }

        public static ValidationResult Validate(this FilterableInput input, IResolveFieldContext<object> context)
        {
            var result = new ValidationResult();

            if (input.Fields == default || input.Fields.Any() == false)
            {
                result.IsValid = false;
                result.Failures.Add(new ValidationFailure("Fields", "At least one field is required"));
            }

            var selectedFields = ((Field)context.Operation.SelectionSet.Selections
                .First(x => ((Field)x).Name == context.Path.First())).SelectionSet.Selections;

            ValidateFields(ref result, input.Fields, selectedFields, context);

            return result;
        }

        private static void ValidateFields(ref ValidationResult result, IEnumerable<FilterableInputField> fields, IList<ISelection> selectedFields, IResolveFieldContext<object> context)
        {
            foreach (var field in fields)
            {
                if (field.Target == default)
                {
                    result.IsValid = false;
                    result.Failures.Add(new ValidationFailure("Target", "All fields needs to have an target"));
                }

                if (field.Target != "All")
                {
                    if (selectedFields.Any(x => ((Field)x).Name.Equals(field.Target, StringComparison.InvariantCultureIgnoreCase)) == false)
                    {
                        result.IsValid = false;
                        result.Failures.Add(new ValidationFailure("Target", $"Filtered field '{field.Target}' needs to be included in selection"));
                    }
                }

                if (string.IsNullOrEmpty(field.Value) && (field.Fields == default || field.Fields.Any() == false))
                {
                    result.IsValid = false;
                    result.Failures.Add(new ValidationFailure("Value", "All fields not targeting a data loaded property needs to have a value"));
                }

                if (field.Fields != default && field.Fields.Any())
                {
                    ValidateFields(
                        ref result,
                        field.Fields,
                        selectedFields
                            .Where(x => ((Field)x).Name.Equals(field.Target, StringComparison.InvariantCultureIgnoreCase))
                            .Select(x => ((Field)x).SelectionSet.Selections)
                            .First(),
                        context);
                }
            }
        }
    }
}