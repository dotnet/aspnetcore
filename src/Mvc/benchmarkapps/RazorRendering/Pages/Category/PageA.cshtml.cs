using Data;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Pages
{
    public class PageA : Page
    {
        public int Value { get; } = 0;
        public int Value1 { get; } = 0;
        public int Value2 { get; } = 0;
        public int Value3 { get; } = 1;
        public bool Condition { get; } = true;

        public string Name { get; } = "A Name";

        public List<DataA> Data1 { get; }
        public List<DataB> Data2 { get; }

        public PageA(List<DataA> dataA, List<DataB> dataB, ILogger<PageA> logger) : base(logger)
        {
            Data1 = dataA;
            Data2 = dataB;
        }

        public async Task OnGetAsync()
        {
            PageTitle = "PageA Title";
            PageIcon = "sicon dialogue_pagea";
            await Task.Delay(0);
        }
    }

}