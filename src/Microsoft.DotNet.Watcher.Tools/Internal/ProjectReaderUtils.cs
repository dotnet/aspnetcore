// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Text;

namespace Microsoft.DotNet.Watcher.Internal
{
    public class ProjectReader
    {
        public static bool TryReadProject(string projectFile, out Project project, out string errors)
        {
            errors = null;
            project = null;

            ProjectModel.Project runtimeProject;
            if (!TryGetProject(projectFile, out runtimeProject, out errors))
            {
                return false;
            }

            try
            {
                project = new Project(runtimeProject);
            }
            catch (Exception ex)
            {
                errors = CollectMessages(ex);
                return false;
            }

            return true;
        }

        private static bool TryGetProject(string projectFile, out ProjectModel.Project project, out string errorMessage)
        {
            try
            {
                if (!ProjectModel.ProjectReader.TryGetProject(projectFile, out project))
                {
                    if (project?.Diagnostics != null && project.Diagnostics.Any())
                    {
                        errorMessage = string.Join(Environment.NewLine, project.Diagnostics.Select(e => e.ToString()));
                    }
                    else
                    {
                        errorMessage = "Failed to read project.json";
                    }
                }
                else
                {
                    errorMessage = null;
                    return true;
                }
            }
            catch (Exception ex)
            {
                errorMessage = CollectMessages(ex);
            }

            project = null;
            return false;
        }

        private static string CollectMessages(Exception exception)
        {
            var builder = new StringBuilder();
            builder.AppendLine(exception.Message);

            var aggregateException = exception as AggregateException;
            if (aggregateException != null)
            {
                foreach (var message in aggregateException.Flatten().InnerExceptions.Select(CollectMessages))
                {
                    builder.AppendLine(message);
                }
            }

            while (exception.InnerException != null)
            {
                builder.AppendLine(CollectMessages(exception.InnerException));
                exception = exception.InnerException;
            }
            return builder.ToString();
        }
    }
}
