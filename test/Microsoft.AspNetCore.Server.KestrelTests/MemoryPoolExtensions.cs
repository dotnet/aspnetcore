using Microsoft.AspNetCore.Server.Kestrel.Infrastructure;

namespace Microsoft.AspNetCore.Server.KestrelTests
{
    public static class MemoryPoolExtensions
    {
        public static MemoryPoolIterator2 Add(this MemoryPoolIterator2 iterator, int count)
        {
            int actual;
            return iterator.CopyTo(new byte[count], 0, count,  out actual);
        } 
    }
}
