namespace Microsoft.AspNet.Mvc
{
    public class ActionInfo
    {
        public string ActionName { get; set; }
        public string[] HttpMethods { get; set; }
        public bool RequireActionNameMatch { get; set; }
    }
}
