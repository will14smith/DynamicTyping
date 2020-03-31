using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Lokad.ILPack;
using Xunit;
using Xunit.Abstractions;

namespace DynamicTyping.Tests
{
    public class UnitTest2
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public UnitTest2(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }
        
        [Fact]
        public void Test()
        {
            var values = new[]
            {
                "Id",
                "Name",
                "DecimalProp1",
                "DecimalProp2",
                "DecimalProp3",
                "DecimalProp4",
                "DecimalProp5",
                "DecimalProp6",
                "DecimalProp7",
                "DecimalProp8",
                "DecimalProp9",
                "StringProp1",
                "StringProp2",
                "StringProp3",
                "StringProp4",
                "StringProp5",
                "StringProp6",
                "StringProp7",
                "StringProp8",
                "StringProp9",
                "Realllll",
                "ReallllllyLongggggggPropertyyyyyyyyy1",
                "ReallllllyLonggggggg1Propertyyyyyyyyy1",
                "ReallllllyLongggggggPropertyyyyyyyyy2",
                "ReallllllyLonggggggg2Propertyyyyyyyyy1",
                "Parent",
                "Children",
            };
            
            var tree = new RadixTree();
            for (var index = 0; index < values.Length; index++) tree.Add(values[index], index);

            var assemblyName = new AssemblyName($"DynamicTyping{Guid.NewGuid():N}");
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name);

            var typeBuilder = moduleBuilder.DefineType("Person", TypeAttributes.Public | TypeAttributes.Class);
            var methodBuilder = typeBuilder.DefineMethod("Resolve", MethodAttributes.Public | MethodAttributes.Static, typeof(string), new[] {typeof(string)});
            
            RadixTreeCompiler.Build(tree, methodBuilder, (il, isRef) =>
            {
                if (isRef)
                {
                    il.Emit(OpCodes.Ldarga, 0);
                }
                else
                {
                    il.Emit(OpCodes.Ldarg_0);
                }
            }, (il, node) =>
            {
                il.Emit(OpCodes.Ldstr, node.Key);
                il.Emit(OpCodes.Ret);
            }, il =>
            {
                il.Emit(OpCodes.Ldstr, "None");
                il.Emit(OpCodes.Ret);
            });            
            var type = typeBuilder.CreateType();
            var method = type.GetMethod(methodBuilder.Name);

            var path = @"C:\Projects\PactTest\DynamicTyping\DynamicTyping.Tests\bin\Debug\output.dll";
            File.Delete(path);
            new AssemblyGenerator().GenerateAssembly(type.Assembly, path);
            
            foreach (var item in values)
            {
                _testOutputHelper.WriteLine(item + " = " + (string) method.Invoke(null, new object[] { item }));
            }
            
            _testOutputHelper.WriteLine("Invalid = " + (string) method.Invoke(null, new object[] { "Invalid" }));
        }

        private static string Fmt(IEnumerable<ulong> xs) => string.Join(", ", xs.Select(x => $"0x{x:x16}"));
    }
}