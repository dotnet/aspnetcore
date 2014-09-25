// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers
{
    public class Valid_PlainTagHelper : TagHelper
    {
    }

    public class Valid_InheritedTagHelper : Valid_PlainTagHelper
    {
    }

    public class SingleAttributeTagHelper : TagHelper
    {
        public int IntAttribute { get; set; }
    }

    public class MissingAccessorTagHelper : TagHelper
    {
        public string ValidAttribute { get; set; }
        public string InvalidNoGetAttribute { set { } }
        public string InvalidNoSetAttribute { get { return string.Empty; } }
    }

    public class PrivateAccessorTagHelper : TagHelper
    {
        public string ValidAttribute { get; set; }
        public string InvalidPrivateSetAttribute { get; private set; }
        public string InvalidPrivateGetAttribute { private get; set; }
    }
}