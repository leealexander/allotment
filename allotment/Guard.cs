using Allotment.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Allotment
{
    public static class Guard
    {
        public static T Get<T>(T? propertyValue) => propertyValue ?? throw new Exceptions.UninitializedPropertyException();
        public static T Get<T>(T? propertyValue, string propertyName) => propertyValue ?? throw new Exceptions.UninitializedPropertyException(propertyName);

        public static T Validate<T>(T ?value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            ValidateInternal(value);

            return value;
        }

        public static void ValidateInternal(object value)
        {
            var ctx = new ValidationContext(value);
            var results = new List<ValidationResult>();
            var success = Validator.TryValidateObject(value, ctx, results, validateAllProperties: true);
            var objType = value.GetType();
            if (!success)
            {
                throw new ObjectValidationFailedException(objType, results);
            }

            foreach(var property in objType
                                    .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                                    .Where(x=>x.PropertyType.Namespace != typeof(int).Namespace) // ignore system vars
                                    )
            {
                var o = property.GetValue(value, null);
                if (o != null)
                {
                    ValidateInternal(o);
                }
            }
        }
    }
}
