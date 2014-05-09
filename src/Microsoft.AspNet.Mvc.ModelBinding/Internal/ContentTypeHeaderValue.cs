// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    internal class ContentTypeHeaderValue
    {
        public ContentTypeHeaderValue([NotNull] string contentType,
                                      string charSet)
        {
            ContentType = contentType;
            CharSet = charSet;
        }

        public string ContentType { get; private set; }

        public string CharSet { get; set; }
    }
}
