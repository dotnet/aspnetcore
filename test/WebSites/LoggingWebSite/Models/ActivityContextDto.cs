using System;

namespace LoggingWebSite
{
    public class ActivityContextDto
    {
        public Guid Id { get; set; }

        public RequestInfoDto RequestInfo { get; set; }

        public ScopeNodeDto Root { get; set; }

        public bool RepresentsScope { get; set; }
    }
}