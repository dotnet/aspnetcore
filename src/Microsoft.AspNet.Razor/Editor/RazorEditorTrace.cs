// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Razor.Resources;
using Microsoft.AspNet.Razor.Text;

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
                    Trace.WriteLine(String.Format(
                        CultureInfo.CurrentCulture,
                        RazorResources.Trace_Startup,
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
                Trace.WriteLine(String.Format(
                    CultureInfo.CurrentCulture,
                    RazorResources.Trace_Format,
                    String.Format(CultureInfo.CurrentCulture, format, args)));
#endif
            }
        }
    }
}
