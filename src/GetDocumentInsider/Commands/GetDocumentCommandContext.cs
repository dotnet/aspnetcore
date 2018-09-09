// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.ApiDescription.Client.Commands
{
    [Serializable]
    public class GetDocumentCommandContext
    {
        public string AssemblyDirectory { get; set; }

        public string AssemblyName { get; set; }

        public string AssemblyPath { get; set; }

        public string DocumentName { get; set; }

        public string Method { get; set; }

        public string Output { get; set; }

        public string Service { get; set; }

        public string Uri { get; set; }
    }
}
