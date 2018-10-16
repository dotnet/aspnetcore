using System.Collections.Generic;
using TriageBuildFailures.Abstractions;

namespace TriageBuildFailures.VSTS
{
    public class VSTSConfig : CIConfigBase
    {
        public string Account { get; set; }

        public string BuildPath { get; set; }

        public string PersonalAccessToken { get; set; }
    }
}
