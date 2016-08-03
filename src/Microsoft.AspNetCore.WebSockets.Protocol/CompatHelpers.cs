using System.Threading.Tasks;

namespace System.Net.WebSockets
{
    // Needed to support the WebSockets code from CoreFX.
    internal static class CompatHelpers
    {
        internal static readonly Task CompletedTask;

        static CompatHelpers()
        {
            var tcs = new TaskCompletionSource<object>();
            tcs.SetResult(null);
            CompletedTask = tcs.Task;
        }

        public static Task FromException(Exception ex)
        {
#if NET451
            return FromException<object>(ex);
#else
            return Task.FromException(ex);
#endif
        }

        public static Task<T> FromException<T>(Exception ex)
        {
#if NET451
            var tcs = new TaskCompletionSource<T>();
            tcs.SetException(ex);
            return tcs.Task;
#else
            return Task.FromException<T>(ex);
#endif
        }

        internal static T[] Empty<T>()
        {
#if NET451
            return new T[0];
#else
            return Array.Empty<T>();
#endif
        }
    }

    // This is just here to be used by a nameof in the CoreFX code.
    //internal static class ClientWebSocket { }
}
