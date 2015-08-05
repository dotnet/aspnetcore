using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Server.Kestrel.GeneratedCode
{
    public class Program
    {
        public int Main(string[] args)
        {
            var text = KnownHeaders.GeneratedFile();

            if (args.Length == 1)
            {
                var existing = File.Exists(args[0]) ? File.ReadAllText(args[0]) : "";
                if (!string.Equals(text, existing))
                {
                    File.WriteAllText(args[0], text);
                }
            }
            else
            {
                Console.WriteLine(text);
            }
            return 0;
        }
    }
}
