using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace DynamicTyping
{
    public class RadixTree
    {
        internal RadixTreeNode Root { get; }

        public RadixTree()
        {
            Root = new RadixTreeNode(0);
        }

        public void Add(string key, in int value)
        {
            var node = Root;

            var offset = 0;
            while (offset < key.Length)
            {
                var segmentKey = RadixKey.Calculate(key, ref offset);

                if (offset >= key.Length)
                {
                    node = node.Add(segmentKey, key, value);
                }
                else
                {
                    node = node.Add(segmentKey);
                }
            }
        }
    }

    public class RadixTreeNode
    {
        public ulong NodeKey { get; }

        public string Key { get; private set; } = null;
        public int Value { get; private set; } = -1;
        public bool HasValue => Value != -1;

        public SortedList<ulong, RadixTreeNode> Children { get; } = new SortedList<ulong, RadixTreeNode>();
        public bool HasChildren => Children.Count > 0;

        public RadixTreeNode(ulong nodeKey)
        {
            NodeKey = nodeKey;
        }
            
        public RadixTreeNode Add(ulong nodeKey)
        {
            if (Children.TryGetValue(nodeKey, out var node))
            {
                return node;
            }
                
            node = new RadixTreeNode(nodeKey);
            Children.Add(nodeKey, node);
            return node;
        }
            
        public RadixTreeNode Add(in ulong nodeKey, string key, in int value)
        {
            var node = Add(nodeKey);
            node.Key = key;
            node.Value = value;
            return node;
        }
    }

    public static class RadixKey
    {
        public static ulong Calculate(in string s, ref int offset)
        {
            var buffer = 0ul;
            var bufferSize = 0;
            
            while (bufferSize < sizeof(ulong) && offset < s.Length)
            {
                var c = s[offset++];
                if (c > 0xff)
                {
                    throw new NotSupportedException($"Char is more than a byte: {c} (0x{(ushort)c:x4}), offset {offset} in \"{s}\"");
                }
                
                buffer = buffer << 8 | c;
                bufferSize += 1;
            }
            
            return buffer;
        }
    }

    public static class RadixTreeCompiler
    {
        private class State
        {
            public ILGenerator IL { get; set; }
            
            public LocalBuilder KeyLocal { get; set; }
            public LocalBuilder LengthLocal { get; set; }
            public LocalBuilder OffsetLocal { get; set; }
            public Label NotFoundLabel { get; set; }

            public Action<ILGenerator, bool> LoadInput { get; set; }
            public Action<ILGenerator, RadixTreeNode> HandleMatch { get; set; }
            public Action<ILGenerator> HandleNotFound { get; set; }
        }
        
        public static void Build(RadixTree tree, MethodBuilder method, Action<ILGenerator, bool> loadInput, Action<ILGenerator, RadixTreeNode> handleMatch, Action<ILGenerator> handleNotFound)
        {
            var il = method.GetILGenerator();

            var state = new State
            {
                IL = il,

                KeyLocal = il.DeclareLocal(typeof(ulong)),
                LengthLocal = il.DeclareLocal(typeof(int)),
                OffsetLocal = il.DeclareLocal(typeof(int)),
                NotFoundLabel = il.DefineLabel(),
                
                LoadInput = loadInput,
                HandleMatch = handleMatch,
                HandleNotFound = handleNotFound,
            };


            // var length = input.Length
            state.LoadInput(il, false);
            il.EmitCall(OpCodes.Call, typeof(string).GetProperty(nameof(string.Length)).GetMethod, null);
            il.Emit(OpCodes.Stloc, state.LengthLocal);
            // var offset = 0
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Stloc, state.OffsetLocal);
            
            BuildNode(state, tree.Root);
            
            il.MarkLabel(state.NotFoundLabel);
            handleNotFound(il);
        }

        private static void BuildNode(State state, RadixTreeNode node)
        {
            var il = state.IL;
            
            // key = RadixKey.Calculate(in input, ref offset)
            state.LoadInput(il, true);
            il.Emit(OpCodes.Ldloca, state.OffsetLocal);
            il.EmitCall(OpCodes.Call, typeof(RadixKey).GetMethod(nameof(RadixKey.Calculate)), null);
            il.Emit(OpCodes.Stloc, state.KeyLocal);
            
            BuildNodeNext(state, node.Children.Values);
        }

        private static void BuildNodeNext(State state, IList<RadixTreeNode> nodes)
        {
            if (nodes.Count < 4)
            {
                BuildNodeLinear(state, nodes);
            }
            else
            {
                BuildNodeBinary(state, nodes);
            }
        }

        private static void BuildNodeLinear(State state, IList<RadixTreeNode> nodes)
        {
            var il = state.IL;
            
            foreach (var node in nodes)
            {
                var notMatchedLabel = il.DefineLabel();
                var searchNextLabel = il.DefineLabel();
                
                // if (state.Key == node.Key)
                il.Emit(OpCodes.Ldloc, state.KeyLocal);
                Emit_Ldc_U8(il, node.NodeKey);
                il.Emit(OpCodes.Bne_Un, notMatchedLabel);

                // // handle found
                if (node.HasValue)
                {
                    // if (offset == length) handle match
                    il.Emit(OpCodes.Ldloc, state.OffsetLocal);
                    il.Emit(OpCodes.Ldloc, state.LengthLocal);
                    il.Emit(OpCodes.Bne_Un, searchNextLabel);

                    state.HandleMatch(il, node);
                }

                // else handle next
                il.MarkLabel(searchNextLabel);
                if (node.HasChildren)
                {
                    BuildNode(state, node);
                }

                il.MarkLabel(notMatchedLabel);
            }
            
            il.Emit(OpCodes.Br, state.NotFoundLabel);
        }

        private static void BuildNodeBinary(State state, IList<RadixTreeNode> nodes)
        {
            var il = state.IL;

            var pivotIndex = nodes.Count / 2;
            var pivotValue = nodes[pivotIndex].NodeKey;

            var lower = nodes.Take(pivotIndex).ToList();
            var upper = nodes.Skip(pivotIndex).ToList();

            var elseLabel = il.DefineLabel();
            // if (state.Key < pivotValue)
            il.Emit(OpCodes.Ldloc, state.KeyLocal);
            Emit_Ldc_U8(il, pivotValue);
            il.Emit(OpCodes.Bge_Un, elseLabel);
            BuildNodeNext(state, lower);
            
            // else
            il.MarkLabel(elseLabel);
            BuildNodeNext(state, upper);
        }
        
        private static void Emit_Ldc_U8(ILGenerator il, ulong value) => il.Emit(OpCodes.Ldc_I8, unchecked((long)value));
    }
    
    public class RadixResolverBuilder
    {
        private readonly ModuleBuilder _moduleBuilder;

        public RadixResolverBuilder(ModuleBuilder moduleBuilder)
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

            var (tree, lookup) = BuildTree(targetType); 
            
            RadixTreeCompiler.Build(tree, resolveMethod, (il, isRef) =>
            {
                if (isRef)
                {
                    il.Emit(OpCodes.Ldarga, 2);
                }
                else
                {
                    il.Emit(OpCodes.Ldarg_2);
                }
            }, (il, node) =>
            {
                var property = lookup[node.Value];
                
                il.Emit(OpCodes.Ldarg_1);
                il.EmitCall(OpCodes.Call, property.GetMethod, null);
                if (property.PropertyType.IsValueType)
                {
                    il.Emit(OpCodes.Box, property.PropertyType);
                }
                il.EmitCall(OpCodes.Call, typeof(ResolveResult).GetMethod(nameof(ResolveResult.Resolve), new [] { typeof(object) }), null);
                il.Emit(OpCodes.Ret);
            }, il =>
            {
                il.MarkLabel(unresolvedLabel);
                il.Emit(OpCodes.Ldsfld, typeof(ResolveResult).GetField(nameof(ResolveResult.Unresolved)));
                il.Emit(OpCodes.Ret);
            });
            
            return resolverType.CreateTypeInfo();
        }

        private (RadixTree, IReadOnlyDictionary<int, PropertyInfo>) BuildTree(Type targetType)
        {
            var tree = new RadixTree();
            var lookup = new Dictionary<int, PropertyInfo>();

            var properties = targetType.GetProperties();
            for (var i = 0; i < properties.Length; i++)
            {
                var property = properties[i];
                
                tree.Add(property.Name, i);
                lookup.Add(i, property);
            }
            
            return (tree, lookup);
        }
    } 
}