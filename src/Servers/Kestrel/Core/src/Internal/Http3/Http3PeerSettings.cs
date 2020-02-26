// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3
{
    internal class Http3PeerSettings
    {
        public const long DefaultMaxHeaderListSize = long.MaxValue;
        public const long DefaultQPackMaxTableCapacity = 0;
        public const long DefaultQPackBlockedStreams = 0;

        public long MaxHeaderListSize { get; set; } = DefaultMaxHeaderListSize;
        public long QPackMaxTableCapacity { get; set; } = DefaultQPackMaxTableCapacity;
        public long QPackBlockedStreams { get; set; } = DefaultQPackBlockedStreams;

        public void Update(IList<Http3PeerSetting> settings)
        {
            foreach (var setting in settings)
            {
                var value = setting.Value;
                switch (setting.Parameter)
                {
                    case Http3SettingsParameter.MaxHeaderListSize:
                        MaxHeaderListSize = value;
                        break;
                    case Http3SettingsParameter.QPackBlockedStreams:
                        QPackMaxTableCapacity = value;
                        break;
                    case Http3SettingsParameter.QPackMaxTableCapacity:
                        QPackBlockedStreams = value;
                        break;
                }
            }
        }

        internal IList<Http3PeerSetting> GetNonProtocolDefaults()
        {
            var list = new List<Http3PeerSetting>(1);

            if (MaxHeaderListSize != DefaultMaxHeaderListSize)
            {
                list.Add(new Http3PeerSetting(Http3SettingsParameter.MaxHeaderListSize, MaxHeaderListSize));
            }

            if (QPackMaxTableCapacity != DefaultQPackMaxTableCapacity)
            {
                list.Add(new Http3PeerSetting(Http3SettingsParameter.QPackMaxTableCapacity, QPackMaxTableCapacity));
            }

            if (QPackBlockedStreams != DefaultQPackBlockedStreams)
            {
                list.Add(new Http3PeerSetting(Http3SettingsParameter.QPackBlockedStreams, QPackBlockedStreams));
            }

            return list;
        }
    }
}
