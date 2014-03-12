
namespace Microsoft.AspNet.Mvc
{
    public static class UrlHelperExtensions
    {
        public static string Action([NotNull] this IUrlHelper generator)
        {
            return generator.Action(action: null, controller: null, values: null);
        }

        public static string Action([NotNull] this IUrlHelper generator, string action)
        {
            return generator.Action(action: action, controller: null, values: null);
        }

        public static string Action([NotNull] this IUrlHelper generator, string action, object values)
        {
            return generator.Action(action: action, controller: null, values: values);
        }

        public static string Action([NotNull] this IUrlHelper generator, string action, string controller)
        {
            return generator.Action(action: action, controller: controller, values: null);
        }
    }
}
