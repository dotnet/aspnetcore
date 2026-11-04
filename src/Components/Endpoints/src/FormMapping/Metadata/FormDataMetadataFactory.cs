// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.Endpoints.FormMapping.Metadata;

internal partial class FormDataMetadataFactory(List<IFormDataConverterFactory> factories, ILoggerFactory loggerFactory)
{
    private readonly object _lock = new object();
    private readonly FormMetadataContext _context = new();
    private readonly ParsableConverterFactory _parsableFactory = factories.OfType<ParsableConverterFactory>().Single();
    private readonly DictionaryConverterFactory _dictionaryFactory = factories.OfType<DictionaryConverterFactory>().Single();
    private readonly FileConverterFactory _fileConverterFactory = factories.OfType<FileConverterFactory>().Single();
    private readonly CollectionConverterFactory _collectionFactory = factories.OfType<CollectionConverterFactory>().Single();
    private readonly ILogger<FormDataMetadataFactory> _logger = loggerFactory.CreateLogger<FormDataMetadataFactory>();

    [RequiresDynamicCode(FormMappingHelpers.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(FormMappingHelpers.RequiresUnreferencedCodeMessage)]
    public FormDataTypeMetadata? GetOrCreateMetadataFor(Type type, FormDataMapperOptions options)
    {
        var shouldClearContext = !_context.ResolutionInProgress;
        lock (_lock)
        {
            try
            {
                // We are walking the graph in order to detect recursive types.
                // We evaluate whether a type is:
                // 1. Primitive
                // 2. Dictionary
                // 3. Collection
                // 4. Complex
                // Only complex types can be recursive.
                // We only compute metadata when we are dealing with objects, other classes of
                // types are handled directly by the appropriate converters.
                // We keep track of the metadata for the types because it is useful when we generate
                // the specific object converter for a type.
                // The code generation varies depending on whether there is recursion or not within
                // the type graph.
                if (shouldClearContext)
                {
                    Log.StartResolveMetadataGraph(_logger, type);
                    _context.BeginResolveGraph();
                }

                // Try to get the metadata for the type or create and add a new instance.
                var result = _context.TypeMetadata.TryGetValue(type, out var value) ? value : new FormDataTypeMetadata(type);
                if (value == null)
                {
                    Log.NoMetadataFound(_logger, type);
                    _context.TypeMetadata[type] = result;
                }
                else
                {
                    Log.MetadataFound(_logger, type);
                }

                if (type.IsGenericTypeDefinition)
                {
                    Log.GenericTypeDefinitionNotSupported(_logger, type);
                    _context.TypeMetadata.Remove(type);
                    return null;
                }

                // Check for cycles and mark any type as recursive if needed.
                DetectCyclesAndMarkMetadataTypesAsRecursive(type, result);

                // We found the value on the existing metadata, we can return it.
                if (value != null)
                {
                    return result;
                }

                // These blocks are evaluated in a specific order.
                if (_parsableFactory.CanConvert(type, options) || type.IsEnum ||
                    (Nullable.GetUnderlyingType(type) is { } underlyingType &&
                        _parsableFactory.CanConvert(underlyingType, options)))
                {
                    Log.PrimitiveType(_logger, type);
                    result.Kind = FormDataTypeKind.Primitive;
                    return result;
                }

                if (_fileConverterFactory.CanConvert(type, options))
                {
                    result.Kind = FormDataTypeKind.File;
                    return result;
                }

                if (WellKnownConverters.Converters.TryGetValue(type, out var converter))
                {
                    result.Kind = FormDataTypeKind.Primitive;
                    return result;
                }

                if (_dictionaryFactory.CanConvert(type, options))
                {
                    Log.DictionaryType(_logger, type);
                    result.Kind = FormDataTypeKind.Dictionary;
                    var (keyType, valueType) = DictionaryConverterFactory.ResolveDictionaryTypes(type)!;
                    result.KeyType = GetOrCreateMetadataFor(keyType, options);
                    result.ValueType = GetOrCreateMetadataFor(valueType, options);
                    return result;
                }

                if (_collectionFactory.CanConvert(type, options))
                {
                    Log.CollectionType(_logger, type);
                    result.Kind = FormDataTypeKind.Collection;
                    result.ElementType = GetOrCreateMetadataFor(CollectionConverterFactory.ResolveElementType(type)!, options);
                    return result;
                }

                Log.ObjectType(_logger, type);
                result.Kind = FormDataTypeKind.Object;
                _context.Track(type);
                var constructors = type.GetConstructors();

                if (constructors.Length == 1)
                {
                    result.Constructor = constructors[0];
                    if (type.IsAbstract)
                    {
                        Log.AbstractClassesNotSupported(_logger, type);
                        _context.TypeMetadata.Remove(type);
                        return null;
                    }
                }
                else if (constructors.Length > 1)
                {
                    // We can't select the constructor when there are multiple of them.
                    Log.MultiplePublicConstructorsFound(_logger, type);
                    return null;
                }
                else if (!type.IsValueType)
                {
                    if (type.IsInterface)
                    {
                        Log.InterfacesNotSupported(_logger, type);
                    }
                    else if (type.IsAbstract)
                    {
                        Log.AbstractClassesNotSupported(_logger, type);
                    }
                    else
                    {
                        Log.NoPublicConstructorFound(_logger, type);
                    }

                    _context.TypeMetadata.Remove(type);
                    // We can't bind to reference types without constructors.
                    return null;
                }

                if (result.Constructor != null)
                {
                    if (_logger.IsEnabled(LogLevel.Debug))
                    {
                        var parameters = $"({string.Join(", ", result.Constructor.GetParameters().Select(p => p.ParameterType.Name))})";
                        Log.ConstructorFound(_logger, type, parameters);
                    }

                    var values = result.Constructor.GetParameters();

                    foreach (var parameter in values)
                    {
                        Log.ConstructorParameter(_logger, type, parameter.Name!, parameter.ParameterType);
                        var parameterTypeInfo = GetOrCreateMetadataFor(parameter.ParameterType, options);
                        if (parameterTypeInfo == null)
                        {
                            Log.ConstructorParameterTypeNotSupported(_logger, type, parameter.Name!, parameter.ParameterType);
                            _context.TypeMetadata.Remove(type);
                            return null;
                        }

                        result.ConstructorParameters.Add(new FormDataParameterMetadata(parameter, parameterTypeInfo));
                    }
                }

                var candidateProperty = PropertyHelper.GetVisibleProperties(type);
                foreach (var propertyHelper in candidateProperty)
                {
                    var property = propertyHelper.Property;
                    Log.CandidateProperty(_logger, propertyHelper.Name, property.PropertyType);
                    var matchingConstructorParameter = result
                        .ConstructorParameters
                        .FirstOrDefault(p => string.Equals(p.Name, property.Name, StringComparison.OrdinalIgnoreCase));

                    if (matchingConstructorParameter != null)
                    {
                        Log.MatchingConstructorParameterFound(_logger, matchingConstructorParameter.Name, property.Name);
                        var dataMember = property.GetCustomAttribute<DataMemberAttribute>();
                        if (dataMember != null && dataMember.IsNameSetExplicitly && dataMember.Name != null)
                        {
                            Log.CustomParameterNameMetadata(_logger, dataMember.Name, property.Name);
                            matchingConstructorParameter.Name = dataMember.Name;
                        }

                        // The propertyHelper is already present in the constructor, we don't need to add it again.
                        continue;
                    }

                    var ignoreDataMember = property.GetCustomAttribute<IgnoreDataMemberAttribute>();
                    if (ignoreDataMember != null)
                    {
                        Log.IgnoredProperty(_logger, property.Name);
                        // The propertyHelper is marked as ignored, we don't need to add it.
                        continue;
                    }

                    if (property.SetMethod == null || !property.SetMethod.IsPublic)
                    {
                        Log.NonPublicSetter(_logger, property.Name);
                        // The property is readonly, we don't need to add it.
                        continue;
                    }

                    var propertyTypeInfo = GetOrCreateMetadataFor(property.PropertyType, options);
                    if (propertyTypeInfo == null)
                    {
                        Log.PropertyTypeNotSupported(_logger, type, property.Name, property.PropertyType);
                        _context.TypeMetadata.Remove(type);
                        return null;
                    }
                    var propertyInfo = new FormDataPropertyMetadata(property, propertyTypeInfo);

                    var dataMemberAttribute = property.GetCustomAttribute<DataMemberAttribute>();
                    if (dataMemberAttribute != null && dataMemberAttribute.IsNameSetExplicitly && dataMemberAttribute.Name != null)
                    {
                        Log.CustomParameterNameMetadata(_logger, dataMemberAttribute.Name, property.Name);
                        propertyInfo.Name = dataMemberAttribute.Name;
                        propertyInfo.Required = dataMemberAttribute.IsRequired;
                        Log.PropertyRequired(_logger, propertyInfo.Name);
                    }

                    var requiredAttribute = property.GetCustomAttribute<RequiredMemberAttribute>();
                    if (requiredAttribute != null)
                    {
                        propertyInfo.Required = true;
                        Log.PropertyRequired(_logger, propertyInfo.Name);
                    }

                    result.Properties.Add(propertyInfo);
                }

                Log.MetadataComputed(_logger, type);
                return result;
            }
            finally
            {
                _context.Untrack(type);
                if (shouldClearContext)
                {
                    Log.EndResolveMetadataGraph(_logger, type);
                    _context.EndResolveGraph();
                }
            }
        }
    }

    internal bool HasMetadataFor(Type type) => _context.TypeMetadata.ContainsKey(type);

    private void DetectCyclesAndMarkMetadataTypesAsRecursive(Type type, FormDataTypeMetadata result)
    {
        // Mark any type as recursive if its already present in the current resolution graph or
        // if there is a base type for it on the resolution graph.
        // For example, given A and B : A. With A having a propertyHelper of type B.
        // when we inspect B, we can tell that A is recursive because you can have A -> B -> B -> B -> ...
        // The opposite scenario is not something we need to worry about because we don't support polymorphism,
        // meaning that we will never create an instance of B if the declared type is A.
        // In that scenario, A and B : A. With B having a propertyHelper of type A.
        // The recursion stops at A, because A doesn't define the propertyHelper and we will never bind B to A.
        // If in the future we support polymorphism, it's a matter or updating the logic below to account for it.
        for (var i = 0; i < _context.CurrentTypes.Count; i++)
        {
            if (_context.CurrentTypes[i] == type)
            {
                // We found an exact match in the current resolution graph.
                // This means that the type is recursive.
                result.IsRecursive = true;
                ReportRecursiveChain(type);

            }
            else if (type.IsSubclassOf(_context.CurrentTypes[i]))
            {
                // We found a type that is assignable from the current type.
                // This means that the type is recursive.
                var existingType = _context.TypeMetadata[_context.CurrentTypes[i]];
                ReportRecursiveChain(type);
                existingType.IsRecursive = true;
            }

            // We don't need to check for interfaces here because we never map to an interface, so if we encounter one, we will fail,
            // as we can't construct the converter to map the interface to a concrete instance.
        }

        void ReportRecursiveChain(Type type)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                var chain = string.Join(" -> ", _context.CurrentTypes.Append(type).Select(t => t.Name));
                Log.RecursiveTypeFound(_logger, type, chain);
            }
        }
    }

    private class FormMetadataContext
    {
        public Dictionary<Type, FormDataTypeMetadata> TypeMetadata { get; set; } = new();

        public List<Type> CurrentTypes { get; set; } = new();

        public bool ResolutionInProgress { get; internal set; }

        internal void BeginResolveGraph()
        {
            if (ResolutionInProgress)
            {
                throw new InvalidOperationException("Cannot begin a new resolution graph while one is already in progress.");
            }
            ResolutionInProgress = true;
        }

        internal void EndResolveGraph()
        {
            if (!ResolutionInProgress)
            {
                throw new InvalidOperationException("Cannot end a resolution graph while one is not in progress.");
            }
            ResolutionInProgress = false;
            CurrentTypes.Clear();
        }

        internal void Track(Type type)
        {
            CurrentTypes.Add(type);
        }

        internal void Untrack(Type type)
        {
            CurrentTypes.Remove(type);
        }
    }

    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Debug, "Begin resolve metadata graph for type '{Type}'.", EventName = nameof(StartResolveMetadataGraph))]
        public static partial void StartResolveMetadataGraph(ILogger logger, Type type);

        [LoggerMessage(2, LogLevel.Debug, "End resolve metadata graph for type '{Type}'.", EventName = nameof(EndResolveMetadataGraph))]
        public static partial void EndResolveMetadataGraph(ILogger logger, Type type);

        [LoggerMessage(3, LogLevel.Debug, "Cached metadata found for type '{Type}'.", EventName = nameof(Metadata))]
        public static partial void MetadataFound(ILogger<FormDataMetadataFactory> logger, Type type);

        [LoggerMessage(4, LogLevel.Debug, "No cached metadata graph for type '{Type}'.", EventName = nameof(NoMetadataFound))]
        public static partial void NoMetadataFound(ILogger<FormDataMetadataFactory> logger, Type type);

        [LoggerMessage(5, LogLevel.Debug, "Recursive type '{Type}' found in the resolution graph '{Chain}'.", EventName = nameof(RecursiveTypeFound))]
        public static partial void RecursiveTypeFound(ILogger<FormDataMetadataFactory> logger, Type type, string chain);

        [LoggerMessage(6, LogLevel.Debug, "'{Type}' identified as primitive.", EventName = nameof(PrimitiveType))]
        public static partial void PrimitiveType(ILogger<FormDataMetadataFactory> logger, Type type);

        [LoggerMessage(7, LogLevel.Debug, "'{Type}' identified as dictionary.", EventName = nameof(DictionaryType))]
        public static partial void DictionaryType(ILogger<FormDataMetadataFactory> logger, Type type);

        [LoggerMessage(8, LogLevel.Debug, "'{Type}' identified as collection.", EventName = nameof(CollectionType))]
        public static partial void CollectionType(ILogger<FormDataMetadataFactory> logger, Type type);

        [LoggerMessage(9, LogLevel.Debug, "'{Type}' identified as object.", EventName = nameof(ObjectType))]
        public static partial void ObjectType(ILogger<FormDataMetadataFactory> logger, Type type);

        [LoggerMessage(10, LogLevel.Debug, "Constructor found for type '{Type}' with parameters '{Parameters}'.", EventName = nameof(ConstructorFound))]
        public static partial void ConstructorFound(ILogger<FormDataMetadataFactory> logger, Type type, string parameters);

        [LoggerMessage(11, LogLevel.Debug, "Constructor parameter '{Name}' of type '{ParameterType}' found for type '{Type}'.", EventName = nameof(ConstructorParameter))]
        public static partial void ConstructorParameter(ILogger<FormDataMetadataFactory> logger, Type type, string name, Type parameterType);

        [LoggerMessage(12, LogLevel.Debug, "Candidate property '{Name}' of type '{PropertyType}'.", EventName = nameof(CandidateProperty))]
        public static partial void CandidateProperty(ILogger<FormDataMetadataFactory> logger, string name, Type propertyType);

        [LoggerMessage(13, LogLevel.Debug, "Candidate property {PropertyName} has a matching constructor parameter '{ConstructorParameterName}'.", EventName = nameof(MatchingConstructorParameterFound))]
        public static partial void MatchingConstructorParameterFound(ILogger<FormDataMetadataFactory> logger, string constructorParameterName, string propertyName);

        [LoggerMessage(14, LogLevel.Debug, "Candidate property or constructor parameter {PropertyName} defines a custom name '{CustomName}'.", EventName = nameof(CustomParameterNameMetadata))]
        public static partial void CustomParameterNameMetadata(ILogger<FormDataMetadataFactory> logger, string customName, string propertyName);

        [LoggerMessage(15, LogLevel.Debug, "Candidate property {Name} will not be mapped. It has been explicitly ignored.", EventName = nameof(IgnoredProperty))]
        public static partial void IgnoredProperty(ILogger<FormDataMetadataFactory> logger, string name);

        [LoggerMessage(16, LogLevel.Debug, "Candidate property {Name} will not be mapped. It has no public setter.", EventName = nameof(NonPublicSetter))]
        public static partial void NonPublicSetter(ILogger<FormDataMetadataFactory> logger, string name);

        [LoggerMessage(17, LogLevel.Debug, "Candidate property {Name} is marked as required.", EventName = nameof(PropertyRequired))]
        public static partial void PropertyRequired(ILogger<FormDataMetadataFactory> logger, string name);

        [LoggerMessage(18, LogLevel.Debug, "Metadata created for {Type}.", EventName = nameof(MetadataComputed))]
        public static partial void MetadataComputed(ILogger<FormDataMetadataFactory> logger, Type type);

        [LoggerMessage(19, LogLevel.Debug, "Can not map type generic type definition '{Type}'.", EventName = nameof(GenericTypeDefinitionNotSupported))]
        public static partial void GenericTypeDefinitionNotSupported(ILogger<FormDataMetadataFactory> logger, Type type);

        [LoggerMessage(20, LogLevel.Debug, "Unable to select a constructor. Multiple public constructors found for type '{Type}'.", EventName = nameof(MultiplePublicConstructorsFound))]
        public static partial void MultiplePublicConstructorsFound(ILogger<FormDataMetadataFactory> logger, Type type);

        [LoggerMessage(21, LogLevel.Debug, "Can not map interface type '{Type}'.", EventName = nameof(InterfacesNotSupported))]
        public static partial void InterfacesNotSupported(ILogger<FormDataMetadataFactory> logger, Type type);

        [LoggerMessage(22, LogLevel.Debug, "Can not map abstract type '{Type}'.", EventName = nameof(AbstractClassesNotSupported))]
        public static partial void AbstractClassesNotSupported(ILogger<FormDataMetadataFactory> logger, Type type);

        [LoggerMessage(23, LogLevel.Debug, "Unable to select a constructor. No public constructors found for type '{Type}'.", EventName = nameof(NoPublicConstructorFound))]
        public static partial void NoPublicConstructorFound(ILogger<FormDataMetadataFactory> logger, Type type);

        [LoggerMessage(24, LogLevel.Debug, "Can not map type '{Type}'. Constructor parameter {Name} of type {ParameterType} is not supported.", EventName = nameof(ConstructorParameterTypeNotSupported))]
        public static partial void ConstructorParameterTypeNotSupported(ILogger<FormDataMetadataFactory> logger, Type type, string name, Type parameterType);

        [LoggerMessage(25, LogLevel.Debug, "Can not map type '{Type}'. Property {Name} of type {PropertyType} is not supported.", EventName = nameof(PropertyTypeNotSupported))]
        public static partial void PropertyTypeNotSupported(ILogger<FormDataMetadataFactory> logger, Type type, string name, Type propertyType);
    }
}
