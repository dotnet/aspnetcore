// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Microsoft.CodeAnalysis.Razor
{
    public abstract class Invalid_AbstractTagHelper : TagHelper
    {
    }

    public class Invalid_GenericTagHelper<T> : TagHelper
    {
    }

    internal class Invalid_InternalTagHelper : TagHelper
    {
    }

    public class Valid_PlainTagHelper : TagHelper
    {
    }

    public class Valid_InheritedTagHelper : Valid_PlainTagHelper
    {
    }
}
