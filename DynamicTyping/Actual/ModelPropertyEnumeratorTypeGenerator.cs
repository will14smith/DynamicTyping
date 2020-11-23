using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace DynamicTyping.Actual
{
    internal class ModelPropertyEnumeratorTypeGenerator
    {
        private readonly ModuleBuilder _moduleBuilder;

        public ModelPropertyEnumeratorTypeGenerator(ModuleBuilder moduleBuilder)
        {
            _moduleBuilder = moduleBuilder;
        }

        public ConstructorInfo CreateEnumeratorType(Type propertyType, IReadOnlyList<FieldBuilder> fieldStates, IReadOnlyDictionary<string, ModelPropertyTypeGenerator.Field> fields)
        {
            var typeBuilder = _moduleBuilder.DefineUniqueType("ModelPropertiesEnumerator");
            
            var groupedFields = fieldStates.Select(fs => (FieldState: fs, Fields: (IReadOnlyList<ModelPropertyTypeGenerator.Field>)fields.Values.Where(f => f.State.Field == fs).OrderBy(f => f.State.Offset).ToList())).ToList();
            
            var enumeratorFields = new EnumeratorFields(typeBuilder, propertyType, fieldStates);
            var constructor = ImplementConstructor(typeBuilder, propertyType, enumeratorFields, fieldStates);
            
            typeBuilder.AddInterfaceImplementation(typeof(IEnumerator<KeyValuePair<string, object>>));
            ImplementEnumerator(typeBuilder, enumeratorFields, groupedFields);
            ImplementDisposable(typeBuilder);

            typeBuilder.CreateTypeInfo();

            return constructor;
        }

        private static ConstructorInfo ImplementConstructor(TypeBuilder typeBuilder, Type propertyType, EnumeratorFields enumeratorFields, IReadOnlyList<FieldBuilder> fieldStates)
        {
            var types = new Type[1 + fieldStates.Count];
            types[0] = propertyType;
            for (var index = 0; index < fieldStates.Count; index++)
            {
                types[1 + index] = typeof(BitVector32);
            }

            var constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, types);
            constructorBuilder.DefineParameter(1, ParameterAttributes.None, "properties");
            for (var index = 0; index < fieldStates.Count; index++)
            {
                constructorBuilder.DefineParameter(2 + index, ParameterAttributes.None, $"fieldState{index}");
            }
            var il = constructorBuilder.GetILGenerator();
            
            // this._properties = properties;
            il.Emit(OpCodes.Ldarg_0); 
            il.Emit(OpCodes.Ldarg_1); 
            il.Emit(OpCodes.Stfld, enumeratorFields.Properties);

            for (var index = 0; index < fieldStates.Count; index++)
            {
                var fieldState = fieldStates[index];
                // this._fieldState{i} = fieldState{i};
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg, 2 + index);
                il.Emit(OpCodes.Stfld, enumeratorFields.FieldStatesMap[fieldState]);
            }

            il.Emit(OpCodes.Ret);

            return constructorBuilder;
        }

        private static void ImplementEnumerator(TypeBuilder typeBuilder, EnumeratorFields enumeratorFields, IReadOnlyList<(FieldBuilder FieldState, IReadOnlyList<ModelPropertyTypeGenerator.Field> Fields)> fields)
        {
            ImplementMoveNext(typeBuilder, enumeratorFields, fields);
            ImplementReset(typeBuilder);
            ImplementCurrentProperty(typeBuilder, enumeratorFields.Current);
        }

        private static void ImplementMoveNext(TypeBuilder typeBuilder, EnumeratorFields enumeratorFields, IReadOnlyList<(FieldBuilder FieldState, IReadOnlyList<ModelPropertyTypeGenerator.Field> Fields)> fields)
        {
            var methodBuilder = typeBuilder.DefineMethod(
                "MoveNext",
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Final | MethodAttributes.Virtual,
                typeof(bool),
                new Type[0]);

            var il = methodBuilder.GetILGenerator();

            // switch (this._state)
            var caseLabels = fields.SelectMany(x => x.Fields).Select(_ => il.DefineLabel()).ToArray();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, enumeratorFields.State);
            il.Emit(OpCodes.Switch, caseLabels);
            
            // default: return false;
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Ret);

            var i = 0;
            for (var fsi = 0; fsi < fields.Count; fsi++)
            {
                var fsFields = fields[fsi].Fields;
                
                for (var fi = 0; fi < fsFields.Count; fi++)
                {
                    var field = fsFields[fi];
                    
                    // case i:
                    il.MarkLabel(caseLabels[i++]);

                    var skip = il.DefineLabel();
                    // if(!this._fieldStates{fsi}[fi]) goto skip;
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldflda, enumeratorFields.FieldStatesMap[field.State.Field]);
                    il.Emit(OpCodes.Ldc_I4, 1 << fi);
                    il.Emit(OpCodes.Call, typeof(BitVector32).GetProperty("Item", typeof(bool)).GetMethod);
                    il.Emit(OpCodes.Brfalse, skip);
                    
                    // this._current = new KeyValuePair("Key", this._properties.Key)
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldstr, field.Property.Name);
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldfld, enumeratorFields.Properties);
                    il.Emit(OpCodes.Callvirt, field.Property.GetMethod);
                    if (field.Type.IsValueType)
                    {
                        il.Emit(OpCodes.Box, field.Type);
                    }
                    il.Emit(OpCodes.Newobj, typeof(KeyValuePair<string, object>).GetConstructor(new []{typeof(string), typeof(object)}));
                    il.Emit(OpCodes.Stfld, enumeratorFields.Current);
                    
                    // this._state = i+1
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldc_I4, i);
                    il.Emit(OpCodes.Stfld, enumeratorFields.State);
                   
                    // return true;
                    il.Emit(OpCodes.Ldc_I4_1);
                    il.Emit(OpCodes.Ret);

                    il.MarkLabel(skip);
                }
            }
            
            // return false;
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Ret);
        }

        private static void ImplementReset(TypeBuilder typeBuilder)
        {
            var methodBuilder = typeBuilder.DefineMethod(
                "Reset",
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Final | MethodAttributes.Virtual,
                typeof(void),
                new Type[0]);

            var il = methodBuilder.GetILGenerator();

            il.ThrowException(typeof(NotSupportedException));
        }

        private static void ImplementCurrentProperty(TypeBuilder typeBuilder, FieldInfo currentBuilder)
        {
            const MethodAttributes propertyMethodAttributes = MethodAttributes.Private | MethodAttributes.Final | MethodAttributes.SpecialName | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.NewSlot;
            
            {
                var propertyBuilder = typeBuilder.DefineProperty("Current", PropertyAttributes.None, typeof(object), null);

                var getterBuilder =
                    typeBuilder.DefineMethod("get_IEnumerator.Current", propertyMethodAttributes, typeof(object), Type.EmptyTypes);
                var getterIL = getterBuilder.GetILGenerator();
                getterIL.Emit(OpCodes.Ldarg_0);
                getterIL.Emit(OpCodes.Ldfld, currentBuilder);
                getterIL.Emit(OpCodes.Box, currentBuilder.FieldType);
                getterIL.Emit(OpCodes.Ret);
                propertyBuilder.SetGetMethod(getterBuilder);
                
                typeBuilder.DefineMethodOverride(getterBuilder, typeof(IEnumerator).GetMethod("get_Current"));
            }

            {
                var propertyBuilder = typeBuilder.DefineProperty("Current", PropertyAttributes.None,
                    typeof(KeyValuePair<string, object>), null);

                var getterBuilder = typeBuilder.DefineMethod("get_IEnumeratorGenericCurrent", propertyMethodAttributes,
                    typeof(KeyValuePair<string, object>), Type.EmptyTypes);
                var getterIL = getterBuilder.GetILGenerator();
                getterIL.Emit(OpCodes.Ldarg_0);
                getterIL.Emit(OpCodes.Ldfld, currentBuilder);
                getterIL.Emit(OpCodes.Ret);
                propertyBuilder.SetGetMethod(getterBuilder);
                
                typeBuilder.DefineMethodOverride(getterBuilder, typeof(IEnumerator<KeyValuePair<string, object>>).GetMethod("get_Current"));
            }
        }

        private static void ImplementDisposable(TypeBuilder typeBuilder)
        {
            var methodBuilder = typeBuilder.DefineMethod(
                "Dispose",
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Final | MethodAttributes.Virtual,
                typeof(void),
                new Type[0]);

            var il = methodBuilder.GetILGenerator();
            il.Emit(OpCodes.Ret);
        }
        
        private class EnumeratorFields
        {
            public EnumeratorFields(TypeBuilder typeBuilder, Type propertyType, IReadOnlyList<FieldBuilder> fieldStates)
            {
                State = typeBuilder.DefineField("_state", typeof(int), FieldAttributes.Private);
                Current = typeBuilder.DefineField("_current", typeof(KeyValuePair<string, object>), FieldAttributes.Private); 

                Properties = typeBuilder.DefineField("_properties", propertyType, FieldAttributes.Private);
                var i = 0;
                FieldStatesMap = fieldStates.ToDictionary(x => x, _ => typeBuilder.DefineField($"_fieldState{i++}", typeof(BitVector32), FieldAttributes.Private));
            }

            public FieldBuilder State { get; }
            public FieldBuilder Current { get; }
            
            public FieldBuilder Properties { get; }
            public IReadOnlyDictionary<FieldBuilder, FieldBuilder> FieldStatesMap { get; }
        }
    }
}