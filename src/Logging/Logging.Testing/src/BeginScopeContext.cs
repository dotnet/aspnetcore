// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.Logging.Testing
{
    public class BeginScopeContext
    {
        public object Scope { get; set; }

        public string LoggerName { get; set; }
    }
}