using System;
using Microsoft.AspNetCore.Html;

namespace Data
{
    public class DataB
    {
        public DataB(int id, HtmlString icon, string name, int value, DateTimeOffset startDate, DateTimeOffset completeDate)
        {
            Id = id;
            Icon = icon;
            Name = name;
            Value = value;
            StartDate = startDate;
            CompleteDate = completeDate;
        }

        public int Id { get; }
        public HtmlString Icon { get; }
        public string Name { get; }
        public int Value { get; }
        public DateTimeOffset StartDate { get; }
        public DateTimeOffset CompleteDate { get; }
    }
}
