// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Rewrite.ApacheModRewrite
{
    // For more information of flags, and what flags we currently support:
    // https://github.com/aspnet/BasicMiddleware/issues/66
    // http://httpd.apache.org/docs/current/expr.html#vars
    internal class Flags
    {
        public IDictionary<FlagType, string> FlagDictionary { get; }

        public Flags(IDictionary<FlagType, string> flags)
        {
            FlagDictionary = flags;
        }

        public Flags()
        {
            FlagDictionary = new Dictionary<FlagType, string>();
        }

        public void SetFlag(FlagType flag, string value)
        {
            if (value == null)
            {
                value = string.Empty;
            }
            FlagDictionary[flag] = value;
        }

        public bool GetValue(FlagType flag, out string value)
        {
            string res;
            if (!FlagDictionary.TryGetValue(flag, out res))
            {
                value = null;
                return false;
            }
            value = res;
            return true;
        }

        public string this[FlagType flag]
        {
            get
            {
                string res;
                if (!FlagDictionary.TryGetValue(flag, out res))
                {
                    return null;
                }
                return res;
            }
            set
            {
                FlagDictionary[flag] = value ?? string.Empty;
            }
        }

        public bool HasFlag(FlagType flag)
        {
            string res;
            return FlagDictionary.TryGetValue(flag, out res);
        }
    }
}
