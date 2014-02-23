using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.AspNet.Mvc.ModelBinding.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public abstract class AssociatedMetadataProvider<TModelMetadata> : IModelMetadataProvider
        where TModelMetadata : ModelMetadata
    {
        private readonly ConcurrentDictionary<Type, TypeInformation> _typeInfoCache = new ConcurrentDictionary<Type, TypeInformation>();

        public IEnumerable<ModelMetadata> GetMetadataForProperties(object container, Type containerType)
        {
            if (containerType == null)
            {
                throw Error.ArgumentNull("containerType");
            }

            return GetMetadataForPropertiesCore(container, containerType);
        }

        public ModelMetadata GetMetadataForProperty(Func<object> modelAccessor, Type containerType, string propertyName)
        {
            if (containerType == null)
            {
                throw Error.ArgumentNull("containerType");
            }
            if (String.IsNullOrEmpty(propertyName))
            {
                throw Error.ArgumentNullOrEmpty("propertyName");
            }

            TypeInformation typeInfo = GetTypeInformation(containerType);
            PropertyInformation propertyInfo;
            if (!typeInfo.Properties.TryGetValue(propertyName, out propertyInfo))
            {
                throw Error.Argument("propertyName", Resources.Common_PropertyNotFound, containerType, propertyName);
            }

            return CreateMetadataFromPrototype(propertyInfo.Prototype, modelAccessor);
        }

        public ModelMetadata GetMetadataForType(Func<object> modelAccessor, Type modelType)
        {
            if (modelType == null)
            {
                throw Error.ArgumentNull("modelType");
            }

            TModelMetadata prototype = GetTypeInformation(modelType).Prototype;
            return CreateMetadataFromPrototype(prototype, modelAccessor);
        }

        public ModelMetadata GetMetadataForParameter(ParameterInfo parameter)
        {
            if (parameter == null)
            {
                throw Error.ArgumentNull("parameter");
            }

            TModelMetadata prototype = GetTypeInformation(parameter.ParameterType, parameter.GetCustomAttributes()).Prototype;
            return CreateMetadataFromPrototype(prototype, modelAccessor: null);
        }

        // Override for creating the prototype metadata (without the accessor)
        protected abstract TModelMetadata CreateMetadataPrototype(IEnumerable<Attribute> attributes,
                                                                  Type containerType,
                                                                  Type modelType,
                                                                  string propertyName);

        // Override for applying the prototype + modelAccess to yield the final metadata
        protected abstract TModelMetadata CreateMetadataFromPrototype(TModelMetadata prototype, 
                                                                      Func<object> modelAccessor);

        private IEnumerable<ModelMetadata> GetMetadataForPropertiesCore(object container, Type containerType)
        {
            TypeInformation typeInfo = GetTypeInformation(containerType);
            foreach (KeyValuePair<string, PropertyInformation> kvp in typeInfo.Properties)
            {
                PropertyInformation propertyInfo = kvp.Value;
                Func<object> modelAccessor = null;
                if (container != null)
                {
                    Func<object, object> propertyGetter = propertyInfo.ValueAccessor;
                    modelAccessor = () => propertyGetter(container);
                }
                yield return CreateMetadataFromPrototype(propertyInfo.Prototype, modelAccessor);
            }
        }

        private TypeInformation GetTypeInformation(Type type, IEnumerable<Attribute> associatedAttributes = null)
        {
            // This retrieval is implemented as a TryGetValue/TryAdd instead of a GetOrAdd to avoid the performance cost of creating instance delegates
            TypeInformation typeInfo;
            if (!_typeInfoCache.TryGetValue(type, out typeInfo))
            {
                typeInfo = CreateTypeInformation(type, associatedAttributes);
                _typeInfoCache.TryAdd(type, typeInfo);
            }
            return typeInfo;
        }

        private TypeInformation CreateTypeInformation(Type type, IEnumerable<Attribute> associatedAttributes)
        {
            TypeInfo typeInfo = type.GetTypeInfo();
            IEnumerable<Attribute> attributes = typeInfo.GetCustomAttributes();
            if (associatedAttributes != null)
            {
                attributes = attributes.Concat(associatedAttributes);
            }
            TypeInformation info = new TypeInformation
            {
                Prototype = CreateMetadataPrototype(attributes, containerType: null, modelType: type, propertyName: null)
            };
            // TODO: Determine if we need this. TypeDescriptor does not exist in CoreCLR.
            //ICustomTypeDescriptor typeDescriptor = TypeDescriptorHelper.Get(type);
            //info.TypeDescriptor = typeDescriptor;

            Dictionary<string, PropertyInformation> properties = new Dictionary<string, PropertyInformation>();

            // TODO: Figure out if there's a better way to identify public non-static properties
            foreach (PropertyInfo property in type.GetRuntimeProperties().Where(p => p.GetMethod.IsPublic && !p.GetMethod.IsStatic))
            {
                // Avoid re-generating a property descriptor if one has already been generated for the property name
                if (!properties.ContainsKey(property.Name))
                {
                    properties.Add(property.Name, CreatePropertyInformation(type, property));
                }
            }
            info.Properties = properties;

            return info;
        }

        private PropertyInformation CreatePropertyInformation(Type containerType, PropertyInfo property)
        {
            PropertyInformation info = new PropertyInformation();
            info.ValueAccessor = CreatePropertyValueAccessor(property);
            info.Prototype = CreateMetadataPrototype(property.GetCustomAttributes().Cast<Attribute>(), containerType, property.PropertyType, property.Name);
            return info;
        }

        private static Func<object, object> CreatePropertyValueAccessor(PropertyInfo property)
        {
            Type declaringType = property.DeclaringType;
            TypeInfo declaringTypeInfo = declaringType.GetTypeInfo();
            if (declaringTypeInfo.IsVisible)
            {
                if (property.CanRead)
                {
                    MethodInfo getMethodInfo = property.GetMethod;
                    if (getMethodInfo != null)
                    {
                        return CreateDynamicValueAccessor(getMethodInfo, declaringType, property.Name);
                    }
                }
            }

            // If either the type isn't public or we can't find a public getter, use the slow Reflection path
            return container => property.GetValue(container);
        }

        // Uses Lightweight Code Gen to generate a tiny delegate that gets the property value
        // This is an optimization to avoid having to go through the much slower System.Reflection APIs
        // e.g. generates (object o) => (Person)o.Id
        private static Func<object, object> CreateDynamicValueAccessor(MethodInfo getMethodInfo, Type declaringType, string propertyName)
        {
            Contract.Assert(getMethodInfo != null && getMethodInfo.IsPublic && !getMethodInfo.IsStatic);

            TypeInfo declaringTypeInfo = declaringType.GetTypeInfo();
            Type propertyType = getMethodInfo.ReturnType;
            DynamicMethod dynamicMethod = new DynamicMethod("Get" + propertyName + "From" + declaringType.Name, typeof(object), new Type[] { typeof(object) });
            ILGenerator ilg = dynamicMethod.GetILGenerator();

            // Load the container onto the stack, convert from object => declaring type for the property
            ilg.Emit(OpCodes.Ldarg_0);
            if (declaringTypeInfo.IsValueType)
            {
                ilg.Emit(OpCodes.Unbox, declaringType);
            }
            else
            {
                ilg.Emit(OpCodes.Castclass, declaringType);
            }

            // if declaring type is value type, we use Call : structs don't have inheritance
            // if get method is sealed or isn't virtual, we use Call : it can't be overridden
            if (declaringTypeInfo.IsValueType || !getMethodInfo.IsVirtual || getMethodInfo.IsFinal)
            {
                ilg.Emit(OpCodes.Call, getMethodInfo);
            }
            else
            {
                ilg.Emit(OpCodes.Callvirt, getMethodInfo);
            }

            // Box if the property type is a value type, so it can be returned as an object
            if (propertyType.GetTypeInfo().IsValueType)
            {
                ilg.Emit(OpCodes.Box, propertyType);
            }

            // Return property value
            ilg.Emit(OpCodes.Ret);

            return (Func<object, object>)dynamicMethod.CreateDelegate(typeof(Func<object, object>));
        }

        private sealed class TypeInformation
        {
            public TModelMetadata Prototype { get; set; }
            public Dictionary<string, PropertyInformation> Properties { get; set; }
        }

        private sealed class PropertyInformation
        {
            public Func<object, object> ValueAccessor { get; set; }
            public TModelMetadata Prototype { get; set; }
        }
    }
}
