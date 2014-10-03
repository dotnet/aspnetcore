// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.PageExecutionInstrumentation;

namespace Microsoft.AspNet.Mvc.Razor
{
    /// <summary>
    /// Represents the contract for an <see cref="IView"/> that executes <see cref="IRazorPage"/> as part of its
    /// execution.
    /// </summary>
    public interface IRazorView : IView
    {
        /// <summary>
        /// Contextualizes the current instance of the <see cref="IRazorView"/> providing it with the 
        /// <see cref="IRazorPage"/> to execute.
        /// </summary>
        /// <param name="razorPage">The <see cref="IRazorPage"/> instance to execute.</param>
        /// <param name="isPartial">Determines if the view is to be executed as a partial.</param>
        void Contextualize(IRazorPage razorPage,
                           bool isPartial,
                           IPageExecutionListenerFeature pageExecutionListenerFeature);
    }
}