using Microsoft.AspNet.Abstractions;

namespace Microsoft.AspNet.Mvc
{
    public class DefaultAntiForgeryAdditionalDataProvider : IAntiForgeryAdditionalDataProvider
    {
        public virtual string GetAdditionalData(HttpContext context)
        {
            return string.Empty;
        }

        public virtual bool ValidateAdditionalData(HttpContext context, string additionalData)
        {
            // Default implementation does not understand anything but empty data.
            return string.IsNullOrEmpty(additionalData);
        }
    }
}