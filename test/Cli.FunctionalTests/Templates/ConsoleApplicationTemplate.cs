// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Cli.FunctionalTests.Util;

namespace Cli.FunctionalTests.Templates
{
    public class ConsoleApplicationTemplate : ClassLibraryTemplate
    {
        public ConsoleApplicationTemplate() { }

        public override string Name => "console";

        public override string OutputPath => Path.Combine("Debug", DotNetUtil.TargetFrameworkMoniker, RuntimeIdentifier.Path);

        public override TemplateType Type => TemplateType.ConsoleApplication;

        private IDictionary<RuntimeIdentifier, Func<IEnumerable<string>>> _additionalObjFilesAfterBuild =>
            new Dictionary<RuntimeIdentifier, Func<IEnumerable<string>>>()
            {
                { RuntimeIdentifier.None, () => Enumerable.Empty<string>() },
                { RuntimeIdentifier.Win_x64, () => new[]
                    {
                        Path.Combine(DotNetUtil.TargetFrameworkMoniker, RuntimeIdentifier.Path, "host", $"{Name}.exe"),
                    }
                },
                { RuntimeIdentifier.Linux_x64, () => new[]
                    {
                        Path.Combine(DotNetUtil.TargetFrameworkMoniker, RuntimeIdentifier.Path, "host", $"{Name}"),
                    }
                },
                { RuntimeIdentifier.OSX_x64, () => _additionalObjFilesAfterBuild[RuntimeIdentifier.Linux_x64]() },
            };

        public override IEnumerable<string> ExpectedObjFilesAfterBuild =>
            base.ExpectedObjFilesAfterBuild
            .Concat(_additionalObjFilesAfterBuild[RuntimeIdentifier]());

        private IDictionary<RuntimeIdentifier, Func<IEnumerable<string>>> _additionalBinFilesAfterBuild =>
            new Dictionary<RuntimeIdentifier, Func<IEnumerable<string>>>()
            {
                { RuntimeIdentifier.None, () => new[]
                    {
                        $"{Name}.runtimeconfig.dev.json",
                        $"{Name}.runtimeconfig.json",
                    }.Select(p => Path.Combine(OutputPath, p))
                },
                { RuntimeIdentifier.Win_x64, () =>
                    _additionalBinFilesAfterBuild[RuntimeIdentifier.None]()
                    .Concat(new[]
                    {
                        $"{Name}.exe",
                        "hostfxr.dll",
                        "hostpolicy.dll",
                    }.Select(p => Path.Combine(OutputPath, p)))
                },
                { RuntimeIdentifier.Linux_x64, () =>
                    _additionalBinFilesAfterBuild[RuntimeIdentifier.None]()
                    .Concat(new[]
                    {
                        $"{Name}",
                        "libhostfxr.so",
                        "libhostpolicy.so",
                    }.Select(p => Path.Combine(OutputPath, p)))
                },
                { RuntimeIdentifier.OSX_x64, () =>
                    _additionalBinFilesAfterBuild[RuntimeIdentifier.Linux_x64]()
                    .Select(f => Regex.Replace(f, ".so$", ".dylib"))
                },
            };

        public override IEnumerable<string> ExpectedBinFilesAfterBuild =>
            base.ExpectedBinFilesAfterBuild
            .Concat(_additionalBinFilesAfterBuild[RuntimeIdentifier]());

        protected override IEnumerable<string> NormalizeFilesAfterPublish(IEnumerable<string> filesAfterPublish)
        {
            // A few files included in self-contained deployments contain version numbers in the filename, which must
            // be replaced so tests can pass on all versions.
            return base.NormalizeFilesAfterPublish(filesAfterPublish)
                .Select(f => Regex.Replace(f, @"_amd64_amd64_[0-9\.]+\.dll$", "_amd64_amd64_[VERSION].dll"));
        }

        private Func<IEnumerable<string>> _additionalFilesAfterPublishCommon = () => new[]
        {
            "Microsoft.CSharp.dll",
            "Microsoft.VisualBasic.dll",
            // It may seem unusual to include Microsoft.Win32 assemblies in all platforms, but it appears to be by design
            // https://github.com/dotnet/corefx/issues/14896
            "Microsoft.Win32.Primitives.dll",
            "Microsoft.Win32.Registry.dll",
            "mscorlib.dll",
            "netstandard.dll",
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
            "WindowsBase.dll",
        };

        private IDictionary<RuntimeIdentifier, Func<IEnumerable<string>>> _additionalFilesAfterPublish =>
            new Dictionary<RuntimeIdentifier, Func<IEnumerable<string>>>()
            {
                { RuntimeIdentifier.None, () => new[]
                    {
                        $"{Name}.runtimeconfig.json",
                    }
                },
                { RuntimeIdentifier.Win_x64, () =>
                    _additionalFilesAfterPublish[RuntimeIdentifier.None]()
                    .Concat(_additionalFilesAfterPublishCommon())
                    .Concat(new[]
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
                        "Microsoft.DiaSymReader.Native.amd64.dll",
                        "mscordaccore.dll",
                        "mscordaccore_amd64_amd64_[VERSION].dll",
                        "mscordbi.dll",
                        "mscorrc.debug.dll",
                        "mscorrc.dll",
                        "sos.dll",
                        "SOS.NETCore.dll",
                        "sos_amd64_amd64_[VERSION].dll",
                        "ucrtbase.dll",
                    })
                },
                { RuntimeIdentifier.Linux_x64, () =>
                    _additionalFilesAfterPublish[RuntimeIdentifier.None]()
                    .Concat(_additionalFilesAfterPublishCommon())
                    .Concat(new[]
                    {
                        $"{Name}",
                        "createdump",
                        "libclrjit.so",
                        "libcoreclr.so",
                        "libcoreclrtraceptprovider.so",
                        "libdbgshim.so",
                        "libhostfxr.so",
                        "libhostpolicy.so",
                        "libmscordaccore.so",
                        "libmscordbi.so",
                        "libsos.so",
                        "libsosplugin.so",
                        "SOS.NETCore.dll",
                        "sosdocsunix.txt",
                        "System.Globalization.Native.so",
                        "System.IO.Compression.Native.a",
                        "System.IO.Compression.Native.so",
                        "System.Native.a",
                        "System.Native.so",
                        "System.Net.Http.Native.a",
                        "System.Net.Http.Native.so",
                        "System.Net.Security.Native.a",
                        "System.Net.Security.Native.so",
                        "System.Security.Cryptography.Native.OpenSsl.a",
                        "System.Security.Cryptography.Native.OpenSsl.so",
                    })
                },
                { RuntimeIdentifier.OSX_x64, () =>
                    _additionalFilesAfterPublish[RuntimeIdentifier.Linux_x64]()
                    .Where(f => f != "createdump")
                    .Where(f => f != "libcoreclrtraceptprovider.so")
                    .Where(f => f != "libsosplugin.so")
                    .Select(f => Regex.Replace(f, ".so$", ".dylib"))
                    .Concat(new[]
                    {
                        "System.Security.Cryptography.Native.Apple.a",
                        "System.Security.Cryptography.Native.Apple.dylib",
                    })
                }
            };

        public override IEnumerable<string> ExpectedFilesAfterPublish =>
            base.ExpectedFilesAfterPublish
            .Concat(_additionalFilesAfterPublish[RuntimeIdentifier]());
    }
}


