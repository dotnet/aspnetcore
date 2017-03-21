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
            return Task.FromException(ex);
        }

        public static Task<T> FromException<T>(Exception ex)
        {
            return Task.FromException<T>(ex);
        }

        internal static T[] Empty<T>()
        {
            return Array.Empty<T>();
        }
    }

    // This is just here to be used by a nameof in the CoreFX code.
    //internal static class ClientWebSocket { }
}
