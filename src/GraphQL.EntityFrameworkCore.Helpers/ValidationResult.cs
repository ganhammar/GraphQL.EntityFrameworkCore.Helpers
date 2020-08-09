using System.Collections.Generic;
using System.Linq;

namespace GraphQL.EntityFrameworkCore.Helpers
{
    public class ValidationResult
    {
        public List<ValidationFailure> Failures { get; set; } = new List<ValidationFailure>();
        public bool IsValid { get => Failures.Any() == false; }
    }

    public class ValidationFailure
    {
        public ValidationFailure(string fieldName, string message)
        {
            FieldName = fieldName;
            Message = message;
        }

        public string FieldName { get; set; }
        public string Message { get; set; }
    }
}