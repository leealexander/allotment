using System.ComponentModel.DataAnnotations;

namespace Allotment.Exceptions
{
    public sealed class ObjectValidationFailedException : Exception
    {
        public ObjectValidationFailedException(Type t, IEnumerable<ValidationResult> results)
            : base($"Validation failed for {t.Name}, reasons: {string.Join(", ", results.Select(x=>x.ToString()))}")
        {
        }
    }
}
