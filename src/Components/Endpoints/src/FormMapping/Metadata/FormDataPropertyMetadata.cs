// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Components.Endpoints.FormMapping.Metadata;
internal class FormDataPropertyMetadata
{
    public FormDataPropertyMetadata(PropertyHelper property, FormDataTypeMetadata propertyTypeInfo)
    {
        Property = property;
        PropertyMetadata = propertyTypeInfo;
    }

    public PropertyHelper Property { get; }
    public FormDataTypeMetadata PropertyMetadata { get; }
}
