using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;

namespace BlazorMultipleApps.FirstClient
{
    public class Program
    {
        public static void Main(string[] args)
        {
            GC.KeepAlive(typeof(System.Text.Json.JsonSerializer));
        }
    }
}
