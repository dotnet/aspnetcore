// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace Microsoft.AspNetCore.Components.Endpoints.FormMapping.Metadata;
internal class FormDataPropertyMetadata(PropertyInfo property, FormDataTypeMetadata propertyTypeInfo) : IFormDataValue
{
    public PropertyInfo Property => property;

    public FormDataTypeMetadata PropertyMetadata { get; } = propertyTypeInfo;

    public string Name { get; set; } = property.Name!;

    public Type Type => property.PropertyType;

    public bool Required { get; set; }
}
