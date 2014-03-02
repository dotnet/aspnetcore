using System.Collections.Generic;

namespace TCDependencyManager
{
    public class Repository
    {
        private readonly HashSet<Repository> _dependencies = new HashSet<Repository>();

        public string Id { get; set; }

        public string Name { get; set; }

        public HashSet<Repository> Dependencies { get { return _dependencies; } }
    }
}
