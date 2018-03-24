using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.CommandLineUtils;

namespace CodeSign
{
    class Program
    {
        static int Main(string[] args)
        {
            var app = new CommandLineApplication();
            var approvers = app.Option("--approver", "Approver", CommandOptionType.MultipleValue);
            var certificates = app.Option("--cert", "Certificates", CommandOptionType.MultipleValue);
            var localBuild = app.Option("--local-build", "Local (test) build", CommandOptionType.NoValue);
            var description = app.Option("--description", "Description", CommandOptionType.SingleValue);
            var displayName = app.Option("--display-name", "Display Name", CommandOptionType.SingleValue);
            var jobName = app.Option("--job-name", "Job Name", CommandOptionType.SingleValue);
            var displayUrl = app.Option("--display-url", "Display Url", CommandOptionType.SingleValue);
            var batchSize = app.Option("--batch-size", "Batch size", CommandOptionType.SingleValue);
            var logDir = app.Option("--log-dir", "LogDir", CommandOptionType.SingleValue);
            var files = app.Option("-f", "File", CommandOptionType.MultipleValue);

            app.OnExecute(() =>
            {
                var submitCodeSignJob = new SubmitCodeSignJob
                {
                    Approvers = approvers.Values,
                    Certificates = certificates.Values,
                    LocalBuild = localBuild.HasValue(),
                    Description = description.Value(),
                    DisplayName = displayName.Value(),
                    DisplayUrl = displayUrl.Value(),
                    JobName = jobName.Value(),
                    Files = files.Values,
                    LogDir = logDir.Value() ?? AppDomain.CurrentDomain.BaseDirectory,
                };

                using (submitCodeSignJob)
                {
                    submitCodeSignJob.Execute();
                }

                return 0;
            });

            try
            {
                return app.Execute(ExpandResponseFiles(args));
            }
            catch (CommandParsingException ex)
            {
                Console.Error.WriteLine(ex.Message);
                app.ShowHelp();
                return 1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"error: {ex}");
                return 1;
            }
        }

        private static string[] ExpandResponseFiles(string[] args)
        {
            var expandedArgs = new List<string>();
            foreach (var arg in args)
            {
                if (!arg.StartsWith("@", StringComparison.Ordinal))
                {
                    expandedArgs.Add(arg);
                }
                else
                {
                    var fileName = arg.Substring(1);
                    expandedArgs.AddRange(File.ReadLines(fileName));
                }
            }

            return expandedArgs.ToArray();
        }
    }
}
