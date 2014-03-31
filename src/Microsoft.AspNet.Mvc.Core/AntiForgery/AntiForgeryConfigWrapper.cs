namespace Microsoft.AspNet.Mvc
{
    public sealed class AntiForgeryConfigWrapper : IAntiForgeryConfig
    {
        public string CookieName
        {
            get { return AntiForgeryConfig.CookieName; }
        }

        public string FormFieldName
        {
            get { return AntiForgeryConfig.AntiForgeryTokenFieldName; }
        }

        public bool RequireSSL
        {
            get { return AntiForgeryConfig.RequireSsl; }
        }

        public bool SuppressXFrameOptionsHeader
        {
            get { return AntiForgeryConfig.SuppressXFrameOptionsHeader; }
        }
    }
}