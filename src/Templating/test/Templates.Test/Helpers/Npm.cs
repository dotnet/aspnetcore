using System;
using System.IO;
using Xunit.Abstractions;

namespace Templates.Test.Helpers
{
    public static class Npm
    {
        private static object NpmInstallLock = new object();

        public static void RestoreWithRetry(ITestOutputHelper output, string workingDirectory)
        {
            // "npm restore" sometimes fails randomly in AppVeyor with errors like:
            //    EPERM: operation not permitted, scandir <path>...
            // This appears to be a general NPM reliability issue on Windows which has
            // been reported many times (e.g., https://github.com/npm/npm/issues/18380)
            // So, allow multiple attempts at the restore.
            const int maxAttempts = 3;
            var attemptNumber = 0;
            while (true)
            {
                try
                {
                    attemptNumber++;
                    Restore(output, workingDirectory);
                    break; // Success
                }
                catch (Exception ex)
                {
                    if (attemptNumber < maxAttempts)
                    {
                        output.WriteLine(
                            $"NPM restore in {workingDirectory} failed on attempt {attemptNumber} of {maxAttempts}. " +
                            $"Error was: {ex}");

                        // Clean up the possibly-incomplete node_modules dir before retrying
                        var nodeModulesDir = Path.Combine(workingDirectory, "node_modules");
                        if (Directory.Exists(nodeModulesDir))
                        {
                            Directory.Delete(nodeModulesDir, recursive: true);
                        }
                    }
                    else
                    {
                        output.WriteLine(
                            $"Giving up attempting NPM restore in {workingDirectory} after {attemptNumber} attempts.");
                        throw;
                    }
                }
            }
        }

        private static void Restore(ITestOutputHelper output, string workingDirectory)
        {
            // It's not safe to run multiple NPM installs in parallel
            // https://github.com/npm/npm/issues/2500
            lock (NpmInstallLock)
            {
                output.WriteLine($"Restoring NPM packages in '{workingDirectory}' using npm...");
                ProcessEx.RunViaShell(output, workingDirectory, "npm install");
            }
        }

        public static void Test(ITestOutputHelper outputHelper, string workingDirectory)
        {
            ProcessEx.RunViaShell(outputHelper, workingDirectory, "npm run lint");
            if (!File.Exists(Path.Join(workingDirectory, "angular.json")))
            {
                ProcessEx.RunViaShell(outputHelper, workingDirectory, "npm run test");
            }
        }
    }
}
