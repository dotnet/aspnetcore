// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Diagnostics;
using System.Globalization;

namespace Microsoft.AspNet.Razor.Editor
{
    internal static class RazorEditorTrace
    {
        private static bool? _enabled;

        private static bool IsEnabled()
        {
            if (_enabled == null)
            {
                bool enabled;
                if (Boolean.TryParse(Environment.GetEnvironmentVariable("RAZOR_EDITOR_TRACE"), out enabled))
                {
#if NET45
                    // No Trace in CoreCLR

                    Trace.WriteLine(RazorResources.Trace_Startup(
                        enabled ? RazorResources.Trace_Enabled : RazorResources.Trace_Disabled));
#endif
                    _enabled = enabled;
                }
                else
                {
                    _enabled = false;
                }
            }
            return _enabled.Value;
        }

        [Conditional("EDITOR_TRACING")]
        public static void TraceLine(string format, params object[] args)
        {
            if (IsEnabled())
            {
#if NET45
                // No Trace in CoreCLR

                Trace.WriteLine(RazorResources.Trace_Format(
                    String.Format(CultureInfo.CurrentCulture, format, args)));
#endif
            }
        }
    }
}
