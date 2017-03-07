using Microsoft.AspNetCore.Server.Kestrel.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.KestrelTests
{
    public static class MemoryPoolExtensions
    {
        public static MemoryPoolIterator Add(this MemoryPoolIterator iterator, int count)
        {
            for (int i = 0; i < count; i++)
            {
                iterator.Take();
            }
            return iterator;
        } 
    }
}
