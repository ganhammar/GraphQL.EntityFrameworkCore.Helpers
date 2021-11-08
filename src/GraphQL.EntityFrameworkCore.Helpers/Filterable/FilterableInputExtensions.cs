using System;
using System.Collections.Generic;
using System.Linq;
using GraphQL.Language.AST;

namespace GraphQL.EntityFrameworkCore.Helpers
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
                    if (int.TryParse(path.ToString(), out _) == false && new[] { "edges", "node" }.Contains(path.ToString().ToLowerInvariant()) == false)
                    {
                        filterFields = filterFields.FirstOrDefault(x => x.Target.Equals(path.ToString(), StringComparison.InvariantCultureIgnoreCase))?.Fields;

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
                result.Failures.Add(new ValidationFailure("Fields", "At least one field is required"));
                return result;
            }

            var selectedFields = GetSelection(
                ref result,
                context.Operation.SelectionSet.Selections,
                context.Path.First().ToString(),
                context);

            ValidateFields(ref result, input.Fields, selectedFields, context);

            return result;
        }

        private static IList<ISelection> GetSelection(ref ValidationResult result, IList<ISelection> selections, string target, IResolveFieldContext<object> context)
        {
            var selectedFields = ((Field)FindField(target, selections, context)).SelectionSet.Selections;

            if (selectedFields.Any(x => ((Field)x).Name.Equals("edges", StringComparison.InvariantCultureIgnoreCase)))
            {
                selectedFields = ((Field)selectedFields
                    .First(x => ((Field)x).Name.Equals("edges", StringComparison.InvariantCultureIgnoreCase)))
                    .SelectionSet.Selections;

                if (selectedFields.Any(x => ((Field)x).Name.Equals("node", StringComparison.InvariantCultureIgnoreCase)) == false)
                {
                    result.Failures.Add(new ValidationFailure("Fields", "No selections for connection found"));
                }

                selectedFields = ((Field)selectedFields
                    .First(x => ((Field)x).Name.Equals("node", StringComparison.InvariantCultureIgnoreCase)))
                    .SelectionSet.Selections;
            }

            return selectedFields;
        }

        private static ISelection FindField(string name, IList<ISelection> selectedFields, IResolveFieldContext<object> context)
        {
            var field = selectedFields.FirstOrDefault(x => x is Field && ((Field)x).Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));

            if (field == default && selectedFields.Any(x => x is FragmentSpread))
            {
                foreach (var selection in selectedFields.Where(x => x is FragmentSpread))
                {
                    var fragmentSelection = context.Document.Fragments
                        .First(y => y.Name == ((FragmentSpread)selection).Name).SelectionSet.Selections;

                    field = fragmentSelection.FirstOrDefault(y => y is Field && ((Field)y).Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));

                    if (field == default && fragmentSelection.Any(y => y is FragmentSpread))
                    {
                        field = FindField(name, fragmentSelection, context);
                    }
                }
            }

            return field;
        }

        private static void ValidateFields(ref ValidationResult result, IEnumerable<FilterableInputField> fields, IList<ISelection> selectedFields, IResolveFieldContext<object> context)
        {
            foreach (var field in fields)
            {
                if (field.Target == default)
                {
                    result.Failures.Add(new ValidationFailure("Target", "All fields needs to have an target"));
                }

                if (field.Target != "All" && FindField(field.Target, selectedFields, context) == default)
                {
                    result.Failures.Add(new ValidationFailure("Target", $"Filtered field '{field.Target}' needs to be included in selection"));
                }

                if (string.IsNullOrEmpty(field.Value) && (field.Fields == default || field.Fields.Any() == false))
                {
                    result.Failures.Add(new ValidationFailure("Value", "All fields not targeting a data loaded property needs to have a value"));
                }

                if (field.Fields != default && field.Fields.Any())
                {
                    ValidateFields(
                        ref result,
                        field.Fields,
                        GetSelection(ref result, selectedFields, field.Target, context),
                        context);
                }
            }
        }
    }
}