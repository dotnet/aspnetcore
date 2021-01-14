using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace blazorhosted.Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine(typeof(IWebHost));
            GC.KeepAlive(typeof(RazorClassLibrary.Class1));
        }
    }
}
