// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Rewrite.Internal.ModRewrite
{
    // TODO Refactor Condition Flags and Rule Flags under base flag class
    public class ConditionFlags
    {
        private IDictionary<string, ConditionFlagType> _conditionFlagLookup = new Dictionary<string, ConditionFlagType>(StringComparer.OrdinalIgnoreCase) {
            { "nc", ConditionFlagType.NoCase},
            { "nocase", ConditionFlagType.NoCase },
            { "or", ConditionFlagType.Or},
            { "ornext", ConditionFlagType.Or },
            { "nv", ConditionFlagType.NoVary},
            { "novary", ConditionFlagType.NoVary}
            };

        public IDictionary<ConditionFlagType, string> FlagDictionary { get; }

        public ConditionFlags(IDictionary<ConditionFlagType, string> flags)
        {
            FlagDictionary = flags;
        }

        public ConditionFlags()
        {
            FlagDictionary = new Dictionary<ConditionFlagType, string>();
        }
        public void SetFlag(string flag)
        {
            SetFlag(flag, null);
        }

        public void SetFlag(string flag, string value)
        {
            ConditionFlagType res;
            if (!_conditionFlagLookup.TryGetValue(flag, out res))
            {
                throw new ArgumentException("Invalid flag");
            }
            SetFlag(res, value);
        }

        public void SetFlag(ConditionFlagType flag, string value)
        {
            if (value == null)
            {
                value = string.Empty;
            }
            FlagDictionary[flag] = value;
        }

        public string GetFlag(ConditionFlagType flag)
        {
            CleanupResources();
            string res;
            if (!FlagDictionary.TryGetValue(flag, out res))
            {
                return null;
            }
            return res;
        }

        public string this[ConditionFlagType flag]
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

        public bool HasFlag(ConditionFlagType flag)
        {
            CleanupResources();
            string res;
            return FlagDictionary.TryGetValue(flag, out res);
        }

        // If this method is called, all flags have been processed, 
        // therefore to clean up memory, delete dictionary.
        private void CleanupResources()
        {
            if (_conditionFlagLookup != null)
            {
                _conditionFlagLookup = null;
            }
        }
    }
}
