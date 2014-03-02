using System.Collections.Generic;

namespace TCDependencyManager
{
    public class Project
    {
        public Repository Repo { get; set; }

        public string ProjectName { get; set; }

        public List<string> Dependencies { get; set; }
    }
}
