using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin.FileSystems;

namespace Microsoft.AspNet.CoreServices
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

        public async Task<CompilationResult> Compile(IFileInfo fileInfo)
        {
            Directory.CreateDirectory(_tempDir);
            string outFile = Path.Combine(_tempDir, Path.GetRandomFileName() + ".dll");
            StringBuilder args = new StringBuilder("/target:library ");
            args.AppendFormat("/out:\"{0}\" ", outFile);
            foreach (var file in  Directory.EnumerateFiles(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "*.dll"))
            {
                args.AppendFormat("/R:\"{0}\" ", file);
            }
            args.AppendFormat("\"{0}\"", fileInfo.PhysicalPath);
            var outputStream = new MemoryStream();
            var errorStream = new MemoryStream();

            // common execute
            var process = CreateProcess(args.ToString());
            int exitCode = await Start(process, outputStream, errorStream);

            string output = GetString(outputStream);
            string error = GetString(errorStream);
            if (exitCode != 0)
            {
                return CompilationResult.Failed(String.Empty, error.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                                                                   .Select(e => new CompilationMessage(e)));
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

        private static async Task<int> Start(Process process, Stream output, Stream error)
        {
            var tcs = new TaskCompletionSource<int>();
            process.EnableRaisingEvents = true;
            process.Exited += (sender, eventArgs) =>
            {
                tcs.SetResult(process.ExitCode);
            };

            process.Start();

            var tasks = new[]
            {
                process.StandardOutput.BaseStream.CopyToAsync(output),
                process.StandardError.BaseStream.CopyToAsync(error)
            };

            int result = await tcs.Task;

            // Process has exited, draining the stdout and stderr
            await Task.WhenAll(tasks);

            return result;
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

