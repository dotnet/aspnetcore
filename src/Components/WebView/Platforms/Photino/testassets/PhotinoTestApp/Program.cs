using PhotinoNET;
using System;

namespace PhotinoTestApp
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            new PhotinoWindow("Hello, world!")
                .Load("wwwroot/index.html")
                .WaitForClose();
        }
    }
}
