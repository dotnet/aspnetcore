using Newtonsoft.Json.Linq;

namespace Microsoft.AspNetCore.SpaServices.Prerendering
{
    public class RenderToStringResult
    {
        public JObject Globals { get; set; }
        public string Html { get; set; }
        public string RedirectUrl { get; set; }
    }
}