// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components.Web.Extensions
{
    internal interface IInputFileJsCallbacks
    {
        Task NotifyChange(FileListEntry[] files);
    }
}
