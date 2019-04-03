using System;
using System.Collections.Generic;

namespace TriageBuildFailures.VSTS.Models
{
    public class VSTSTimeline
    {
        public IEnumerable<VSTSTimelineRecord> Records { get; set; }
        public string Id { get; set; }
        public string ChangeId { get; set; }

        public class VSTSTimelineRecord
        {
            public int Attempt { get; set; }
            public int ErrorCount { get; set; }
            public string Name { get; set; }
            public string Id { get; set; }
            public string Type { get; set; }
            public DateTime? StartTime { get; set; }
            public DateTime? FinishTime { get; set; }
            public string State { get; set; }
            public VSTSTaskResult? Result { get; set; }
            public VSTSBuildLogReference Log { get; set; }

            public class VSTSBuildLogReference
            {
                public string Id { get; set; }
                public string Type { get; set; }
                public string Url { get; set; }
            }
        }
    }
}
