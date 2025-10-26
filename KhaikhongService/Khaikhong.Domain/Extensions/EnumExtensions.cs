using System.ComponentModel;
using System.Reflection;

namespace Khaikhong.Domain.Extensions
{
    public static class EnumExtensions
    {
        public static string GetDescription<T>(this T value) where T : Enum
        {
            FieldInfo? fieldInfo = value.GetType().GetField(value.ToString());
            if (fieldInfo != null)
            {
                DescriptionAttribute[] attributes = (DescriptionAttribute[])fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false);
                if (attributes != null && attributes.Length > 0)
                {
                    return attributes[0].Description;
                }
            }
            return value.ToString();
        }

        public static T ToEnumFromDescription<T>(this string description) where T : struct, Enum
        {
            foreach (var value in Enum.GetValues<T>().Cast<T>())
            {
                var desc = value.GetDescription();
                if (desc.Equals(description, StringComparison.OrdinalIgnoreCase) ||
                    value.ToString().Equals(description, StringComparison.OrdinalIgnoreCase))
                {
                    return value;
                }
            }

            return default;
        }
    }
}