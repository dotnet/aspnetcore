// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text;

namespace Microsoft.AspNet.Mvc.Internal
{
    public static class UTF8EncodingWithoutBOM
    {
        public static readonly Encoding Encoding
            = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    }
}