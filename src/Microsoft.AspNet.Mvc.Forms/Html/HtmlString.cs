
namespace Microsoft.AspNet.Mvc
{
    public class HtmlString
    {
        private readonly string _input;

        public HtmlString(string input)
        {
            _input = input;
        }

        public override string ToString()
        {
            return _input;
        }
    }
}
