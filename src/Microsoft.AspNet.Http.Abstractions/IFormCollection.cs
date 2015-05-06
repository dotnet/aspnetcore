// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
