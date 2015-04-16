// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNet.Http
{
    /// <summary>
    /// Contains the parsed form values.
    /// </summary>
    public interface IFormCollection : IReadableStringCollection
    {
        IFormFileCollection Files { get; }
    }
}
