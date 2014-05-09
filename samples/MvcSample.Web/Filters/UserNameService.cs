using Microsoft.AspNet.Mvc;

namespace MvcSample.Web.Filters
{
    public class UserNameService
    {
        private static readonly string[] _userNames = new[] { "Jon", "David", "Goliath" };
        private static int _index;
        
        public string GetName()
        {
            return _userNames[_index++ % 3];
        }
    }
}
