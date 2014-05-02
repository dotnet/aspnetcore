// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

namespace Microsoft.AspNet.Mvc.Rendering
{
    /// <summary>
    /// Controls the value-rendering method For HTML5 input elements of types such as date, time, datetime and
    /// datetime-local.
    /// </summary>
    public enum Html5DateRenderingMode
    {
        /// <summary>
        /// Render date and time values according to the current culture's ToString behavior.
        /// </summary>
        CurrentCulture = 0,

        /// <summary>
        /// Render date and time values as Rfc3339 compliant strings to support HTML5 date and time types of input
        /// elements.
        /// </summary>
        Rfc3339,
    }
}