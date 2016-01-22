// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Mvc.Filters;

namespace Microsoft.AspNet.Mvc.ApplicationModels
{
    public interface IFilterModel
    {
        IList<IFilterMetadata> Filters { get; }
    }
}