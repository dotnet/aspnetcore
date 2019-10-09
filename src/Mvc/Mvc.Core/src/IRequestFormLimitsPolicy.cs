// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.Filters;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// A marker interface for filters which define a policy for limits on a request's body read as a form.
    /// </summary>
    public interface IRequestFormLimitsPolicy : IFilterMetadata
    {
    }
}
