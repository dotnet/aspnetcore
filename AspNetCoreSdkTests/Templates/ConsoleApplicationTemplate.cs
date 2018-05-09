using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AspNetCoreSdkTests.Templates
{
    public class ConsoleApplicationTemplate : ClassLibraryTemplate
    {
        public ConsoleApplicationTemplate() { }

        public override string Name => "console";

        public override string OutputPath => Path.Combine("Debug", "netcoreapp2.1", RuntimeIdentifier.Path);

        public override TemplateType Type => TemplateType.ConsoleApplication;

        private IDictionary<RuntimeIdentifier, Func<IEnumerable<string>>> _additionalObjFilesAfterBuild =>
            new Dictionary<RuntimeIdentifier, Func<IEnumerable<string>>>()
            {
                { RuntimeIdentifier.None, () => Enumerable.Empty<string>() },
                { RuntimeIdentifier.Win_x64, () => new[]
                    {
                        Path.Combine("netcoreapp2.1", RuntimeIdentifier.Path, "host", $"{Name}.exe"),
                    }
                }
            };

        public override IEnumerable<string> ExpectedObjFilesAfterBuild =>
            Enumerable.Concat(base.ExpectedObjFilesAfterBuild, _additionalObjFilesAfterBuild[RuntimeIdentifier]());

        private IDictionary<RuntimeIdentifier, Func<IEnumerable<string>>> _additionalBinFilesAfterBuild =>
            new Dictionary<RuntimeIdentifier, Func<IEnumerable<string>>>()
            {
                { RuntimeIdentifier.None, () => new[]
                    {
                        $"{Name}.runtimeconfig.dev.json",
                        $"{Name}.runtimeconfig.json",
                    }.Select(p => Path.Combine(OutputPath, p))
                },
                { RuntimeIdentifier.Win_x64, () => Enumerable.Concat(_additionalBinFilesAfterBuild[RuntimeIdentifier.None](), new[] 
                    {
                        $"{Name}.exe",
                        "hostfxr.dll",
                        "hostpolicy.dll",
                    }.Select(p => Path.Combine(OutputPath, p)))
                }
            };

        public override IEnumerable<string> ExpectedBinFilesAfterBuild =>
            Enumerable.Concat(base.ExpectedBinFilesAfterBuild, _additionalBinFilesAfterBuild[RuntimeIdentifier]());

        private IDictionary<RuntimeIdentifier, Func<IEnumerable<string>>> _additionalFilesAfterPublish =>
            new Dictionary<RuntimeIdentifier, Func<IEnumerable<string>>>()
            {
                { RuntimeIdentifier.None, () => new[]
                    {
                        $"{Name}.runtimeconfig.json",
                    }
                },
                { RuntimeIdentifier.Win_x64, () => Enumerable.Concat(_additionalFilesAfterPublish[RuntimeIdentifier.None](), new[]
                    {
                        $"{Name}.exe",
                        "api-ms-win-core-console-l1-1-0.dll",
                        "api-ms-win-core-datetime-l1-1-0.dll",
                        "api-ms-win-core-debug-l1-1-0.dll",
                        "api-ms-win-core-errorhandling-l1-1-0.dll",
                        "api-ms-win-core-file-l1-1-0.dll",
                        "api-ms-win-core-file-l1-2-0.dll",
                        "api-ms-win-core-file-l2-1-0.dll",
                        "api-ms-win-core-handle-l1-1-0.dll",
                        "api-ms-win-core-heap-l1-1-0.dll",
                        "api-ms-win-core-interlocked-l1-1-0.dll",
                        "api-ms-win-core-libraryloader-l1-1-0.dll",
                        "api-ms-win-core-localization-l1-2-0.dll",
                        "api-ms-win-core-memory-l1-1-0.dll",
                        "api-ms-win-core-namedpipe-l1-1-0.dll",
                        "api-ms-win-core-processenvironment-l1-1-0.dll",
                        "api-ms-win-core-processthreads-l1-1-0.dll",
                        "api-ms-win-core-processthreads-l1-1-1.dll",
                        "api-ms-win-core-profile-l1-1-0.dll",
                        "api-ms-win-core-rtlsupport-l1-1-0.dll",
                        "api-ms-win-core-string-l1-1-0.dll",
                        "api-ms-win-core-synch-l1-1-0.dll",
                        "api-ms-win-core-synch-l1-2-0.dll",
                        "api-ms-win-core-sysinfo-l1-1-0.dll",
                        "api-ms-win-core-timezone-l1-1-0.dll",
                        "api-ms-win-core-util-l1-1-0.dll",
                        "api-ms-win-crt-conio-l1-1-0.dll",
                        "api-ms-win-crt-convert-l1-1-0.dll",
                        "api-ms-win-crt-environment-l1-1-0.dll",
                        "api-ms-win-crt-filesystem-l1-1-0.dll",
                        "api-ms-win-crt-heap-l1-1-0.dll",
                        "api-ms-win-crt-locale-l1-1-0.dll",
                        "api-ms-win-crt-math-l1-1-0.dll",
                        "api-ms-win-crt-multibyte-l1-1-0.dll",
                        "api-ms-win-crt-private-l1-1-0.dll",
                        "api-ms-win-crt-process-l1-1-0.dll",
                        "api-ms-win-crt-runtime-l1-1-0.dll",
                        "api-ms-win-crt-stdio-l1-1-0.dll",
                        "api-ms-win-crt-string-l1-1-0.dll",
                        "api-ms-win-crt-time-l1-1-0.dll",
                        "api-ms-win-crt-utility-l1-1-0.dll",
                        "clrcompression.dll",
                        "clretwrc.dll",
                        "clrjit.dll",
                        "coreclr.dll",
                        "dbgshim.dll",
                        "hostfxr.dll",
                        "hostpolicy.dll",
                        "Microsoft.CSharp.dll",
                        "Microsoft.DiaSymReader.Native.amd64.dll",
                        "Microsoft.VisualBasic.dll",
                        "Microsoft.Win32.Primitives.dll",
                        "Microsoft.Win32.Registry.dll",
                        "mscordaccore.dll",
                        "mscordaccore_amd64_amd64_4.6.26426.02.dll",
                        "mscordbi.dll",
                        "mscorlib.dll",
                        "mscorrc.debug.dll",
                        "mscorrc.dll",
                        "netstandard.dll",
                        "sos.dll",
                        "SOS.NETCore.dll",
                        "sos_amd64_amd64_4.6.26426.02.dll",
                        "System.AppContext.dll",
                        "System.Buffers.dll",
                        "System.Collections.Concurrent.dll",
                        "System.Collections.dll",
                        "System.Collections.Immutable.dll",
                        "System.Collections.NonGeneric.dll",
                        "System.Collections.Specialized.dll",
                        "System.ComponentModel.Annotations.dll",
                        "System.ComponentModel.DataAnnotations.dll",
                        "System.ComponentModel.dll",
                        "System.ComponentModel.EventBasedAsync.dll",
                        "System.ComponentModel.Primitives.dll",
                        "System.ComponentModel.TypeConverter.dll",
                        "System.Configuration.dll",
                        "System.Console.dll",
                        "System.Core.dll",
                        "System.Data.Common.dll",
                        "System.Data.dll",
                        "System.Diagnostics.Contracts.dll",
                        "System.Diagnostics.Debug.dll",
                        "System.Diagnostics.DiagnosticSource.dll",
                        "System.Diagnostics.FileVersionInfo.dll",
                        "System.Diagnostics.Process.dll",
                        "System.Diagnostics.StackTrace.dll",
                        "System.Diagnostics.TextWriterTraceListener.dll",
                        "System.Diagnostics.Tools.dll",
                        "System.Diagnostics.TraceSource.dll",
                        "System.Diagnostics.Tracing.dll",
                        "System.dll",
                        "System.Drawing.dll",
                        "System.Drawing.Primitives.dll",
                        "System.Dynamic.Runtime.dll",
                        "System.Globalization.Calendars.dll",
                        "System.Globalization.dll",
                        "System.Globalization.Extensions.dll",
                        "System.IO.Compression.Brotli.dll",
                        "System.IO.Compression.dll",
                        "System.IO.Compression.FileSystem.dll",
                        "System.IO.Compression.ZipFile.dll",
                        "System.IO.dll",
                        "System.IO.FileSystem.AccessControl.dll",
                        "System.IO.FileSystem.dll",
                        "System.IO.FileSystem.DriveInfo.dll",
                        "System.IO.FileSystem.Primitives.dll",
                        "System.IO.FileSystem.Watcher.dll",
                        "System.IO.IsolatedStorage.dll",
                        "System.IO.MemoryMappedFiles.dll",
                        "System.IO.Pipes.AccessControl.dll",
                        "System.IO.Pipes.dll",
                        "System.IO.UnmanagedMemoryStream.dll",
                        "System.Linq.dll",
                        "System.Linq.Expressions.dll",
                        "System.Linq.Parallel.dll",
                        "System.Linq.Queryable.dll",
                        "System.Memory.dll",
                        "System.Net.dll",
                        "System.Net.Http.dll",
                        "System.Net.HttpListener.dll",
                        "System.Net.Mail.dll",
                        "System.Net.NameResolution.dll",
                        "System.Net.NetworkInformation.dll",
                        "System.Net.Ping.dll",
                        "System.Net.Primitives.dll",
                        "System.Net.Requests.dll",
                        "System.Net.Security.dll",
                        "System.Net.ServicePoint.dll",
                        "System.Net.Sockets.dll",
                        "System.Net.WebClient.dll",
                        "System.Net.WebHeaderCollection.dll",
                        "System.Net.WebProxy.dll",
                        "System.Net.WebSockets.Client.dll",
                        "System.Net.WebSockets.dll",
                        "System.Numerics.dll",
                        "System.Numerics.Vectors.dll",
                        "System.ObjectModel.dll",
                        "System.Private.CoreLib.dll",
                        "System.Private.DataContractSerialization.dll",
                        "System.Private.Uri.dll",
                        "System.Private.Xml.dll",
                        "System.Private.Xml.Linq.dll",
                        "System.Reflection.DispatchProxy.dll",
                        "System.Reflection.dll",
                        "System.Reflection.Emit.dll",
                        "System.Reflection.Emit.ILGeneration.dll",
                        "System.Reflection.Emit.Lightweight.dll",
                        "System.Reflection.Extensions.dll",
                        "System.Reflection.Metadata.dll",
                        "System.Reflection.Primitives.dll",
                        "System.Reflection.TypeExtensions.dll",
                        "System.Resources.Reader.dll",
                        "System.Resources.ResourceManager.dll",
                        "System.Resources.Writer.dll",
                        "System.Runtime.CompilerServices.VisualC.dll",
                        "System.Runtime.dll",
                        "System.Runtime.Extensions.dll",
                        "System.Runtime.Handles.dll",
                        "System.Runtime.InteropServices.dll",
                        "System.Runtime.InteropServices.RuntimeInformation.dll",
                        "System.Runtime.InteropServices.WindowsRuntime.dll",
                        "System.Runtime.Loader.dll",
                        "System.Runtime.Numerics.dll",
                        "System.Runtime.Serialization.dll",
                        "System.Runtime.Serialization.Formatters.dll",
                        "System.Runtime.Serialization.Json.dll",
                        "System.Runtime.Serialization.Primitives.dll",
                        "System.Runtime.Serialization.Xml.dll",
                        "System.Security.AccessControl.dll",
                        "System.Security.Claims.dll",
                        "System.Security.Cryptography.Algorithms.dll",
                        "System.Security.Cryptography.Cng.dll",
                        "System.Security.Cryptography.Csp.dll",
                        "System.Security.Cryptography.Encoding.dll",
                        "System.Security.Cryptography.OpenSsl.dll",
                        "System.Security.Cryptography.Primitives.dll",
                        "System.Security.Cryptography.X509Certificates.dll",
                        "System.Security.dll",
                        "System.Security.Principal.dll",
                        "System.Security.Principal.Windows.dll",
                        "System.Security.SecureString.dll",
                        "System.ServiceModel.Web.dll",
                        "System.ServiceProcess.dll",
                        "System.Text.Encoding.dll",
                        "System.Text.Encoding.Extensions.dll",
                        "System.Text.RegularExpressions.dll",
                        "System.Threading.dll",
                        "System.Threading.Overlapped.dll",
                        "System.Threading.Tasks.Dataflow.dll",
                        "System.Threading.Tasks.dll",
                        "System.Threading.Tasks.Extensions.dll",
                        "System.Threading.Tasks.Parallel.dll",
                        "System.Threading.Thread.dll",
                        "System.Threading.ThreadPool.dll",
                        "System.Threading.Timer.dll",
                        "System.Transactions.dll",
                        "System.Transactions.Local.dll",
                        "System.ValueTuple.dll",
                        "System.Web.dll",
                        "System.Web.HttpUtility.dll",
                        "System.Windows.dll",
                        "System.Xml.dll",
                        "System.Xml.Linq.dll",
                        "System.Xml.ReaderWriter.dll",
                        "System.Xml.Serialization.dll",
                        "System.Xml.XDocument.dll",
                        "System.Xml.XmlDocument.dll",
                        "System.Xml.XmlSerializer.dll",
                        "System.Xml.XPath.dll",
                        "System.Xml.XPath.XDocument.dll",
                        "ucrtbase.dll",
                        "WindowsBase.dll",
                    })
                }
            };

        public override IEnumerable<string> ExpectedFilesAfterPublish =>
            Enumerable.Concat(base.ExpectedFilesAfterPublish, _additionalFilesAfterPublish[RuntimeIdentifier]());
    }
}


