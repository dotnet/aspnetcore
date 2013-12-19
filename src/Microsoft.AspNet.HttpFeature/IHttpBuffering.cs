namespace Microsoft.AspNet.Interfaces
{
    public interface IHttpBuffering
    {
        void DisableRequestBuffering();
        void DisableResponseBuffering();
    }
}
