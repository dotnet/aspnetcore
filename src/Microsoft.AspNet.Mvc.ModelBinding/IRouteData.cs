using System;
using Microsoft.AspNet.Abstractions;

namespace Microsoft.AspNet.Mvc.Routing
{
    // Move to routing middleware
    public interface IRouteData
    {
        string GetRouteValue(string name);
    }

    public class FakeRouteData : IRouteData
    {
        private readonly string[] _parts;

        public FakeRouteData(HttpContext context)
        {
            _parts = (context.Request.PathBase + context.Request.Path).Value.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
        }

        public string GetRouteValue(string name)
        {
            if (name.Equals("controller", StringComparison.OrdinalIgnoreCase))
            {
                return GetPartOrDefault(0, "HomeController");
            }
            else if (name.Equals("action", StringComparison.OrdinalIgnoreCase))
            {
                return GetPartOrDefault(1, "Index");
            }

            return null;
        }

        private string GetPartOrDefault(int index, string defaultValue)
        {
            return index < _parts.Length ? _parts[index] : defaultValue;
        }
    }
}
