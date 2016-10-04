namespace Microsoft.AspNetCore.Sockets
{
    // TODO: Should the user implement this or just register their formatters?
    public interface IFormatterFactory
    {
        IFormatter CreateFormatter(Format format, string formatType);
    }
}
