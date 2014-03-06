
namespace Microsoft.AspNet.Mvc
{
    public static class RenderUrlExtension
    {
        public static string Action(this IRenderUrl generator)
        {
            return generator.Action(null, null, null);
        }

        public static string Action(this IRenderUrl generator, string action)
        {
            return generator.Action(action, null, null);
        }

        public static string Action(this IRenderUrl generator, string action, object values)
        {
            return generator.Action(action, null, values);
        }

        public static string Action(this IRenderUrl generator, string action, string controller)
        {
            return generator.Action(action, controller, null);
        }
    }
}
