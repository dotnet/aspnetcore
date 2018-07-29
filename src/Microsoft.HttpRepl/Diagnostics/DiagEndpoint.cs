namespace Microsoft.HttpRepl.Diagnostics
{
    public class DiagEndpoint
    {
        public string DisplayName { get; set; }
        public string Url { get; set; }
        public DiagEndpointMetadata[] Metadata { get; set; }
    }
}
