// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Http.Features
{
    /// <summary>
    /// This feature provides access to request server variables set.
    /// <para>
    /// This feature is only available when hosting ASP.NET Core in-process with IIS or IIS Express.
    /// </para>
    /// </summary>
    /// <remarks>
    /// For a list of common server variables available in IIS, see http://go.microsoft.com/fwlink/?LinkId=52471.
    /// </remarks>
    public interface IServerVariablesFeature
    {
        /// <summary>
        /// Gets or sets the value of a server variable for the current request.
        /// </summary>
        /// <param name="variableName">The variable name</param>
        /// <returns>May return null or empty if the variable does not exist or is not set.</returns>
        string this[string variableName] { get; set; }
    }
}
