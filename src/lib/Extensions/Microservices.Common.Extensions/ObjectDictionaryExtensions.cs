using System;
using System.Collections.Generic;
using System.Linq;

namespace Microservices.Common.Extensions
{
    public static class ObjectDictionaryExtensions
    {
        public static Dictionary<string, string> ObjectToDictionary(this object obj)
        {
            return obj.GetType()
                .GetProperties()
                .ToDictionary(
                    x => x.Name,
                    x => (x.GetGetMethod().Invoke(obj, null)?.ToString() ?? string.Empty)
                );
        }
    }
}
