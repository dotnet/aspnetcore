namespace Microsoft.HttpRepl.Commands
{
    public class Formatter
    {
        //private readonly List<int> _prefix = new List<int>();
        private int _prefix;
        private int _maxDepth;

        public void RegisterEntry(int prefixLength, int depth)
        {
            //while (_prefix.Count < depth + 1)
            //{
            //    _prefix.Add(0);
            //}

            if (depth > _maxDepth)
            {
                _maxDepth = depth;
            }

            if (prefixLength > _prefix)
            {
                _prefix = prefixLength;
            }
        }

        public string Format(string prefix, string entry, int level)
        {
            string indent = "".PadRight(level * 4);
            return (indent + prefix).PadRight(_prefix + 3 + _maxDepth * 4) + entry;
        }
    }
}