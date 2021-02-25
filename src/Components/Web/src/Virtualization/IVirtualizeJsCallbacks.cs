// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

namespace Microsoft.AspNetCore.Components.Web.Virtualization
{
    internal interface IVirtualizeJsCallbacks
    {
        void OnBeforeSpacerVisible(float spacerSize, float spacerSeparation, float containerSize);
        void OnAfterSpacerVisible(float spacerSize, float spacerSeparation, float containerSize);
    }
}
