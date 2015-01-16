using System;

namespace FormatFilterWebSite
{
    public class Product
    {
        public int SampleInt { get; set; }

        public override string ToString()
        {
            return "SampleInt:" + SampleInt;
        }
    }
}