using System;
using Microsoft.Framework.Logging;

namespace LoggingWebSite
{
    public class LogInfoDto
    {
        public string LoggerName { get; set; }

        /// <summary>
        /// Type of object representing the State. This is useful for tests
        /// to filter the results
        /// </summary>
        public Type StateType { get; set; }

        public LogLevel LogLevel { get; set; }

        public int EventID { get; set; }
        
        public object State { get; set; }

        public Exception Exception { get; set; }
    }
}