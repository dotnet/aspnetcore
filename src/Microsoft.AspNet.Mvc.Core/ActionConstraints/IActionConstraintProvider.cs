// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ActionConstraints
{
    public interface IActionConstraintProvider
    {
        int Order { get; }
        void OnProvidersExecuting([NotNull] ActionConstraintProviderContext context);
        void OnProvidersExecuted([NotNull] ActionConstraintProviderContext context);
    }
}