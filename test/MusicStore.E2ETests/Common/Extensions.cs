namespace System.Net
{
    public static class Extensions
    {
        public static Cookie GetCookieWithName(this CookieCollection cookieCollection, string cookieName)
        {
            foreach (Cookie cookie in cookieCollection)
            {
                if (cookie.Name == cookieName)
                {
                    return cookie;
                }
            }

            return null;
        }
    }
}
