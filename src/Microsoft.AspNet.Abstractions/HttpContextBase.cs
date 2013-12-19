namespace Microsoft.AspNet.Abstractions
{
    public abstract class HttpContextBase
    {
        // TODO - review IOwinContext for properties

        public abstract HttpRequestBase Request { get; }
        public abstract HttpResponseBase Response { get; }
    }
}
