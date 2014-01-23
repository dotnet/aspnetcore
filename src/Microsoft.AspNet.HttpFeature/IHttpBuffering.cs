namespace Microsoft.AspNet.HttpFeature
{
    public interface IHttpBuffering
    {
        void DisableRequestBuffering();
        void DisableResponseBuffering();
    }
}
