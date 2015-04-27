// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.Core
{
    public interface IFilterProvider
    {
        int Order { get; }
        void OnProvidersExecuting([NotNull] FilterProviderContext context);
        void OnProvidersExecuted([NotNull] FilterProviderContext context);
    }
}
