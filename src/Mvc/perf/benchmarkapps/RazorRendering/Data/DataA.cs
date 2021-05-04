using Microsoft.AspNetCore.Html;

namespace Data
{
    public class DataA
    {
        public DataA(int id, HtmlString icon, HtmlString html, string name, int seconds, int max, float perHour)
        {
            Id = id;
            Icon = icon;
            Html = html;
            Name = name;
            Seconds = seconds;
            Max = max;
            PerHour = perHour;
        }

        public int Id { get; }
        public HtmlString Icon { get; }
        public HtmlString Html { get; }
        public string Name { get; }
        public int Seconds { get; }
        public int Max { get; }
        public float PerHour { get; }
    }
}
