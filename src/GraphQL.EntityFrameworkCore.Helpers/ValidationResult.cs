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
        public ValidationFailure(string fieldName, string message, string code = default)
        {
            FieldName = fieldName;
            Message = message;
            Code = code;
        }

        public string FieldName { get; set; }
        public string Message { get; set; }
        public string Code { get; set; }
    }
}