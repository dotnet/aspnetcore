using System;
using System.Diagnostics;
using Microsoft.Framework.Primitives;

namespace SampleApp
{
    public class Program
    {
        public void Main(string[] args)
        {
            for (int i = 0; i < 10; i++)
            {
                Stopwatch timer = new Stopwatch();
                timer.Start();
                string myString;
                string[] myArray;
                StringValues myValues;
                for (int j = 0; j < 100000000; j++)
                {
                    myString = new string('a', 40);
                    myArray = new[] { myString };
                    // myValues = new StringValues(myString);
                    myValues = new StringValues(myArray);
                }
                timer.Stop();
                Console.WriteLine(timer.Elapsed + ", " + Environment.WorkingSet);
            }
        }
    }
}
