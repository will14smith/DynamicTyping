using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using DynamicTyping.Actual;
using Utf8Json;
using Utf8Json.Resolvers;

namespace DynamicTyping.Benchmark
{
    public class ActualBenchmark
    {
        private readonly IResolver _resolver;
        private readonly IProperties<string, object> _instance;
        private readonly Func<byte[], object> _deserializeMethod;

        public ActualBenchmark()
        {
            var (_, deserializeMethod) = CreateType();

            _resolver = new ModelPropertiesResolver();

            _deserializeMethod = deserializeMethod;

            _instance = (IProperties<string, object>) deserializeMethod(Program.InputBytes);
        }
        
        public object Read()
        {
            return Deserialize(Program.InputBytes);
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
        public IReadOnlyCollection<KeyValuePair<string, object>> Enumerate()
        {
            return _instance.ToList();
        }

        private static (Type type, Func<byte[], object> deserialize) CreateType()
        {
            var fields = new Dictionary<string, Type>
            {
                { "Name", typeof(string) },
            };

            for(var i = 1; i < 100; i++) fields.Add($"DecimalProp{i}", typeof(decimal));
            for(var i = 1; i < 100; i++) fields.Add($"StringProp{i}", typeof(string));

            var typeGenerator = new ModelPropertyTypeGenerator();

            var type = typeGenerator.CreateTypeForProperties(fields, Type.EmptyTypes);
            
            var dm = new DynamicMethod("Deserialize", typeof(object), new [] { typeof(byte[]) });
            var il = dm.GetILGenerator();
            
            // var reader = new JsonReader(input, 0);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Newobj, typeof(JsonReader).GetConstructor(new [] { typeof(byte[]), typeof(int) }));

            var readerLocal = il.DeclareLocal(typeof(JsonReader));
            il.Emit(OpCodes.Stloc, readerLocal);
            
            // return Formatter.Deserialize(ref reader, JsonSerializer.DefaultResolver);
            il.EmitCall(OpCodes.Call, typeof(JsonSerializer).GetProperty(nameof(JsonSerializer.DefaultResolver)).GetMethod, null);
            il.EmitCall(OpCodes.Callvirt, typeof(IJsonFormatterResolver).GetMethod(nameof(IJsonFormatterResolver.GetFormatter)).MakeGenericMethod(type), null);
            il.Emit(OpCodes.Ldloca, readerLocal);
            il.EmitCall(OpCodes.Call, typeof(JsonSerializer).GetProperty(nameof(JsonSerializer.DefaultResolver)).GetMethod, null);
            il.EmitCall(OpCodes.Callvirt, typeof(IJsonFormatter<>).MakeGenericType(type).GetMethod(nameof(IJsonFormatter<object>.Deserialize)), null);

            il.Emit(OpCodes.Ret);
            
            var deserialize = (Func<byte[], object>) dm.CreateDelegate(typeof(Func<byte[], object>));

            return (type, deserialize);
        }

        private object Deserialize(byte[] input)
        {
            return _deserializeMethod(input);
        }
    }

    public class ModelPropertiesResolver : IResolver
    {
        public ResolveResult Resolve(object instance, string field)
        {
            if (instance is IReadOnlyProperties<string, object> properties && properties.TryGetValue(field, out var propertyValue))
            {
                return ResolveResult.Resolve(propertyValue);
            }

            return ResolveResult.Unresolved;
        }
    }
}