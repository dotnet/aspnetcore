// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.Rendering;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    public interface IViewContextAware
    {
        void Contextualize(ViewContext viewContext);
    }
}
