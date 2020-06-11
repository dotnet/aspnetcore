// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Components
{
    // Used to understand boxed generic EventCallbacks
    internal interface IEventCallback
    {
        bool HasDelegate { get; }

        object UnpackForRenderTree();
    }
}
