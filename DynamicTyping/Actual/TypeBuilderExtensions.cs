using System;
using System.Reflection.Emit;

namespace DynamicTyping.Actual
{
    public static class TypeBuilderExtensions
    {
        public static TypeBuilder DefineUniqueType(this ModuleBuilder builder, string name)
        {
            var randomId = Guid.NewGuid().ToString("N").Substring(0, 7);
            return builder.DefineType($"{name}_{randomId}");
        }
    }
}