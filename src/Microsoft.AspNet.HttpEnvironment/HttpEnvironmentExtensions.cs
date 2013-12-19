namespace Microsoft.AspNet.HttpEnvironment
{
    public static class HttpEnvironmentExtensions
    {
        public static TFeature GetFeature<TFeature>(this IHttpEnvironment environment) where TFeature : class
        {
            return (TFeature)environment.GetFeature(typeof(TFeature));
        }
    }
}
