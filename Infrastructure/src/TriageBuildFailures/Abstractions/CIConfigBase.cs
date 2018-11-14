using System.Collections.Generic;

namespace TriageBuildFailures.Abstractions
{
    public abstract class CIConfigBase
    {
        public IEnumerable<string> BuildIdIgnoreList { get; set; }
    }
}
