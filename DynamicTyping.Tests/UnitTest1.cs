using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Lokad.ILPack;
using Utf8Json;
using Xunit;
using Xunit.Abstractions;

namespace DynamicTyping.Tests
{
    public class UnitTest1
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private MethodInfo _deserializeMethod;

        public UnitTest1(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public void Test1()
        {
            var fields = new Dictionary<string, Type>
            {
                { "Height", typeof(decimal) },
                { "BirthDate", typeof(DateTime) },
                { "Hats", typeof(List<string>) },
            };

            var assemblyName = new AssemblyName("DynamicTyping1");
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name);

            var typeBuilder = moduleBuilder.DefineType("Person", TypeAttributes.Public | TypeAttributes.Class);
            
            // static fields
            typeBuilder
                .WithField("Id", typeof(Guid))
                .WithField("Name", typeof(string));
            var (_, parentProperty) = typeBuilder.AddIgnoredField("Parent", typeBuilder);
            var (_, childrenProperty) = typeBuilder.AddField("Children", typeof(List<>).MakeGenericType(typeBuilder));
            
            foreach (var (fieldName, fieldType) in fields)
            {
                typeBuilder.WithField(fieldName, fieldType);
            }

            var deserializeMethod = AddDeserializeActualMethod(typeBuilder, typeBuilder.AddDeserializeMethod(), parentProperty, childrenProperty);
            
            var type = typeBuilder.CreateType();
            _deserializeMethod = type.GetMethod(deserializeMethod.Name);
            var resolverType = new RadixResolverBuilder(moduleBuilder).Build(type); 

            var path = @"C:\Projects\PactTest\DynamicTyping\DynamicTyping.Tests\bin\Debug\output2.dll";
            File.Delete(path);
            new AssemblyGenerator().GenerateAssembly(resolverType.Assembly, path);

            
            var input = @"{""Id"":""00000000-0000-0000-0000-000000000001"",""Name"":""A"",""Height"":0,""BirthDate"":""0001-01-01T00:00:00"",""Hats"":[""Bowler""],""Children"":[{""Id"":""00000000-0000-0000-0000-000000000002"",""Name"":null,""Height"":1.5,""BirthDate"":""2001-01-01T00:00:00"",""Hats"":null,""Children"":null}]}";
            var instance = Deserialize(type, Encoding.UTF8.GetBytes(input));
            
            var str = JsonSerializer.ToJsonString(instance);
            _testOutputHelper.WriteLine(str);

            var resolver = (IResolver) Activator.CreateInstance(resolverType);
            _testOutputHelper.WriteLine(resolver.Resolve(instance, "Id").ToString());
            _testOutputHelper.WriteLine(resolver.Resolve(instance, "Missing").ToString());
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

        private object Deserialize(Type type, byte[] input)
        {
            return _deserializeMethod.Invoke(null, new object[] {input});
        }
    }
}