using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Microservices.Common.Extensions
{
    public static class EnumHelper
    {
        public static string GetEnumDescription(Enum value)
        {
            var fi = value.GetType().GetField(value.ToString());

            var attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);

            return attributes.Length > 0 ? attributes[0].Description : value.ToString();
        }

    }
    public static class EnumExtensions
    {
        public static string GetEnumDescription(this Enum value) => EnumHelper.GetEnumDescription(value);
    }
}
