using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Utf8Json;

namespace DynamicTyping.Benchmark
{
    class DictBenchmark
    {
        private readonly Dict _instance;
        private readonly IResolver _resolver;
        
        public DictBenchmark()
        {
            _instance = JsonSerializer.Deserialize<Dict>(Program.DictInputBytes);
            SetParents(_instance);
            
            _resolver = new DictResolver();
        }

        public Dict Read()
        {
            var a = JsonSerializer.Deserialize<Dict>(Program.DictInputBytes);
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
        public IReadOnlyCollection<KeyValuePair<string, object>> Enumerate()
        {
            return _instance.Properties.ToList();
        }
        
        private static void SetParents(Dict instance)
        {
            var children = instance.Children;
            if (children == null) return;

            foreach (var child in children)
            {
                if (child == null) continue;

                child.Parent = instance;
                SetParents(child);
            }
        }
    }
    
    
    class DictResolver : IResolver
    {
        public ResolveResult Resolve(object obj, string field)
        {
            if (!(obj is Dict instance)) return ResolveResult.Unresolved;

            return field switch
            {
                nameof(Static.Parent) => ResolveResult.Resolve(instance.Parent),
                nameof(Static.Children) => ResolveResult.Resolve(instance.Children),
            
                _ => instance.Properties.TryGetValue(field, out var value) ? ResolveResult.Resolve(value) : ResolveResult.Unresolved,
            };
        }
    }

    public class Dict
    {
        public Dictionary<string, object> Properties { get; set; }
        [IgnoreDataMember]
        public Dict Parent { get; set; }
        public List<Dict> Children { get; set; }
    }
}