using System.Collections.Generic;

namespace TCDependencyManager
{
    public class Triggers
    {
        public List<Trigger> Trigger { get; set; }
    }

    public class Trigger
    {
        public string Id { get; set; }

        public string Type { get; set; }

        public Properties Properties { get; set; }
    }
}
