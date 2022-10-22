namespace Allotment.Exceptions
{
    public sealed class UninitializedPropertyException: Exception
    {
        public UninitializedPropertyException()
            : base($"Uninitialized property")
        {
        }

        public UninitializedPropertyException(string propertyName)
            : base($"Uninitialized property: {propertyName}")
        {

        }
    }
}
