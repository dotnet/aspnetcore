// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;

namespace Microsoft.AspNetCore.Http;

internal class SurrogateParameterInfo : ParameterInfo
{
    private readonly PropertyInfo _underlyingProperty;
    private readonly ParameterInfo? _constructionParameterInfo;

    private readonly NullabilityInfoContext _nullabilityContext;
    private NullabilityInfo? _nullabilityInfo;

    public SurrogateParameterInfo(PropertyInfo propertyInfo, NullabilityInfoContext? nullabilityContext = null)
    {
        Debug.Assert(null != propertyInfo);

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

    public SurrogateParameterInfo(PropertyInfo property, ParameterInfo parameterInfo, NullabilityInfoContext? nullabilityContext = null)
        : this(property, nullabilityContext)
    {
        _constructionParameterInfo = parameterInfo;
    }

    public override bool HasDefaultValue
        => _constructionParameterInfo is not null && _constructionParameterInfo.HasDefaultValue;
    public override object? DefaultValue
        => _constructionParameterInfo is not null ? _constructionParameterInfo.DefaultValue : null;
    public override int MetadataToken => _underlyingProperty.MetadataToken;
    public override object? RawDefaultValue
        => _constructionParameterInfo is not null ? _constructionParameterInfo.RawDefaultValue : null;

    public override object[] GetCustomAttributes(Type attributeType, bool inherit)
    {
        var attributes = _constructionParameterInfo?.GetCustomAttributes(attributeType, inherit);

        if (attributes == null || attributes is { Length: 0 })
        {
            attributes = _underlyingProperty.GetCustomAttributes(attributeType, inherit);
        }

        return attributes;
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

    public new bool IsOptional => NullabilityInfo.ReadState != NullabilityState.NotNull;

    public NullabilityInfo NullabilityInfo
        => _nullabilityInfo ??= _constructionParameterInfo is not null ?
        _nullabilityContext.Create(_constructionParameterInfo) :
        _nullabilityContext.Create(_underlyingProperty);
}
