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
            CookieToken = cookieToken;
        }

        public string FormToken { get; private set; }

        // The cookie token is allowed to be null. 
        // This would be the case when the old cookie token is still valid.
        // In such cases a call to GetTokens would return a token set with null cookie token.
        public string CookieToken { get; private set; }
    }
}