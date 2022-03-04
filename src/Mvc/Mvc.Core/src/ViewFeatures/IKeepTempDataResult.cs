// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    /// <summary>
    /// A marker interface for <see cref="IActionResult"/> types which need to have temp data saved.
    /// </summary>
    public interface IKeepTempDataResult : IActionResult
    {
    }
}
