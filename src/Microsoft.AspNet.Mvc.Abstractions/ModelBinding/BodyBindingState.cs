// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// Represents state of models which are bound using body.
    /// </summary>
    public enum BodyBindingState
    {
        /// <summary>
        /// Represents if there has been no metadata found which needs to read the body during the current
        /// model binding process.
        /// </summary>
        NotBodyBased,

        /// <summary>
        /// Represents if there is a <see cref="BindingSource.Body"/> that
        /// has been found during the current model binding process.
        /// </summary>
        FormatterBased,

        /// <summary>
        /// Represents if there is a <see cref = "BindingSource.Form" /> that
        /// has been found during the current model binding process.
        /// </summary>
        FormBased
    }
}
