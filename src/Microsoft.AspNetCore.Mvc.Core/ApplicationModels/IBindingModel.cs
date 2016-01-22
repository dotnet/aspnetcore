// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc.ModelBinding;

namespace Microsoft.AspNet.Mvc.ApplicationModels
{
    public interface IBindingModel
    {
        BindingInfo BindingInfo { get; set; }
    }
}