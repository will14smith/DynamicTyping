using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;

namespace DynamicTyping.Benchmark
{
    [MemoryDiagnoser]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    [CategoriesColumn]
    [ShortRunJob]
    // [SimpleJob(RuntimeMoniker.NetCoreApp31)]
    public class Benchmark
    {
        private StaticBenchmark _static;
        private DictBenchmark _dict;
        private DynamicBenchmark _dynamic;
        private DynamicCachedBenchmark _dynamicCached;
        private ActualBenchmark _actual;
        
        [GlobalSetup]
        public void Setup()
        {
            _static = new StaticBenchmark();
            _dict = new DictBenchmark();
            _dynamic = new DynamicBenchmark();
            _dynamicCached = new DynamicCachedBenchmark();
            _actual = new ActualBenchmark();
        }
    
        [BenchmarkCategory("Read"), Benchmark(Baseline = true)]
        public Static StaticRead() => _static.Read();
        [BenchmarkCategory("Write"), Benchmark(Baseline = true)]
        public string StaticWrite() => _static.Write();
        [BenchmarkCategory("Resolve"), Benchmark(Baseline = true)]
        public ResolveResult[] StaticResolve() => _static.Resolve();
        
        [BenchmarkCategory("Read"), Benchmark]
        public object DictRead() => _dict.Read();
        [BenchmarkCategory("Write"), Benchmark]
        public string DictWrite() => _dict.Write();
        [BenchmarkCategory("Resolve"), Benchmark]
        public ResolveResult[] DictResolve() => _dict.Resolve();
        [BenchmarkCategory("Enumerate"), Benchmark]
        public IReadOnlyCollection<KeyValuePair<string, object>> DictEnumerate() => _dict.Enumerate();

        [BenchmarkCategory("Read"), Benchmark]
        public object DynamicRead() => _dynamic.Read();
        [BenchmarkCategory("Write"), Benchmark]
        public string DynamicWrite() => _dynamic.Write();
        [BenchmarkCategory("Resolve"), Benchmark]
        public ResolveResult[] DynamicResolve() => _dynamic.Resolve();
        
        [BenchmarkCategory("Read"), Benchmark]
        public object DynamicCachedRead() => _dynamicCached.Read();
        [BenchmarkCategory("Write"), Benchmark]
        public string DynamicCachedWrite() => _dynamicCached.Write();
        [BenchmarkCategory("Resolve"), Benchmark]
        public ResolveResult[] DynamicCachedResolve() => _dynamicCached.Resolve();
        
        [BenchmarkCategory("Read"), Benchmark]
        public object ActualRead() => _actual.Read();
        [BenchmarkCategory("Write"), Benchmark]
        public string ActualWrite() => _actual.Write();
        [BenchmarkCategory("Resolve"), Benchmark]
        public ResolveResult[] ActualResolve() => _actual.Resolve();
        [BenchmarkCategory("Enumerate"), Benchmark]
        public IReadOnlyCollection<KeyValuePair<string, object>> ActualEnumerate() => _actual.Enumerate();
    }
}