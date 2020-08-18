using System;
using System.Threading.Tasks;

namespace ProcessWithHang
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            await new TaskCompletionSource<object>().Task;
        }
    }
}
