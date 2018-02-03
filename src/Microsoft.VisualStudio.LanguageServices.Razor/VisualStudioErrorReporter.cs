// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Razor
{
    internal class VisualStudioErrorReporter : ErrorReporter
    {
        private readonly IServiceProvider _services;

        public VisualStudioErrorReporter(IServiceProvider services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            _services = services;
        }

        public override void ReportError(Exception exception)
        {
            if (exception == null)
            {
                return;
            }

            var activityLog = GetActivityLog();
            if (activityLog != null)
            {
                var hr = activityLog.LogEntry(
                    (uint)__ACTIVITYLOG_ENTRYTYPE.ALE_ERROR,
                    "Razor Language Services",
                    $"Error encountered:{Environment.NewLine}{exception}");
                ErrorHandler.ThrowOnFailure(hr);
            }
        }

        public override void ReportError(Exception exception, ProjectSnapshot project)
        {
            if (exception == null)
            {
                return;
            }

            var activityLog = GetActivityLog();
            if (activityLog != null)
            {
                var hr = activityLog.LogEntry(
                    (uint)__ACTIVITYLOG_ENTRYTYPE.ALE_ERROR,
                    "Razor Language Services",
                    $"Error encountered from project '{project?.FilePath}':{Environment.NewLine}{exception}");
                ErrorHandler.ThrowOnFailure(hr);
            }
        }

        public override void ReportError(Exception exception, Project workspaceProject)
        {
            if (exception == null)
            {
                return;
            }

            var activityLog = GetActivityLog();
            if (activityLog != null)
            {
                var hr = activityLog.LogEntry(
                    (uint)__ACTIVITYLOG_ENTRYTYPE.ALE_ERROR,
                    "Razor Language Services",
                    $"Error encountered from project '{workspaceProject?.Name}' '{workspaceProject?.FilePath}':{Environment.NewLine}{exception}");
                ErrorHandler.ThrowOnFailure(hr);
            }
        }

        private IVsActivityLog GetActivityLog()
        {
            return _services.GetService(typeof(SVsActivityLog)) as IVsActivityLog;
        }
    }
}
