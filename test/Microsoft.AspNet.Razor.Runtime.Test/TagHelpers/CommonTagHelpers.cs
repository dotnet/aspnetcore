// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers
{
    public class Valid_PlainTagHelper : ITagHelper
    {
    }

    public class Valid_InheritedTagHelper : Valid_PlainTagHelper
    {
    }

    public class SingleAttributeTagHelper : ITagHelper
    {
        public int IntAttribute { get; set; }
    }

    public class MissingAccessorTagHelper : ITagHelper
    {
        public string ValidAttribute { get; set; }
        public string InvalidNoGetAttribute { set { } }
        public string InvalidNoSetAttribute { get { return string.Empty; } }
    }

    public class PrivateAccessorTagHelper : ITagHelper
    {
        public string ValidAttribute { get; set; }
        public string InvalidPrivateSetAttribute { get; private set; }
        public string InvalidPrivateGetAttribute { private get; set; }
    }
}