using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3
{
    internal readonly struct Http3PeerSetting
    {
        public Http3PeerSetting(Http3SettingsParameter parameter, long value)
        {
            Parameter = parameter;
            Value = value;
        }

        public Http3SettingsParameter Parameter { get; }

        public long Value { get; }
    }
}
