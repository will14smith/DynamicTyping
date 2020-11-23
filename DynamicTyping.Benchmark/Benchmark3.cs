using System;
using System.Reflection.Emit;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;

namespace DynamicTyping.Benchmark
{
    [MemoryDiagnoser]
    [SimpleJob(RuntimeMoniker.NetCoreApp31)]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByJob)]
    public class Benchmark3
    {
        private Type _type;
        private Func<object> _compiledFactory;

        [GlobalSetup]
        public void Setup()
        {
            _type = typeof(Static);
            
            var method = new DynamicMethod("Factory", typeof(object), Type.EmptyTypes);
            var il = method.GetILGenerator();

            il.Emit(OpCodes.Newobj, _type.GetConstructor(Type.EmptyTypes));
            il.Emit(OpCodes.Ret);
            
            _compiledFactory = (Func<object>) method.CreateDelegate(typeof(Func<object>));
        }

        [Benchmark]
        public object ActivatorCreateInstance() => Activator.CreateInstance(_type);
        
        [Benchmark]
        public object CompiledFactory() => _compiledFactory();
    }
}