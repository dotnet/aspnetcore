#if NET45
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class CscBasedCompilationService : ICompilationService
    {
        private static readonly string _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        private readonly string _path;

        public CscBasedCompilationService()
        {
            _path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                                 @"Microsoft.NET\Framework\v4.0.30319\csc.exe");
        }

        public async Task<CompilationResult> Compile(string contents)
        {
            Directory.CreateDirectory(_tempDir);
            string inFile = Path.Combine(_tempDir, Path.GetRandomFileName() + ".cs");
            string outFile = Path.Combine(_tempDir, Path.GetRandomFileName() + ".dll");
            StringBuilder args = new StringBuilder("/target:library ");
            args.AppendFormat("/out:\"{0}\" ", outFile);

            string binDir = Path.Combine(Directory.GetCurrentDirectory(), "bin");
            // In the k-world, CurrentDir happens to be the bin dir
            binDir = Directory.Exists(binDir) ? binDir : Directory.GetCurrentDirectory();
            foreach (var file in Directory.EnumerateFiles(binDir, "*.dll"))
            {
                args.AppendFormat("/R:\"{0}\" ", file);
            }
            args.AppendFormat("\"{0}\"", inFile);
            var outputStream = new MemoryStream();

            // common execute
            Process process = CreateProcess(args.ToString());
            int exitCode;
            try
            {
                File.WriteAllText(inFile, contents);
                exitCode = await Start(process, outputStream);
            }
            finally
            {
                File.Delete(inFile);
            }


            string output = GetString(outputStream);
            if (exitCode != 0)
            {
                IEnumerable<CompilationMessage> messages = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                                                                 .Skip(3)
                                                                 .Select(e => new CompilationMessage(e));
                return CompilationResult.Failed(String.Empty, messages);
            }

            var type = Assembly.LoadFrom(outFile)
                               .GetExportedTypes()
                               .First();
            return CompilationResult.Successful(String.Empty, type);
        }


        private string GetString(MemoryStream stream)
        {
            if (stream.Length > 0)
            {
                return Encoding.UTF8.GetString(stream.GetBuffer(), 0, (int)stream.Length);
            }

            return String.Empty;
        }

        private static async Task<int> Start(Process process, Stream output)
        {
            var tcs = new TaskCompletionSource<int>();
            process.EnableRaisingEvents = true;
            process.Exited += (sender, eventArgs) =>
            {
                tcs.SetResult(process.ExitCode);
            };

            process.Start();

            var copyTask = process.StandardOutput.BaseStream.CopyToAsync(output);
            await Task.WhenAll(tcs.Task, copyTask);

            return process.ExitCode;
        }

        internal Process CreateProcess(string arguments)
        {
            var psi = new ProcessStartInfo
            {
                FileName = _path,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = false,
                ErrorDialog = false,
                Arguments = arguments
            };

            psi.StandardOutputEncoding = Encoding.UTF8;
            psi.StandardErrorEncoding = Encoding.UTF8;

            var process = new Process()
            {
                StartInfo = psi
            };

            return process;
        }
    }
}
#endif