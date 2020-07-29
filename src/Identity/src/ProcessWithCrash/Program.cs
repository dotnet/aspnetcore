using System;
using System.Threading;
using System.Threading.Tasks;

namespace ProcessWithCrash
{
    public class Program
    {
        static Task Main(string[] args)
        {
            var tcs = new TaskCompletionSource<object>();

            ThreadPool.QueueUserWorkItem(state =>
            {
                NullReference(null);

                tcs.TrySetResult(null);
            }, 
            null);

            return tcs.Task;
        }

        private static void NullReference(object o)
        {
            o.ToString();
        }
    }
}
