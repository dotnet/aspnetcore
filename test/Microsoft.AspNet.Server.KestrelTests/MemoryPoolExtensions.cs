using Microsoft.AspNet.Server.Kestrel.Http;

namespace Microsoft.AspNet.Server.KestrelTests
{
    public static class MemoryPoolExtensions
    {
        public static MemoryPoolBlock2.Iterator Add(this MemoryPoolBlock2.Iterator iterator, int count)
        {
            int actual;
            return iterator.CopyTo(new byte[count], 0, count,  out actual);
        } 
    }
}
