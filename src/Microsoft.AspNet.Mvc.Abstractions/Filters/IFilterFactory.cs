// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.Filters
{
    public interface IFilterFactory : IFilterMetadata
    {
        IFilterMetadata CreateInstance([NotNull] IServiceProvider serviceProvider);
    }
}
