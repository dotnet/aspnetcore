
namespace Microsoft.AspNet.Mvc
{
    public static class UrlHelperExtensions
    {
        public static string Action([NotNull] this IUrlHelper generator)
        {
            return generator.Action(null, null, null);
        }

        public static string Action([NotNull] this IUrlHelper generator, string action)
        {
            return generator.Action(action, null, null);
        }

        public static string Action([NotNull] this IUrlHelper generator, string action, object values)
        {
            return generator.Action(action, null, values);
        }

        public static string Action([NotNull] this IUrlHelper generator, string action, string controller)
        {
            return generator.Action(action, controller, null);
        }
    }
}
