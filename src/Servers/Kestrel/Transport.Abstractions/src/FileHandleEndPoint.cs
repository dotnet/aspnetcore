using System.Net;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions
{
    public class FileHandleEndPoint : EndPoint
    {
        public FileHandleEndPoint(ulong fileHandle, FileHandleType fileHandleType)
        {
            FileHandle = fileHandle;
            FileHandleType = fileHandleType;
        }

        public ulong FileHandle { get; }
        public FileHandleType FileHandleType { get; }
    }
}
