using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using Utf8Json;

namespace DynamicTyping
{
    public interface IResolver
    {
        ResolveResult Resolve(object instance, string field);
    }

    public struct ResolveResult
    {
        public static readonly ResolveResult Unresolved = new ResolveResult { Resolved = false };
        public static ResolveResult Resolve(object result) => new ResolveResult { Resolved = true, Result = result };
        
        public bool Resolved { get; private set; }
        public object Result { get; private set; }

        public override string ToString()
        {
            return Resolved ? $"Resolved: {Result}" : "Unresolved";
        }
    }
    
    public struct ResolveResult<T>
    {
        public static readonly ResolveResult<T> Unresolved = new ResolveResult<T> { Resolved = false };
        public static ResolveResult<T> Resolve(T result) => new ResolveResult<T> { Resolved = true, Result = result };
        
        public bool Resolved { get; private set; }
        public T Result { get; private set; }

        public override string ToString()
        {
            return Resolved ? $"Resolved: {Result}" : "Unresolved";
        }
    }


    public class ResolverBuilder
    {
        private readonly ModuleBuilder _moduleBuilder;

        public ResolverBuilder(ModuleBuilder moduleBuilder)
        {
            _moduleBuilder = moduleBuilder;
        }

        public Type Build(Type targetType)
        {
            var resolverType = _moduleBuilder.DefineType($"{targetType}Resolver");
            resolverType.AddInterfaceImplementation(typeof(IResolver));

            // ResolveResult Resolve(object instance, string field)
            var resolveMethod = resolverType.DefineMethod(nameof(IResolver.Resolve), MethodAttributes.Public | MethodAttributes.Virtual, typeof(ResolveResult), new[] {typeof(object), typeof(string)});
            var resolveIL = resolveMethod.GetILGenerator();

            // if(!(instance is targetType)) return Unresolved
            var unresolvedLabel = resolveIL.DefineLabel();
            resolveIL.Emit(OpCodes.Ldarg_1);
            resolveIL.Emit(OpCodes.Isinst, targetType);
            resolveIL.Emit(OpCodes.Brfalse, unresolvedLabel);
            
            var equalsMethod = typeof(string).GetMethod(nameof(string.Equals), new[] { typeof(string), typeof(string) });
            
            foreach (var property in targetType.GetProperties())
            {
                // if (field == property.Name) return ResolveResult.Resolve(instance[property])
                var endTarget = resolveIL.DefineLabel();
                
                resolveIL.Emit(OpCodes.Ldstr, property.Name);
                resolveIL.Emit(OpCodes.Ldarg_2);
                resolveIL.EmitCall(OpCodes.Call, equalsMethod, null);
                resolveIL.Emit(OpCodes.Brfalse, endTarget);
                
                resolveIL.Emit(OpCodes.Ldarg_1);
                resolveIL.EmitCall(OpCodes.Call, property.GetMethod, null);
                if (property.PropertyType.IsValueType)
                {
                    resolveIL.Emit(OpCodes.Box, property.PropertyType);
                }
                resolveIL.EmitCall(OpCodes.Call, typeof(ResolveResult).GetMethod(nameof(ResolveResult.Resolve), new [] { typeof(object) }), null);
                resolveIL.Emit(OpCodes.Ret);

                resolveIL.MarkLabel(endTarget);
            }
            
            resolveIL.MarkLabel(unresolvedLabel);
            resolveIL.Emit(OpCodes.Ldsfld, typeof(ResolveResult).GetField(nameof(ResolveResult.Unresolved)));
            resolveIL.Emit(OpCodes.Ret);
            
            return resolverType.CreateTypeInfo();
        }
    } 
    
