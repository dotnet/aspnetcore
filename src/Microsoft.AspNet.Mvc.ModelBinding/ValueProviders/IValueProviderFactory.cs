
namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public interface IValueProviderFactory
    {
        /// <summary>
        /// Get a value provider with values from the given <paramref name="requestContext"/>.
        /// </summary>
        /// <param name="requestContext">RequestContext that value provider will populate from</param>
        /// <returns>a value provider instance or null</returns>
        IValueProvider GetValueProvider(RequestContext requestContext);
    }
}
