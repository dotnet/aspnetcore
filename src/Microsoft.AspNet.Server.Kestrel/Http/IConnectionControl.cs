namespace Microsoft.AspNet.Server.Kestrel.Http
{
    public interface IConnectionControl
    {
        void Pause();
        void Resume();
        void End(ProduceEndType endType);
    }
}