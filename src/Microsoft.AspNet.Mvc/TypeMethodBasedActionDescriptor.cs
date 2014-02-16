namespace Microsoft.AspNet.Mvc
{
    public class TypeMethodBasedActionDescriptor : ActionDescriptor
    {
        // TODO:
        // In the next PR the content of the descriptor is changing, and the string below will
        // be represented as route constraints, so for now leaving as is.
        public string ControllerName { get; set; }

        public string ActionName { get; set; }
    }
}
