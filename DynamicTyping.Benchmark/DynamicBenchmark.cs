using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Utf8Json;

namespace DynamicTyping.Benchmark
{
    public class DynamicBenchmark
    {
        private readonly Type _type;
        private readonly IResolver _resolver;
        private readonly object _instance;

        public DynamicBenchmark()
        {
            Type resolverType;
            (_type, resolverType) = CreateType();
            _instance = Deserialize(_type, Program.InputBytes);
            SetParents(_instance);
            _resolver = (IResolver) Activator.CreateInstance(resolverType);
        }
        
        public object Read()
        {
            var a = Deserialize(_type, Program.InputBytes);
            SetParents(a);
            return a;
        }
        public string Write()
        {
            return JsonSerializer.ToJsonString(_instance);
        }
        public ResolveResult[] Resolve()
        {
            return new[]
            {
                _resolver.Resolve(_instance, "Id"),
                _resolver.Resolve(_instance, "DecimalProp7"),
                _resolver.Resolve(_instance, "StringProperty98"),
                _resolver.Resolve(_instance, "NotAProperty"),
            };
        }

        private static (Type Type, Type ResolverType) CreateType()
        {
            var fields = new Dictionary<string, Type>
            {
                { "Name", typeof(string) },
            };

            for(var i = 1; i < 100; i++) fields.Add($"DecimalProp{i}", typeof(decimal));
            for(var i = 1; i < 100; i++) fields.Add($"StringProp{i}", typeof(string));

            var assemblyName = new AssemblyName($"DynamicTyping{Guid.NewGuid():N}");
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name);

            var typeBuilder = moduleBuilder.DefineType("Person", TypeAttributes.Public | TypeAttributes.Class);
            
            // static fields
            typeBuilder
                .WithField("Id", typeof(Guid))
                .WithIgnoredField("Parent", typeBuilder)
                .WithField("Children", typeof(List<>).MakeGenericType(typeBuilder));
            
            foreach (var (fieldName, fieldType) in fields)
            {
                typeBuilder.WithField(fieldName, fieldType);
            }
            
            var type = typeBuilder.CreateType();
            var resolverType = new ResolverBuilder(moduleBuilder).Build(type);

            return (type, resolverType);
        }
        
        private object Deserialize(Type type, byte[] input)
        {
            var formatterType = typeof(IJsonFormatter<>).MakeGenericType(type);
            var deserializeMethod = formatterType.GetMethod(nameof(IJsonFormatter<object>.Deserialize));

            var formatter = JsonSerializer.DefaultResolver.GetFormatterDynamic(type);
            var reader = new JsonReader(input, 0);
            return deserializeMethod.Invoke(formatter, new object[] { reader, JsonSerializer.DefaultResolver } );
        }
        
        private static void SetParents(object instance)
        {
            var children = instance.GetType().GetProperty("Children").GetValue(instance);
            if (children == null) return;

            foreach (var child in (IEnumerable) children)
            {
                if (child == null) continue;
                
                child.GetType().GetProperty("Parent").SetValue(child, instance);
                SetParents(child);
            }
        }
    }
}