// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;

namespace Microsoft.AspNet.Mvc.Razor.Precompilation
{
    [RestrictChildren(Never)]
    [CustomValidation(typeof(TypeDoesNotExist)]
    [HtmlTargetElement("img"
    public class TypeWithMalformedAttribute
    {

    }
}
