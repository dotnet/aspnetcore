#if !DOTNET5_4 // TODO: Temp copy. Remove once we target net46.
using System;
namespace System.Threading
{
    internal struct DeferredDisposableLifetime<T> where T : class, IDeferredDisposable
    {
        private int _count;
        public bool AddRef(T obj)
        {
            while (true)
            {
                int num = Volatile.Read(ref this._count);
                if (num < 0)
                {
                    break;
                }
                int num2 = checked(num + 1);
                if (Interlocked.CompareExchange(ref this._count, num2, num) == num)
                {
                    return true;
                }
            }
            throw new ObjectDisposedException(typeof(T).ToString());
        }
        public void Release(T obj)
        {
            int num2;
            int num3;
            while (true)
            {
                int num = Volatile.Read(ref this._count);
                if (num > 0)
                {
                    num2 = num - 1;
                    if (Interlocked.CompareExchange(ref this._count, num2, num) == num)
                    {
                        break;
                    }
                }
                else
                {
                    num3 = num + 1;
                    if (Interlocked.CompareExchange(ref this._count, num3, num) == num)
                    {
                        goto Block_3;
                    }
                }
            }
            if (num2 == 0)
            {
                obj.OnFinalRelease(false);
            }
            return;
            Block_3:
            if (num3 == -1)
            {
                obj.OnFinalRelease(true);
            }
        }
        public void Dispose(T obj)
        {
            int num2;
            while (true)
            {
                int num = Volatile.Read(ref this._count);
                if (num < 0)
                {
                    break;
                }
                num2 = -1 - num;
                if (Interlocked.CompareExchange(ref this._count, num2, num) == num)
                {
                    goto Block_1;
                }
            }
            return;
            Block_1:
            if (num2 == -1)
            {
                obj.OnFinalRelease(true);
            }
        }
    }
}
#endif