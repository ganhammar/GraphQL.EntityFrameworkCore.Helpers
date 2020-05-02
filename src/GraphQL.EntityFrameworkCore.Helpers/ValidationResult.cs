using System.Collections.Generic;

namespace GraphQL.EntityFrameworkCore.Helpers
{
    public class ValidationResult
    {
        public bool IsValid { get; set; } = true;
        public List<ValidationFailure> Failures { get; set; } = new List<ValidationFailure>();
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