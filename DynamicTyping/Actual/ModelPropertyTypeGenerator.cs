using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace DynamicTyping.Actual
{
    public class ModelPropertyTypeGenerator
    {
        private readonly ModuleBuilder _moduleBuilder;
        public ModelPropertyTypeGenerator()
        {
              var assemblyName = new AssemblyName($"{Assembly.GetExecutingAssembly().GetName().Name}.Properties{Guid.NewGuid():N}");
              var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
              _moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name);
        }
        
        internal ModelPropertyTypeGenerator(ModuleBuilder moduleBuilder)
        {
            _moduleBuilder = moduleBuilder;
        }

        public Type CreateTypeForProperties(IReadOnlyDictionary<string, Type> properties, IReadOnlyCollection<Type> interfaces)
        {
            var typeBuilder = _moduleBuilder.DefineUniqueType("ModelProperties");

            EnsureKeysAreCaseInsensitiveUnique(properties);

            foreach (var iface in interfaces)
            {
                properties = ImplementPropertiesInterface(typeBuilder, iface, properties);
            }

            var fieldStateAllocator = new FieldStateAllocator(typeBuilder);
            var fieldInfos = new Dictionary<string, Field>();
            foreach (var property in properties)
            {
                var field = AddProperty(typeBuilder, fieldStateAllocator, property.Key, property.Value);
                fieldInfos.Add(property.Key, field);
            }

            var constructor = typeBuilder.DefineDefaultConstructor(MethodAttributes.Public);

            typeBuilder.AddInterfaceImplementation(typeof(IProperties<string, object>));

            ImplementGetEnumerator(typeBuilder, fieldStateAllocator, fieldInfos);
            ImplementTryGetValue(typeBuilder, fieldInfos);
            ImplementSet(typeBuilder, fieldInfos);
            ImplementCopy(typeBuilder, fieldStateAllocator, constructor, fieldInfos);

            return typeBuilder.CreateTypeInfo();
        }

        private static void EnsureKeysAreCaseInsensitiveUnique(IReadOnlyDictionary<string, Type> properties)
        {
            var seenProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var property in properties)
            {
                if (!seenProperties.Add(property.Key))
                {
                    throw new DuplicatePropertyException(property.Key);
                }
            }
        }

        public class DuplicatePropertyException : ArgumentException
        {
            public DuplicatePropertyException(string property) : base($"Duplicate case-insensitive property: {property}") { }
        }

        private static IReadOnlyDictionary<string, Type> ImplementPropertiesInterface(TypeBuilder typeBuilder, Type iface, IReadOnlyDictionary<string, Type> properties)
        {
            var props = properties.ToDictionary(x => x.Key, x => x.Value);
            typeBuilder.AddInterfaceImplementation(iface);

            foreach (var property in iface.GetProperties())
            {
                if (properties.TryGetValue(property.Name, out var type))
                {
                    if (type != property.PropertyType)
                    {
                        throw new InvalidCastException($"Duplicate property '{property.Name}' with types '{property.PropertyType}' and '{type}'");
                    }
                }
                else
                {
                    props.Add(property.Name, property.PropertyType);
                }
            }

            return props;
        }

        private void ImplementGetEnumerator(TypeBuilder typeBuilder, FieldStateAllocator fieldStateAllocator, IReadOnlyDictionary<string, Field> fieldInfos)
        {
            var enumeratorTypeGenerator = new ModelPropertyEnumeratorTypeGenerator(_moduleBuilder);
            // TODO _fieldState
            var constructor = enumeratorTypeGenerator.CreateEnumeratorType(typeBuilder, fieldStateAllocator.Fields, fieldInfos);

            MethodBuilder getEnumerator;
            {
                var methodBuilder = typeBuilder.DefineMethod(
                    "GetEnumerator",
                    MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Final | MethodAttributes.Virtual,
                    typeof(IEnumerator<KeyValuePair<string, object>>),
                    new Type[0]);

                var il = methodBuilder.GetILGenerator();
                
                // return new ModelPropertyEnumerator(this, ...fieldState{i});
                il.Emit(OpCodes.Ldarg_0);
                for (var i = 0; i < fieldStateAllocator.Fields.Count; i++)
                {
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldfld, fieldStateAllocator.Fields[i]);
                }
                il.Emit(OpCodes.Newobj, constructor);
                il.Emit(OpCodes.Ret);

                getEnumerator = methodBuilder;
            }
            
            {
                var methodBuilder = typeBuilder.DefineMethod(
                    "GetEnumerator",
                    MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Final | MethodAttributes.Virtual,
                    typeof(IEnumerator),
                    new Type[0]);

                var il = methodBuilder.GetILGenerator();
                
                // return this.GetEnumerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Call, getEnumerator);
                il.Emit(OpCodes.Ret);
            }
        }

        private static void ImplementCopy(TypeBuilder typeBuilder, FieldStateAllocator fieldStateAllocator, ConstructorInfo constructor, IReadOnlyDictionary<string, Field> fieldInfos)
        {
            var methodBuilder = typeBuilder.DefineMethod(
                "Copy", 
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.Final, 
                typeof(IProperties<string, object>),
                new Type[0]); 
                
            var il = methodBuilder.GetILGenerator();
            
            // var clonedProps = new ModelProperties();
            il.Emit(OpCodes.Newobj, constructor);
            var clonedProps = il.DeclareLocal(typeBuilder);
            il.Emit(OpCodes.Stloc, clonedProps);

            foreach (var field in fieldStateAllocator.Fields)
            {
                // clonedProps._fieldState = this._fieldState
                il.Emit(OpCodes.Ldloc, clonedProps);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, field);
                il.Emit(OpCodes.Stfld, field);
            }
            
            foreach (var pair in fieldInfos)
            {
                // clonedProps._key = this._key
                il.Emit(OpCodes.Ldloc, clonedProps);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, pair.Value.Builder);
                il.Emit(OpCodes.Stfld, pair.Value.Builder);
            }
            
            // return clonedProps;
            il.Emit(OpCodes.Ldloc, clonedProps);
            il.Emit(OpCodes.Ret);
        }

        private static void ImplementSet(TypeBuilder typeBuilder, IReadOnlyDictionary<string, Field> fieldInfos)
        {
            var methodBuilder = typeBuilder.DefineMethod(
                nameof(IProperties<int,int>.TrySetValue), 
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.Final);

            var valueType = methodBuilder.DefineGenericParameters("T")[0];
            methodBuilder.SetParameters(typeof(string), valueType);
            methodBuilder.SetReturnType(typeof(bool));
            methodBuilder.DefineParameter(1, ParameterAttributes.None, "key");
            methodBuilder.DefineParameter(2, ParameterAttributes.None, "value");
            
            var il = methodBuilder.GetILGenerator();
            
            var invalidTypeLabel = il.DefineLabel();
            foreach (var pair in fieldInfos)
            {
                var key = pair.Key;
                var fieldType = pair.Value.Type;

                // if (key == "Key")
                var label = il.DefineLabel();
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldstr, key);
                il.Emit(OpCodes.Ldc_I4, (int) StringComparison.OrdinalIgnoreCase);
                var stringEqualsMethodInfo = typeof(string).GetMethod(nameof(string.Equals), new []{typeof(string), typeof(string), typeof(StringComparison)});
                il.Emit(OpCodes.Call, stringEqualsMethodInfo);
                il.Emit(OpCodes.Brfalse, label);
                
                // this._fieldState[key] = Used;
                pair.Value.State.MarkAsUsed(il);

                // if (this._key is decimal-like)
               if (fieldType == typeof(decimal))
               {
                   // this._key = Convert.ToDecimal(value);
                   il.Emit(OpCodes.Ldarg_0);
                   il.Emit(OpCodes.Ldarg_2); 
                   il.Emit(OpCodes.Box, valueType);
                   il.Emit(OpCodes.Call, typeof(Convert).GetMethod(nameof(Convert.ToDecimal), new [] { typeof(object) }));
                   il.Emit(OpCodes.Stfld, pair.Value.Builder);
               }
               else if (fieldType == typeof(decimal?))
               {
                   var isNullLabel = il.DefineLabel();
                   var skipElseLabel = il.DefineLabel();
                   // if(value != null)
                   il.Emit(OpCodes.Ldarg_2);
                   il.Emit(OpCodes.Box, valueType);
                   il.Emit(OpCodes.Brfalse, isNullLabel);
                   
                   // this._key = new Nullable<decimal>(Convert.ToDecimal(value));
                   il.Emit(OpCodes.Ldarg_0);
                   il.Emit(OpCodes.Ldarg_2); 
                   il.Emit(OpCodes.Box, valueType);
                   il.Emit(OpCodes.Call, typeof(Convert).GetMethod(nameof(Convert.ToDecimal), new [] { typeof(object) }));
                   il.Emit(OpCodes.Newobj, typeof(Nullable<decimal>).GetConstructor(new [] { typeof(decimal) }));
                   il.Emit(OpCodes.Stfld, pair.Value.Builder);
                   il.Emit(OpCodes.Br, skipElseLabel);

                   // else 
                   il.MarkLabel(isNullLabel);
                   // this._key = new Nullable<decimal>();
                   il.Emit(OpCodes.Ldarg_0);
                   il.Emit(OpCodes.Ldflda, pair.Value.Builder);
                   il.Emit(OpCodes.Initobj, typeof(decimal?));
                   
                   il.MarkLabel(skipElseLabel);
               }
               else
               {
                   // if (value != null && !(value is Type typedValue)) goto throwLabel;
                   var isNullLabel = il.DefineLabel();
                   if (!pair.Value.Type.IsValueType)
                   {
                       il.Emit(OpCodes.Ldarg_2);
                       il.Emit(OpCodes.Box, valueType);
                       il.Emit(OpCodes.Brfalse, isNullLabel);
                   }
                   il.Emit(OpCodes.Ldarg_2);
                   il.Emit(OpCodes.Box, valueType);
                   il.Emit(OpCodes.Isinst, fieldType);
                   il.Emit(OpCodes.Brfalse, invalidTypeLabel);
                
                   il.MarkLabel(isNullLabel);
                
                   // this._key = value;
                   il.Emit(OpCodes.Ldarg_0);
                   il.Emit(OpCodes.Ldarg_2); 
                   il.Emit(OpCodes.Box, valueType);
                   il.Emit(OpCodes.Unbox_Any, fieldType);
                   il.Emit(OpCodes.Stfld, pair.Value.Builder);
               }

               // return true;
                il.Emit(OpCodes.Ldc_I4_1);
                il.Emit(OpCodes.Ret);
                il.MarkLabel(label);
            }
            
            // return false;
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Ret);
           
            // throwLabel
            il.MarkLabel(invalidTypeLabel);
            il.ThrowException(typeof(InvalidCastException));
        }

         private static void ImplementTryGetValue(TypeBuilder typeBuilder, IReadOnlyDictionary<string, Field> fieldInfos)
         {
            var methodBuilder = typeBuilder.DefineMethod(
                "TryGetValue", 
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.Final);

            methodBuilder.SetParameters(typeof(string), typeof(object).MakeByRefType());
            methodBuilder.SetReturnType(typeof(bool));
            methodBuilder.DefineParameter(1, ParameterAttributes.None, "key");
            methodBuilder.DefineParameter(2, ParameterAttributes.Out, "value");

             var il = methodBuilder.GetILGenerator();

             foreach (var pair in fieldInfos)
             {
                 var key = pair.Key;
                 
                 // if (key == "Key")
                 var label = il.DefineLabel();
                 il.Emit(OpCodes.Ldarg_1);
                 il.Emit(OpCodes.Ldstr, key);
                 il.Emit(OpCodes.Ldc_I4, (int) StringComparison.OrdinalIgnoreCase);
                 var stringEqualsMethodInfo = typeof(string).GetMethod(nameof(string.Equals), new []{typeof(string), typeof(string), typeof(StringComparison)});
                 il.Emit(OpCodes.Call, stringEqualsMethodInfo);
                 il.Emit(OpCodes.Brfalse, label);
                 
                 // if(this._fieldState[key] == NotUsed) return false;
                 pair.Value.State.ReturnFalseIfNotUsed(il);

                 // *value = (object) this.Key;
                 il.Emit(OpCodes.Ldarg_2);
                 il.Emit(OpCodes.Ldarg_0);
                 il.Emit(OpCodes.Ldfld, pair.Value.Builder);
                 il.Emit(OpCodes.Box, pair.Value.Type);
                 il.Emit(OpCodes.Stind_Ref);
                 
                 // return true;
                 il.Emit(OpCodes.Ldc_I4_1);
                 il.Emit(OpCodes.Ret);
                 il.MarkLabel(label);
             }
             
             // return false;
             il.Emit(OpCodes.Ldc_I4_0);
             il.Emit(OpCodes.Ret);
        }

         private Field AddProperty(TypeBuilder builder, FieldStateAllocator fieldStateAllocator, string name, Type type)
         {
             var fieldBuilder = builder.DefineField($"_{name}", type, FieldAttributes.Private);
             var propertyBuilder = builder.DefineProperty(name, PropertyAttributes.None, type, null);

             var fieldState = fieldStateAllocator.Allocate();
             
             const MethodAttributes propertyMethodAttributes = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Virtual;
                   
             var getterBuilder = builder.DefineMethod($"get_{name}", propertyMethodAttributes, type, Type.EmptyTypes);
             var getterIL = getterBuilder.GetILGenerator();
             getterIL.Emit(OpCodes.Ldarg_0);
             getterIL.Emit(OpCodes.Ldfld, fieldBuilder);
             getterIL.Emit(OpCodes.Ret);
             propertyBuilder.SetGetMethod(getterBuilder);
                   
             var setterBuilder = builder.DefineMethod($"set_{name}", propertyMethodAttributes, null, new [] { type });
             var setterIL = setterBuilder.GetILGenerator();
             setterIL.Emit(OpCodes.Ldarg_0);
             setterIL.Emit(OpCodes.Ldarg_1);
             setterIL.Emit(OpCodes.Stfld, fieldBuilder);
             fieldState.MarkAsUsed(setterIL);
             setterIL.Emit(OpCodes.Ret);
             propertyBuilder.SetSetMethod(setterBuilder);

             return new Field(fieldBuilder, propertyBuilder, fieldState);
         }

         internal class Field
         {
             public Field(FieldBuilder builder, PropertyBuilder property, FieldState state)
             {
                 Builder = builder;
                 Property = property;
                 State = state;
             }

             public FieldBuilder Builder { get; }
             public PropertyBuilder Property { get; }
             public Type Type => Builder.FieldType;
             public FieldState State { get; }
         }

         internal class FieldState
         {
             private static readonly PropertyInfo Indexer = typeof(BitVector32).GetProperty("Item", typeof(bool));

             public FieldBuilder Field { get; }
             public int Offset { get; }

             public FieldState(FieldBuilder field, int offset)
             {
                 Field = field;
                 Offset = offset;
             }

             public void MarkAsUsed(ILGenerator il)
             {
                 // this._field[mask] = true;
                 il.Emit(OpCodes.Ldarg_0);
                 il.Emit(OpCodes.Ldflda, Field);
                 il.Emit(OpCodes.Ldc_I4, 1 << Offset);
                 il.Emit(OpCodes.Ldc_I4_1);
                 il.Emit(OpCodes.Call, Indexer.SetMethod);
             }

             public void ReturnFalseIfNotUsed(ILGenerator il)
             {
                 var label = il.DefineLabel();

                 // if(!this._field[mask]) return false
                 il.Emit(OpCodes.Ldarg_0);
                 il.Emit(OpCodes.Ldflda, Field);
                 il.Emit(OpCodes.Ldc_I4, 1 << Offset);
                 il.Emit(OpCodes.Call, Indexer.GetMethod);
                 il.Emit(OpCodes.Brtrue, label);
                 
                 il.Emit(OpCodes.Ldc_I4_0);
                 il.Emit(OpCodes.Ret);

                 il.MarkLabel(label);
             }
         }
         
         private class FieldStateAllocator
         {
             private readonly TypeBuilder _typeBuilder;
             private readonly List<FieldBuilder> _fields;

             private FieldBuilder _field;

             public int NextOffset { get; private set; }

             public FieldStateAllocator(TypeBuilder typeBuilder)
             {
                 _typeBuilder = typeBuilder;
                 _fields = new List<FieldBuilder>();
             }

             public IReadOnlyList<FieldBuilder> Fields => _fields;

             public FieldState Allocate()
             {
                 if (NextOffset == 32 || _field == null)
                 {
                     MoveToNextField();
                 }

                 var offset = NextOffset++;

                 return new FieldState(_field, offset);
             }

             private void MoveToNextField()
             {
                 NextOffset = 0;
                 _field = _typeBuilder.DefineField($"_fieldState{Fields.Count}", typeof(BitVector32), FieldAttributes.Private);
                 _fields.Add(_field);
             }
         }
    }
}