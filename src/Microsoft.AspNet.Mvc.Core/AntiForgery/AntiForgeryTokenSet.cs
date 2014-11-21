// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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