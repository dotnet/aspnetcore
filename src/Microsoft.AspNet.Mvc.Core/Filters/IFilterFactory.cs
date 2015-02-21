// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc
{
    public interface IFilterFactory : IFilter
    {
        IFilter CreateInstance([NotNull] IServiceProvider serviceProvider);
    }
}
