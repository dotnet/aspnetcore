using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public abstract class AssociatedMetadataProvider<TModelMetadata> : IModelMetadataProvider
        where TModelMetadata : ModelMetadata
    {
        private readonly ConcurrentDictionary<Type, TypeInformation> _typeInfoCache = new ConcurrentDictionary<Type, TypeInformation>();

        public IEnumerable<ModelMetadata> GetMetadataForProperties(object container, [NotNull] Type containerType)
        {
            return GetMetadataForPropertiesCore(container, containerType);
        }

        public ModelMetadata GetMetadataForProperty(Func<object> modelAccessor, [NotNull] Type containerType, [NotNull] string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                throw new ArgumentException(Resources.FormatArgumentNullOrEmpty("propertyName"), "propertyName");
            }

            var typeInfo = GetTypeInformation(containerType);
            PropertyInformation propertyInfo;
            if (!typeInfo.Properties.TryGetValue(propertyName, out propertyInfo))
            {
                throw new ArgumentException(Resources.FormatCommon_PropertyNotFound(containerType, propertyName), "propertyName");
            }

            return CreatePropertyMetadata(modelAccessor, propertyInfo);
        }

        public ModelMetadata GetMetadataForType(Func<object> modelAccessor, [NotNull] Type modelType)
        {
            TModelMetadata prototype = GetTypeInformation(modelType).Prototype;
            return CreateMetadataFromPrototype(prototype, modelAccessor);
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
            var typeInfo = GetTypeInformation(containerType);
            foreach (var kvp in typeInfo.Properties)
            {
                var propertyInfo = kvp.Value;
                Func<object> modelAccessor = null;
                if (container != null)
                {
                    Func<object, object> propertyGetter = propertyInfo.ValueAccessor;
                    modelAccessor = () => propertyGetter(container);
                }
                yield return CreatePropertyMetadata(modelAccessor, propertyInfo);
            }
        }

        private TModelMetadata CreatePropertyMetadata(Func<object> modelAccessor, PropertyInformation propertyInfo)
        {
            var metadata = CreateMetadataFromPrototype(propertyInfo.Prototype, modelAccessor);
            if (propertyInfo.IsReadOnly)
            {
                metadata.IsReadOnly = true;
            }
            return metadata;
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
            var typeInfo = type.GetTypeInfo();
            var attributes = typeInfo.GetCustomAttributes();
            if (associatedAttributes != null)
            {
                attributes = attributes.Concat(associatedAttributes);
            }
            var info = new TypeInformation
            {
                Prototype = CreateMetadataPrototype(attributes, containerType: null, modelType: type, propertyName: null)
            };

            var properties = new Dictionary<string, PropertyInformation>();
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
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
            return new PropertyInformation
            {
                ValueAccessor = CreatePropertyValueAccessor(property),
                Prototype = CreateMetadataPrototype(property.GetCustomAttributes(),
                                                    containerType,
                                                    property.PropertyType,
                                                    property.Name),
                IsReadOnly = !property.CanWrite || property.SetMethod.IsPrivate
            };
        }

        private static Func<object, object> CreatePropertyValueAccessor(PropertyInfo property)
        {
            var declaringType = property.DeclaringType;
            var declaringTypeInfo = declaringType.GetTypeInfo();
            if (declaringTypeInfo.IsVisible)
            {
                if (property.CanRead)
                {
                    var getMethodInfo = property.GetMethod;
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

            var declaringTypeInfo = declaringType.GetTypeInfo();
            var propertyType = getMethodInfo.ReturnType;
            var dynamicMethod = new DynamicMethod("Get" + propertyName + "From" + declaringType.Name, 
                                                  typeof(object), 
                                                  new [] { typeof(object) });
            var ilg = dynamicMethod.GetILGenerator();

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
            public bool IsReadOnly { get; set; }
        }
    }
}