    public static class TypeBuilderExtensions
    {
        public static (FieldBuilder, PropertyBuilder) AddField(this TypeBuilder builder, string name, Type type)
        {
            var fieldBuilder = builder.DefineField($"_{name}", type, FieldAttributes.Private);
            var propertyBuilder = builder.DefineProperty(name, PropertyAttributes.None, type, null);

            const MethodAttributes propertyMethodAttributes = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig;
                
            var getterBuilder = builder.DefineMethod($"Get_{name}", propertyMethodAttributes, type, Type.EmptyTypes);
            var getterIL = getterBuilder.GetILGenerator();
            getterIL.Emit(OpCodes.Ldarg_0);
            getterIL.Emit(OpCodes.Ldfld, fieldBuilder);
            getterIL.Emit(OpCodes.Ret);
            propertyBuilder.SetGetMethod(getterBuilder);
                
            var setterBuilder = builder.DefineMethod($"Set_{name}", propertyMethodAttributes, null, new [] { type });
            var setterIL = setterBuilder.GetILGenerator();
            setterIL.Emit(OpCodes.Ldarg_0);
            setterIL.Emit(OpCodes.Ldarg_1);
            setterIL.Emit(OpCodes.Stfld, fieldBuilder);
            setterIL.Emit(OpCodes.Ret);
            propertyBuilder.SetSetMethod(setterBuilder);

            return (fieldBuilder, propertyBuilder);
        }

        public static (FieldBuilder, PropertyBuilder) AddIgnoredField(this TypeBuilder builder, string name, Type type)
        {
            var (field, property) = AddField(builder, name, type);
            property.SetCustomAttribute(new CustomAttributeBuilder(typeof(IgnoreDataMemberAttribute).GetConstructor(Type.EmptyTypes), new object[0]));

            return (field, property);
        }

        public static TypeBuilder WithField(this TypeBuilder builder, string name, Type type)
        {
            builder.AddField(name, type);
            return builder;
        }  
        
        public static TypeBuilder WithIgnoredField(this TypeBuilder builder, string name, Type type)
        {
            builder.AddIgnoredField(name, type);
            return builder;
        }

        public static MethodInfo AddDeserializeMethod(this TypeBuilder builder)
        {
            // private static readonly IJsonFormatter<type> Formatter;
            var formatterField = builder.DefineField("Formatter", typeof(IJsonFormatter<>).MakeGenericType(builder), FieldAttributes.Private | FieldAttributes.Static | FieldAttributes.InitOnly);
            
            // static type() { Formatter = JsonSerializer.DefaultResolver.GetFormatterDynamic(type) }
            var typeInitializer = builder.DefineTypeInitializer();
            var typeInitializerIL = typeInitializer.GetILGenerator();
            typeInitializerIL.EmitCall(OpCodes.Call, typeof(JsonSerializer).GetProperty(nameof(JsonSerializer.DefaultResolver)).GetMethod, null);
            typeInitializerIL.EmitCall(OpCodes.Callvirt, typeof(IJsonFormatterResolver).GetMethod(nameof(IJsonFormatterResolver.GetFormatter)).MakeGenericMethod(builder), null);
            typeInitializerIL.Emit(OpCodes.Stsfld, formatterField);
            typeInitializerIL.Emit(OpCodes.Ret);

            // public static type Deserialize(byte[] input)
            var method = builder.DefineMethod("Deserialize", MethodAttributes.Public | MethodAttributes.Static, builder, new[] {typeof(byte[])});
            var methodIL = method.GetILGenerator();
            
            // var reader = new JsonReader(input, 0);
            methodIL.Emit(OpCodes.Ldarg_0);
            methodIL.Emit(OpCodes.Ldc_I4_0);
            methodIL.Emit(OpCodes.Newobj, typeof(JsonReader).GetConstructor(new [] { typeof(byte[]), typeof(int) }));

            var readerLocal = methodIL.DeclareLocal(typeof(JsonReader));
            methodIL.Emit(OpCodes.Stloc, readerLocal);
            
            // return Formatter.Deserialize(ref reader, JsonSerializer.DefaultResolver);
            methodIL.Emit(OpCodes.Ldsfld, formatterField);
            methodIL.Emit(OpCodes.Ldloca, readerLocal);
            methodIL.EmitCall(OpCodes.Call, typeof(JsonSerializer).GetProperty(nameof(JsonSerializer.DefaultResolver)).GetMethod, null);
            methodIL.EmitCall(OpCodes.Callvirt, TypeBuilder.GetMethod(typeof(IJsonFormatter<>).MakeGenericType(builder), typeof(IJsonFormatter<>).GetMethod(nameof(IJsonFormatter<object>.Deserialize))), null);

            methodIL.Emit(OpCodes.Ret);
            
            return method;
        }
    }
}