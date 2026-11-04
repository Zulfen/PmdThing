using System.Reflection;

namespace PmdThing.Core.Util
{
    public static class PmdTypeUtils
    {

        public static bool IsGenericClass(Type type, Type genericType)
        {
            if (type.BaseType?.IsGenericType == true)
            {
                return type.BaseType?.GetGenericTypeDefinition() == genericType;
            }
            if (type.IsGenericType)
            {
                return type.GetGenericTypeDefinition() == genericType;
            }

            return false;
        }

        // Source - https://stackoverflow.com/a/1750024
        // Posted by Philip Wallace, modified by community. See post 'Timeline' for change history
        // Retrieved 2026-09-29, License - CC BY-SA 2.5
        public static bool IsNumericType(this object obj)
        {
            switch (Type.GetTypeCode(obj.GetType()))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return true;
                case TypeCode.Empty:
                case TypeCode.Object:
                case TypeCode.DBNull:
                case TypeCode.Boolean:
                case TypeCode.Char:
                case TypeCode.DateTime:
                case TypeCode.String:
                    break;
                default:
                    return false;
            }

            return false;
        }


    }
}
