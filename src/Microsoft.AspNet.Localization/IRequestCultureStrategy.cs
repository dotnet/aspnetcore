// Copyright (c) .NET Foundation. All rights reserved. 
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Localization
{
    /// <summary>
    /// Represents a strategy for determining the culture information of an <see cref="HttpRequest"/>.
    /// </summary>
    public interface IRequestCultureStrategy
    {
        RequestCulture DetermineRequestCulture(HttpContext httpContext);
    }
}
