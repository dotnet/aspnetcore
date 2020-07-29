using System;
using System.Threading;

namespace ProcessWithAv
{
    public class Program
    {
        static void Main(string[] args)
        {
            Thread.Sleep(5000);

            unsafe
            {
                *(int*)0x12345678 = 0x1;
            }
        }
    }
}
