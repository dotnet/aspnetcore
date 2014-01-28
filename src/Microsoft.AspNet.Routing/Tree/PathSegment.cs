using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.AspNet.Routing.Tree
{
    internal class PathSegment : ITreeSegment
    {
        public PathSegment(string path)
        {
            this.Path = path;
        }

        private string Path
        {
            get;
            set;
        }
    }
}
