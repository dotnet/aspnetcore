using System.Collections.Generic;
using Newtonsoft.Json;

namespace TCDependencyManager
{
    public class SnapshotDependencies
    {
        public int Count { get; set; }

        [JsonProperty("snapshot-dependency")]
        public List<SnapshotDepedency> Dependencies { get; set; }
    }

    public class SnapshotDepedency
    {
        public string Id { get; set; }

        public string Type { get; set; }

        public Properties Properties { get; set; }

        [JsonProperty("source-buildType")]
        public BuildType BuildType { get; set; }
    }

    public class Properties
    {
        public List<NameValuePair> Property { get; set; }
    }

    public class BuildType
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string ProjectId { get; set; }

        public string ProjectName { get; set; }
    }

    public class NameValuePair
    {
        public NameValuePair(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; set; }

        public string Value { get; set; }
    }
}
