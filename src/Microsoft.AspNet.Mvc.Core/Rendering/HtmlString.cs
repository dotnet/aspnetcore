namespace Microsoft.AspNet.Mvc.Rendering
{
    public class HtmlString
    {
        private static readonly HtmlString _empty = new HtmlString(string.Empty);

        private readonly string _input;

        public HtmlString(string input)
        {
            _input = input;
        }

        public static HtmlString Empty
        {
            get
            {
                return _empty;
            }
        }

        public override string ToString()
        {
            return _input;
        }
    }
}
