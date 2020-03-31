using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace DynamicTyping.Benchmark
{
    [MemoryDiagnoser]
    [SimpleJob(RuntimeMoniker.NetCoreApp31)]
    // [ShortRunJob]
    public class Benchmark2
    {
        private Func<string, string> _dynamic;
        private Dictionary<string, string> _dict;

        [GlobalSetup]
        public void Setup()
        {
            var items = new List<string>
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
                "Parent",
                "Children",
            };
            
            var tree = new RadixTree();
            for (var index = 0; index < items.Count; index++) tree.Add(items[index], index);

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
            _dynamic = (Func<string, string>) Delegate.CreateDelegate(typeof(Func<string, string>), method);

            _dict = new Dictionary<string, string>
            {
                {"Id", "Id"},
                {"Name", "Name"},
                {"DecimalProp1", "DecimalProp1"},
                {"DecimalProp2", "DecimalProp2"},
                {"DecimalProp3", "DecimalProp3"},
                {"DecimalProp4", "DecimalProp4"},
                {"DecimalProp5", "DecimalProp5"},
                {"DecimalProp6", "DecimalProp6"},
                {"DecimalProp7", "DecimalProp7"},
                {"DecimalProp8", "DecimalProp8"},
                {"DecimalProp9", "DecimalProp9"},
                {"StringProp1", "StringProp1"},
                {"StringProp2", "StringProp2"},
                {"StringProp3", "StringProp3"},
                {"StringProp4", "StringProp4"},
                {"StringProp5", "StringProp5"},
                {"StringProp6", "StringProp6"},
                {"StringProp7", "StringProp7"},
                {"StringProp8", "StringProp8"},
                {"StringProp9", "StringProp9"},
                {"Parent", "Parent"},
                {"Children", "Children"},
            };
        }
        
        [Params("DecimalProp2", "Q", "Id")]
        public string Target { get; set; }
        
        [Benchmark]
        public string IfElse()
        {
            if(Target == "Id") return "Id";
            if(Target == "Name") return "Name";
            if(Target == "DecimalProp1") return "DecimalProp1";
            if(Target == "DecimalProp2") return "DecimalProp2";
            if(Target == "DecimalProp3") return "DecimalProp3";
            if(Target == "DecimalProp4") return "DecimalProp4";
            if(Target == "DecimalProp5") return "DecimalProp5";
            if(Target == "DecimalProp6") return "DecimalProp6";
            if(Target == "DecimalProp7") return "DecimalProp7";
            if(Target == "DecimalProp8") return "DecimalProp8";
            if(Target == "DecimalProp9") return "DecimalProp9";
            if(Target == "StringProp1") return "StringProp1";
            if(Target == "StringProp2") return "StringProp2";
            if(Target == "StringProp3") return "StringProp3";
            if(Target == "StringProp4") return "StringProp4";
            if(Target == "StringProp5") return "StringProp5";
            if(Target == "StringProp6") return "StringProp6";
            if(Target == "StringProp7") return "StringProp7";
            if(Target == "StringProp8") return "StringProp8";
            if(Target == "StringProp9") return "StringProp9";
            if(Target == "Parent") return "Parent";
            if(Target == "Children") return "Children";
            
            return "None";
        }    
        
        [Benchmark]
        public string Switch()
        {
            return Target switch
            {
                "Id" => "Id",
                "Name" => "Name",
                "DecimalProp1" => "DecimalProp1",
                "DecimalProp2" => "DecimalProp2",
                "DecimalProp3" => "DecimalProp3",
                "DecimalProp4" => "DecimalProp4",
                "DecimalProp5" => "DecimalProp5",
                "DecimalProp6" => "DecimalProp6",
                "DecimalProp7" => "DecimalProp7",
                "DecimalProp8" => "DecimalProp8",
                "DecimalProp9" => "DecimalProp9",
                "StringProp1" => "StringProp1",
                "StringProp2" => "StringProp2",
                "StringProp3" => "StringProp3",
                "StringProp4" => "StringProp4",
                "StringProp5" => "StringProp5",
                "StringProp6" => "StringProp6",
                "StringProp7" => "StringProp7",
                "StringProp8" => "StringProp8",
                "StringProp9" => "StringProp9",
                "Parent" => "Parent",
                "Children" => "Children",
                _ => "None"
            };
        }

        [Benchmark]
        public string Dictionary()
        {
            return _dict.TryGetValue(Target, out var value) ? value : "None";
        }

        [Benchmark]
        public string Dynamic() => _dynamic(Target);
    }
}