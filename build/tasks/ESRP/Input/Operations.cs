using System;
using Newtonsoft.Json.Linq;

namespace Microsoft.Build.OOB.ESRP
{
    public class Operations
    {
        private string _toolVersion;
        private string _toolName;

        public string KeyCode
        {
            get;
            set;
        }

        public OperationCode OperationCode
        {
            get;
            set;
        }

        public JObject Parameters
        {
            get;
            set;
        }

        public string ToolName
        {
            get
            {
                if (String.IsNullOrEmpty(_toolName))
                {
                    _toolName = ESRP.ToolName.SignTool;
                }

                return _toolName;
            }
            set
            {
                _toolName = value;
            }
        }

        public string ToolVersion
        {
            get
            {
                if (String.IsNullOrEmpty(_toolVersion))
                {
                    _toolVersion = ESRP.ToolVersion.V1;
                }

                return _toolVersion;
            }
            set
            {
                _toolVersion = value;
            }
        }
    };
}
