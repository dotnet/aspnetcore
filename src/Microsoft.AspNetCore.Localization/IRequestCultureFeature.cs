// Copyright (c) .NET Foundation. All rights reserved. 
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

namespace Microsoft.AspNetCore.Localization
{
    /// <summary>
    /// Represents the feature that provides the current request's culture information.
    /// </summary>
    public interface IRequestCultureFeature
    {
        /// <summary>
        /// The <see cref="Localization.RequestCulture"/> of the request.
        /// </summary>
        RequestCulture RequestCulture { get; }

        /// <summary>
        /// The <see cref="IRequestCultureProvider"/> that determined the request's culture information.
        /// If the value is <c>null</c> then no provider was used and the request's culture was set to the value of
        /// <see cref="Builder.RequestLocalizationOptions.DefaultRequestCulture"/>.
        /// </summary>
        IRequestCultureProvider Provider { get; }
    }
}