// Copyright (c) .NET Foundation. All rights reserved. 
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

namespace Microsoft.AspNet.Localization
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
    }
}