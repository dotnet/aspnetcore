// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection;

namespace Microsoft.AspNetCore.Http;

internal class SurrogatedParameterInfo : ParameterInfo
{
    private readonly PropertyInfo _underlyingProperty;
    private readonly NullabilityInfoContext _nullabilityContext;
    private NullabilityInfo? _nullabilityInfo;

    public SurrogatedParameterInfo(PropertyInfo propertyInfo, NullabilityInfoContext nullabilityContext)
    {
        Debug.Assert(null != propertyInfo);

        AttrsImpl = (ParameterAttributes)propertyInfo.Attributes;
        NameImpl = propertyInfo.Name;
        MemberImpl = propertyInfo;
        ClassImpl = propertyInfo.PropertyType;
        PositionImpl = -1;//parameter.Position;

        _nullabilityContext = nullabilityContext;
        _underlyingProperty = propertyInfo;
    }

    public override bool HasDefaultValue => false;
    public override object? DefaultValue => null;
    public override int MetadataToken => _underlyingProperty.MetadataToken;
    public override object? RawDefaultValue => null;

    public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        => _underlyingProperty.GetCustomAttributes(attributeType, inherit);

    public override object[] GetCustomAttributes(bool inherit)
        => _underlyingProperty.GetCustomAttributes(inherit);

    public override IList<CustomAttributeData> GetCustomAttributesData()
        => _underlyingProperty.GetCustomAttributesData();

    public override Type[] GetOptionalCustomModifiers()
        => _underlyingProperty.GetOptionalCustomModifiers();

    public override Type[] GetRequiredCustomModifiers()
        => _underlyingProperty.GetRequiredCustomModifiers();

    public override bool IsDefined(Type attributeType, bool inherit)
        => _underlyingProperty.IsDefined(attributeType, inherit);

    public new bool IsOptional => NullabilityInfo.ReadState != NullabilityState.NotNull;

    public NullabilityInfo NullabilityInfo => _nullabilityInfo ??= _nullabilityContext.Create(_underlyingProperty);
}
