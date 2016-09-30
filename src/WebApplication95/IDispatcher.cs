using System;

namespace WebApplication95
{
    public interface IDispatcher
    {
        void OnIncoming(ArraySegment<byte> data);
    }
}