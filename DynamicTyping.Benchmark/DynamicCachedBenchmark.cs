using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Utf8Json;

namespace DynamicTyping.Benchmark
{
    public class DynamicCachedBenchmark
    {
        private readonly IResolver _resolver;
        private readonly object _instance;
        private readonly Func<byte[], object> _deserializeMethod;

        public DynamicCachedBenchmark()
        {
            var (type, resolverType, deserializeMethod) = CreateType();

            _resolver = (IResolver) Activator.CreateInstance(resolverType);

            _deserializeMethod = (Func<byte[], object>) Delegate.CreateDelegate(typeof(Func<byte[], object>), deserializeMethod);

            _instance = Deserialize(Program.InputBytes);
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

        private static (Type Type, Type ResolverType, MethodInfo DeserializeMethod) CreateType()
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
                .WithField("Id", typeof(Guid));
            var (_, parentProperty) = typeBuilder.AddIgnoredField("Parent", typeBuilder);
            var (_, childrenProperty) = typeBuilder.AddField("Children", typeof(List<>).MakeGenericType(typeBuilder));
            
            foreach (var (fieldName, fieldType) in fields)
            {
                typeBuilder.WithField(fieldName, fieldType);
            }

            var innerDeserializeMethod = typeBuilder.AddDeserializeMethod();
            var deserializeMethod = AddDeserializeActualMethod(typeBuilder, innerDeserializeMethod, parentProperty, childrenProperty);
            
            var type = typeBuilder.CreateType();
            var resolverType = new RadixResolverBuilder(moduleBuilder).Build(type);
            
            return (type, resolverType, type.GetMethod(deserializeMethod.Name));
        }

        private static MethodBuilder AddDeserializeActualMethod(TypeBuilder typeBuilder, MethodInfo innerDeserializeMethod, PropertyInfo parentProperty, PropertyInfo childrenProperty)
        {
            var setParentsMethod = typeBuilder.DefineMethod("SetParents", MethodAttributes.Private | MethodAttributes.Static, typeBuilder, new[] {typeBuilder});
            var setParentsMethodIL = setParentsMethod.GetILGenerator();

            // var children = arg0.Children;
            var childrenLoc = setParentsMethodIL.DeclareLocal(typeof(List<>).MakeGenericType(typeBuilder));
            setParentsMethodIL.Emit(OpCodes.Ldarg_0);
            setParentsMethodIL.EmitCall(OpCodes.Call, childrenProperty.GetMethod, null);
            setParentsMethodIL.Emit(OpCodes.Stloc, childrenLoc);
            
            // if (children == null) return arg0
            var childrenNotNullLabel = setParentsMethodIL.DefineLabel();
            setParentsMethodIL.Emit(OpCodes.Ldloc, childrenLoc);
            setParentsMethodIL.Emit(OpCodes.Brtrue, childrenNotNullLabel);
            setParentsMethodIL.Emit(OpCodes.Ldarg_0);
            setParentsMethodIL.Emit(OpCodes.Ret);
            setParentsMethodIL.MarkLabel(childrenNotNullLabel);

            // var count = children.Count
            var countLoc = setParentsMethodIL.DeclareLocal(typeof(int));
            setParentsMethodIL.Emit(OpCodes.Ldloc, childrenLoc);
            setParentsMethodIL.EmitCall(OpCodes.Call, TypeBuilder.GetMethod(typeof(List<>).MakeGenericType(typeBuilder), typeof(List<>).GetProperty(nameof(List<object>.Count)).GetMethod), null);
            setParentsMethodIL.Emit(OpCodes.Stloc, countLoc);

            // var i = 0
            var iLoc = setParentsMethodIL.DeclareLocal(typeof(int));
            setParentsMethodIL.Emit(OpCodes.Ldc_I4_0);
            setParentsMethodIL.Emit(OpCodes.Stloc, iLoc);
            var conditionLabel = setParentsMethodIL.DefineLabel();
            setParentsMethodIL.Emit(OpCodes.Br, conditionLabel);
            // loop:
            var loopLabel = setParentsMethodIL.DefineLabel();
            var incLabel = setParentsMethodIL.DefineLabel();
            setParentsMethodIL.MarkLabel(loopLabel);
            //   var child = children[i];
            var childLoc = setParentsMethodIL.DeclareLocal(typeBuilder);
            setParentsMethodIL.Emit(OpCodes.Ldloc, childrenLoc);
            setParentsMethodIL.Emit(OpCodes.Ldloc, iLoc);
            setParentsMethodIL.EmitCall(OpCodes.Call, TypeBuilder.GetMethod(typeof(List<>).MakeGenericType(typeBuilder), typeof(List<>).GetProperty("Item").GetMethod), null);
            setParentsMethodIL.Emit(OpCodes.Stloc, childLoc);
            //   if (child == null) goto inc
            setParentsMethodIL.Emit(OpCodes.Ldloc, childLoc);
            setParentsMethodIL.Emit(OpCodes.Brfalse, incLabel);
            //   child.Parent = arg0
            setParentsMethodIL.Emit(OpCodes.Ldloc, childLoc);
            setParentsMethodIL.Emit(OpCodes.Ldarg_0);
            setParentsMethodIL.EmitCall(OpCodes.Call, parentProperty.SetMethod, null);
            //   _ = SetParents(child)
            setParentsMethodIL.Emit(OpCodes.Ldloc, childLoc);
            setParentsMethodIL.EmitCall(OpCodes.Call, setParentsMethod, null);
            setParentsMethodIL.Emit(OpCodes.Pop);
            // inc: i++
            setParentsMethodIL.MarkLabel(incLabel);
            setParentsMethodIL.Emit(OpCodes.Ldloc, iLoc);
            setParentsMethodIL.Emit(OpCodes.Ldc_I4_1);
            setParentsMethodIL.Emit(OpCodes.Add);
            setParentsMethodIL.Emit(OpCodes.Stloc, iLoc);
            // cond: if(i < count) goto loop
            setParentsMethodIL.MarkLabel(conditionLabel);
            setParentsMethodIL.Emit(OpCodes.Ldloc, iLoc);
            setParentsMethodIL.Emit(OpCodes.Ldloc, countLoc);
            setParentsMethodIL.Emit(OpCodes.Clt);
            setParentsMethodIL.Emit(OpCodes.Brtrue, loopLabel);

            // return arg0
            setParentsMethodIL.Emit(OpCodes.Ldarg_0);
            setParentsMethodIL.Emit(OpCodes.Ret);

            var deserializeActualMethod = typeBuilder.DefineMethod("DeserializeActual", MethodAttributes.Public | MethodAttributes.Static, typeBuilder, new[] {typeof(byte[])});
            var deserializeActualMethodIL = deserializeActualMethod.GetILGenerator();

            deserializeActualMethodIL.Emit(OpCodes.Ldarg_0);
            deserializeActualMethodIL.EmitCall(OpCodes.Call, innerDeserializeMethod, null);
            deserializeActualMethodIL.EmitCall(OpCodes.Call, setParentsMethod, null);
            deserializeActualMethodIL.Emit(OpCodes.Ret);
            
            return deserializeActualMethod;
        }

        private object Deserialize(byte[] input)
        {
            return _deserializeMethod(input);
        }
    }
}