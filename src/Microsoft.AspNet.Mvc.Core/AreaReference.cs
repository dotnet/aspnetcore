// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Web.Mvc
{
    /// <summary>
    /// Controls interpretation of a controller name when constructing a <see cref="RemoteAttribute"/>.
    /// </summary>
    public enum AreaReference
    {
        /// <summary>
        /// Find the controller in the current area.
        /// </summary>
        UseCurrent = 0,

        /// <summary>
        /// Find the controller in the root area.
        /// </summary>
        UseRoot = 1,
    }
}
