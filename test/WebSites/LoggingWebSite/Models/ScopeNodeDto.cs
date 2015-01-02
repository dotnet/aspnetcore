using System;
using System.Collections.Generic;

namespace LoggingWebSite
{
    public class ScopeNodeDto
    {
        public List<ScopeNodeDto> Children { get; private set; } = new List<ScopeNodeDto>();

        public List<LogInfoDto> Messages { get; private set; } = new List<LogInfoDto>();

        public object State { get; set; }

        public Type StateType { get; set; }

        public string LoggerName { get; set; }
    }
}