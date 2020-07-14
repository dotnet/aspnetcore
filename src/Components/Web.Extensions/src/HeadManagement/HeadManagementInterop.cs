namespace Microsoft.AspNetCore.Components.Web.Extensions
{
    internal static class HeadManagementInterop
    {
        private const string Prefix = "_blazorHeadManager.";

        public const string SetTitle = Prefix + "setTitle";

        public const string SetTag = Prefix + "setTag";

        public const string RemoveTag = Prefix + "removeTag";
    }
}
