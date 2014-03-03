namespace Microsoft.AspNet.Mvc
{
    // This needs more thought, the intent is that we would be able to cache over this constraint without running the accept method.
    public enum RouteKeyHandling
    {
        /// <summary>
        /// Requires that the key will be in the route values, and that the content matches.
        /// </summary>
        RequireKey,

        /// <summary>
        /// Requires that the key will not be in the route values.
        /// </summary>
        DenyKey,

        /// <summary>
        /// Requires that the key will be in the route values, but ignore the content.
        /// </summary>
        CatchAll,

        /// <summary>
        /// Always accept.
        /// </summary>
        AcceptAlways,
    }
}
