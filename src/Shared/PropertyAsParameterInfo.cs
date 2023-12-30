// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Http;

internal sealed class PropertyAsParameterInfo : ParameterInfo
{
    private readonly PropertyInfo _underlyingProperty;
    private readonly ParameterInfo? _constructionParameterInfo;

    private readonly NullabilityInfoContext _nullabilityContext;
    private NullabilityInfo? _nullabilityInfo;

    public PropertyAsParameterInfo(PropertyInfo propertyInfo, NullabilityInfoContext? nullabilityContext = null)
    {
        Debug.Assert(propertyInfo != null, "PropertyInfo must be provided.");

        AttrsImpl = (ParameterAttributes)propertyInfo.Attributes;
        NameImpl = propertyInfo.Name;
        MemberImpl = propertyInfo;
        ClassImpl = propertyInfo.PropertyType;

        // It is not a real parameter in the delegate, so,
        // not defining a real position.
        PositionImpl = -1;

        _nullabilityContext = nullabilityContext ?? new NullabilityInfoContext();
        _underlyingProperty = propertyInfo;
    }

    public PropertyAsParameterInfo(PropertyInfo property, ParameterInfo parameterInfo, NullabilityInfoContext? nullabilityContext = null)
        : this(property, nullabilityContext)
    {
        _constructionParameterInfo = parameterInfo;
    }

    public override bool HasDefaultValue
        => _constructionParameterInfo is not null && _constructionParameterInfo.HasDefaultValue;
    public override object? DefaultValue
        => _constructionParameterInfo?.DefaultValue;
    public override int MetadataToken => _underlyingProperty.MetadataToken;
    public override object? RawDefaultValue
        => _constructionParameterInfo?.RawDefaultValue;

    /// <summary>
    /// Unwraps all parameters that contains <see cref="AsParametersAttribute"/> and
    /// creates a flat list merging the current parameters, not including the
    /// parameters that contain a <see cref="AsParametersAttribute"/>, and all additional
    /// parameters detected.
    /// </summary>
    /// <param name="parameters">List of parameters to be flattened.</param>
    /// <param name="cache">An instance of the method cache class.</param>
    /// <returns>Flat list of parameters.</returns>
    [RequiresUnreferencedCode("Uses unbounded Reflection to access parameter type constructors.")]
    public static ReadOnlySpan<ParameterInfo> Flatten(ParameterInfo[] parameters, ParameterBindingMethodCache cache)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(cache);

        if (parameters.Length == 0)
        {
            return Array.Empty<ParameterInfo>();
        }

        List<ParameterInfo>? flattenedParameters = null;
        NullabilityInfoContext? nullabilityContext = null;

