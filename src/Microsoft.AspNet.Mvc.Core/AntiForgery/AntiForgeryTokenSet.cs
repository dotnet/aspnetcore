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

using System;
using Microsoft.AspNet.Mvc.Core;

namespace Microsoft.AspNet.Mvc
{
    public class AntiForgeryTokenSet
    {
        public AntiForgeryTokenSet(string formToken, string cookieToken)
        {
            if (string.IsNullOrEmpty(formToken))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, formToken);
            }

            FormToken = formToken;

            // Cookie Token is allowed to be null in the case when the old cookie is valid
            // and there is no new cookieToken generated. 
            CookieToken = cookieToken;
        }

        public string FormToken { get; private set; }

        // The cookie token is allowed to be null. 
        // This would be the case when the old cookie token is still valid.
        // In such cases a call to GetTokens would return a token set with null cookie token.
        public string CookieToken { get; private set; }
    }
}