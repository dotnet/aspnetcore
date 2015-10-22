#if !DOTNET5_4 // TODO: Temp copy. Remove once we target net46.
using System;
namespace System.Threading
{
    internal interface IDeferredDisposable
    {
        void OnFinalRelease(bool disposed);
    }
}
#endif