        for (var i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].Name is null)
            {
                throw new InvalidOperationException($"Encountered a parameter of type '{parameters[i].ParameterType}' without a name. Parameters must have a name.");
            }

            if (parameters[i].CustomAttributes.Any(a => a.AttributeType == typeof(AsParametersAttribute)))
            {
                // Initialize the list with all parameter already processed
                // to keep the same parameter ordering
                static List<ParameterInfo> InitializeList(ParameterInfo[] parameters, int i)
                {
                    // will add the rest of the parameters to this list, so set initial capacity to reduce growing the list
                    List<ParameterInfo> list = new(parameters.Length);
                    list.AddRange(parameters.AsSpan(0, i));
                    return list;
                }
                flattenedParameters ??= InitializeList(parameters, i);
                nullabilityContext ??= new();

                var isNullable = Nullable.GetUnderlyingType(parameters[i].ParameterType) != null ||
                    nullabilityContext.Create(parameters[i])?.ReadState == NullabilityState.Nullable;

                if (isNullable)
                {
                    throw new InvalidOperationException($"The nullable type '{TypeNameHelper.GetTypeDisplayName(parameters[i].ParameterType, fullName: false)}' is not supported.");
                }

                var (constructor, constructorParameters) = cache.FindConstructor(parameters[i].ParameterType);
                if (constructor is not null && constructorParameters is { Length: > 0 })
                {
                    foreach (var constructorParameter in constructorParameters)
                    {
                        flattenedParameters.Add(
                            new PropertyAsParameterInfo(
                                constructorParameter.PropertyInfo,
                                constructorParameter.ParameterInfo,
                                nullabilityContext));
                    }
                }
                else
                {
                    var properties = parameters[i].ParameterType.GetProperties();

                    foreach (var property in properties)
                    {
                        if (property.CanWrite)
                        {
                            flattenedParameters.Add(new PropertyAsParameterInfo(property, nullabilityContext));
                        }
                    }
                }
            }
            else
            {
                flattenedParameters?.Add(parameters[i]);
            }
        }

        return flattenedParameters is not null ? CollectionsMarshal.AsSpan(flattenedParameters) : parameters.AsSpan();
    }

    public override object[] GetCustomAttributes(Type attributeType, bool inherit)
    {
        var constructorAttributes = _constructionParameterInfo?.GetCustomAttributes(attributeType, inherit);

        if (constructorAttributes == null || constructorAttributes is { Length: 0 })
        {
            return _underlyingProperty.GetCustomAttributes(attributeType, inherit);
        }

        var propertyAttributes = _underlyingProperty.GetCustomAttributes(attributeType, inherit);

        var mergedAttributes = new Attribute[constructorAttributes.Length + propertyAttributes.Length];
        Array.Copy(constructorAttributes, mergedAttributes, constructorAttributes.Length);
        Array.Copy(propertyAttributes, 0, mergedAttributes, constructorAttributes.Length, propertyAttributes.Length);

        return mergedAttributes;
    }

    public override object[] GetCustomAttributes(bool inherit)
    {
        var constructorAttributes = _constructionParameterInfo?.GetCustomAttributes(inherit);

        if (constructorAttributes == null || constructorAttributes is { Length: 0 })
        {
            return _underlyingProperty.GetCustomAttributes(inherit);
        }

        var propertyAttributes = _underlyingProperty.GetCustomAttributes(inherit);

        // Since the constructors attributes should take priority we will add them first,
        // as we usually call it as First() or FirstOrDefault() in the argument creation
        var mergedAttributes = new object[constructorAttributes.Length + propertyAttributes.Length];
        Array.Copy(constructorAttributes, mergedAttributes, constructorAttributes.Length);
        Array.Copy(propertyAttributes, 0, mergedAttributes, constructorAttributes.Length, propertyAttributes.Length);

        return mergedAttributes;
    }

    public override IList<CustomAttributeData> GetCustomAttributesData()
    {
        var attributes = new List<CustomAttributeData>(
            _constructionParameterInfo?.GetCustomAttributesData() ?? Array.Empty<CustomAttributeData>());
        attributes.AddRange(_underlyingProperty.GetCustomAttributesData());

        return attributes.AsReadOnly();
    }

    public override Type[] GetOptionalCustomModifiers()
        => _underlyingProperty.GetOptionalCustomModifiers();

    public override Type[] GetRequiredCustomModifiers()
        => _underlyingProperty.GetRequiredCustomModifiers();

    public override bool IsDefined(Type attributeType, bool inherit)
    {
        return (_constructionParameterInfo is not null && _constructionParameterInfo.IsDefined(attributeType, inherit)) ||
            _underlyingProperty.IsDefined(attributeType, inherit);
    }

    public new bool IsOptional => NullabilityInfo.ReadState switch
    {
        // Anything nullable is optional
        NullabilityState.Nullable => true,
        // In an oblivious context, the required modifier makes
        // members non-optional
        NullabilityState.Unknown => !_underlyingProperty.GetCustomAttributes().OfType<RequiredMemberAttribute>().Any(),
        // Non-nullable types are only optional if they have a default
        // value
        NullabilityState.NotNull => HasDefaultValue,
        // Assume that types are optional by default so we
        // don't greedily opt parameters into param checking
        _ => true
    };

    public NullabilityInfo NullabilityInfo
        => _nullabilityInfo ??= _constructionParameterInfo is not null ?
        _nullabilityContext.Create(_constructionParameterInfo) :
        _nullabilityContext.Create(_underlyingProperty);
}
