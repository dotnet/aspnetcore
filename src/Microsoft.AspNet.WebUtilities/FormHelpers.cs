// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.WebUtilities
{
    public static class FormHelpers
    {
        /// <summary>
        /// Parses an HTTP form body.
        /// </summary>
        /// <param name="text">The HTTP form body to parse.</param>
        /// <returns>The <see cref="T:Microsoft.Owin.IFormCollection" /> object containing the parsed HTTP form body.</returns>
        public static IFormCollection ParseForm(string text)
        {
            return ParsingHelpers.GetForm(text);
        }
    }
}