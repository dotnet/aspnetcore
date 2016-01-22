// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Represents an <see cref="ActionResult"/> that when executed will
    /// do nothing.
    /// </summary>
    public class EmptyResult : ActionResult
    {
        /// <inheritdoc />
        public override void ExecuteResult(ActionContext context)
        {
        }
    }
}
