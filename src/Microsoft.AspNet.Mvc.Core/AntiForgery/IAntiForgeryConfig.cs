namespace Microsoft.AspNet.Mvc
{
    // Provides configuration information about the anti-forgery system.
    public interface IAntiForgeryConfig
    {
        // Name of the cookie to use.
        string CookieName { get; }

        // Name of the form field to use.
        string FormFieldName { get; }

        // Whether SSL is mandatory for this request.
        bool RequireSSL { get; }

        // Skip X-FRAME-OPTIONS header.
        bool SuppressXFrameOptionsHeader { get; }
    }
}