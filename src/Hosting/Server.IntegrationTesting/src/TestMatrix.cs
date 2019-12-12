// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.Server.IntegrationTesting
{
    public class TestMatrix : IEnumerable<object[]>
    {
        public IList<ServerType> Servers { get; set; } = new List<ServerType>();
        public IList<string> Tfms { get; set; } = new List<string>();
        public IList<ApplicationType> ApplicationTypes { get; set; } = new List<ApplicationType>();
        public IList<RuntimeArchitecture> Architectures { get; set; } = new List<RuntimeArchitecture>();

        // ANCM specific...
        public IList<HostingModel> HostingModels { get; set; } = new List<HostingModel>();

        private IList<Tuple<Func<TestVariant, bool>, string>> Skips { get; } = new List<Tuple<Func<TestVariant, bool>, string>>();

        public static TestMatrix ForServers(params ServerType[] types)
        {
            return new TestMatrix()
            {
                Servers = types
            };
        }

        public TestMatrix WithTfms(params string[] tfms)
        {
            Tfms = tfms;
            return this;
        }

        public TestMatrix WithApplicationTypes(params ApplicationType[] types)
        {
            ApplicationTypes = types;
            return this;
        }

        public TestMatrix WithAllApplicationTypes()
        {
            ApplicationTypes.Add(ApplicationType.Portable);
            ApplicationTypes.Add(ApplicationType.Standalone);
            return this;
        }
        public TestMatrix WithArchitectures(params RuntimeArchitecture[] archs)
        {
            Architectures = archs;
            return this;
        }

        public TestMatrix WithAllArchitectures()
        {
            Architectures.Add(RuntimeArchitecture.x64);
            Architectures.Add(RuntimeArchitecture.x86);
            return this;
        }

        public TestMatrix WithHostingModels(params HostingModel[] models)
        {
            HostingModels = models;
            return this;
        }

        public TestMatrix WithAllHostingModels()
        {
            HostingModels.Add(HostingModel.OutOfProcess);
            HostingModels.Add(HostingModel.InProcess);
            return this;
        }

        /// <summary>
        /// V2 + InProc
        /// </summary>
        /// <returns></returns>
        public TestMatrix WithAncmV2InProcess() => WithHostingModels(HostingModel.InProcess);

        public TestMatrix Skip(string message, Func<TestVariant, bool> check)
        {
            Skips.Add(new Tuple<Func<TestVariant, bool>, string>(check, message));
            return this;
        }

        private IEnumerable<TestVariant> Build()
        {
            if (!Servers.Any())
            {
                throw new ArgumentException("No servers were specified.");
            }

            // TFMs.
            if (!Tfms.Any())
            {
                throw new ArgumentException("No TFMs were specified.");
            }

            ResolveDefaultArchitecture();

            if (!ApplicationTypes.Any())
            {
                ApplicationTypes.Add(ApplicationType.Portable);
            }

            if (!HostingModels.Any())
            {
                HostingModels.Add(HostingModel.OutOfProcess);
            }

            var variants = new List<TestVariant>();
            VaryByServer(variants);

            CheckForSkips(variants);

            return variants;
        }

        private void ResolveDefaultArchitecture()
        {
            if (!Architectures.Any())
            {
                switch (RuntimeInformation.OSArchitecture)
                {
                    case Architecture.X86:
                        Architectures.Add(RuntimeArchitecture.x86);
                        break;
                    case Architecture.X64:
                        Architectures.Add(RuntimeArchitecture.x64);
                        break;
                    default:
                        throw new ArgumentException(RuntimeInformation.OSArchitecture.ToString());
                }
            }
        }

        private void VaryByServer(List<TestVariant> variants)
        {
            foreach (var server in Servers)
            {
                var skip = SkipIfServerIsNotSupportedOnThisOS(server);

                VaryByTfm(variants, server, skip);
            }
        }

        private static string SkipIfServerIsNotSupportedOnThisOS(ServerType server)
        {
            var skip = false;
            switch (server)
            {
                case ServerType.IIS:
                case ServerType.IISExpress:
                case ServerType.HttpSys:
                    skip = !RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
                    break;
                case ServerType.Kestrel:
                    break;
                case ServerType.Nginx:
                    // Technically it's possible but we don't test it.
                    skip = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
                    break;
                default:
                    throw new ArgumentException(server.ToString());
            }

            return skip ? "This server is not supported on this operating system." : null;
        }

        private void VaryByTfm(List<TestVariant> variants, ServerType server, string skip)
        {
            foreach (var tfm in Tfms)
            {
                if (!CheckTfmIsSupportedForServer(tfm, server))
                {
                    // Don't generate net461 variations for nginx server.
                    continue;
                }

                var skipTfm = skip ?? SkipIfTfmIsNotSupportedOnThisOS(tfm);

                VaryByApplicationType(variants, server, tfm, skipTfm);
            }
        }

        private bool CheckTfmIsSupportedForServer(string tfm, ServerType server)
        {
            // Not a combination we test
            return !(Tfm.Matches(Tfm.Net461, tfm) && ServerType.Nginx == server);
        }

        private static string SkipIfTfmIsNotSupportedOnThisOS(string tfm)
        {
            if (Tfm.Matches(Tfm.Net461, tfm) && !RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return "This TFM is not supported on this operating system.";
            }

            return null;
        }

        private void VaryByApplicationType(List<TestVariant> variants, ServerType server, string tfm, string skip)
        {
            foreach (var t in ApplicationTypes)
            {
                var type = t;
                if (Tfm.Matches(Tfm.Net461, tfm) && type == ApplicationType.Portable)
                {
                    if (ApplicationTypes.Count == 1)
                    {
                        // Override the default
                        type = ApplicationType.Standalone;
                    }
                    else
                    {
                        continue;
                    }
                }

                VaryByArchitecture(variants, server, tfm, skip, type);
            }
        }

        private void VaryByArchitecture(List<TestVariant> variants, ServerType server, string tfm, string skip, ApplicationType type)
        {
            foreach (var arch in Architectures)
            {
                if (!IsArchitectureSupportedOnServer(arch, server))
                {
                    continue;
                }
                var archSkip = skip ?? SkipIfArchitectureNotSupportedOnCurrentSystem(arch);

                if (server == ServerType.IISExpress || server == ServerType.IIS)
                {
                    VaryByAncmHostingModel(variants, server, tfm, type, arch, archSkip);
                }
                else
                {
                    variants.Add(new TestVariant()
                    {
                        Server = server,
                        Tfm = tfm,
                        ApplicationType = type,
                        Architecture = arch,
                        Skip = archSkip,
                    });
                }
            }
        }

        private string SkipIfArchitectureNotSupportedOnCurrentSystem(RuntimeArchitecture arch)
        {
            if (arch == RuntimeArchitecture.x64)
            {
                // Can't run x64 on a x86 OS.
                return (RuntimeInformation.OSArchitecture == Architecture.Arm || RuntimeInformation.OSArchitecture == Architecture.X86)
                    ? $"Cannot run {arch} on your current system." : null;
            }

            // No x86 runtimes available on MacOS or Linux.
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? null : $"No {arch} available for non-Windows systems.";
        }

        private bool IsArchitectureSupportedOnServer(RuntimeArchitecture arch, ServerType server)
        {
            // No x86 Mac/Linux runtime, don't generate a test variation that will always be skipped.
            return !(arch == RuntimeArchitecture.x86 && ServerType.Nginx == server);
        }

        private void VaryByAncmHostingModel(IList<TestVariant> variants, ServerType server, string tfm, ApplicationType type, RuntimeArchitecture arch, string skip)
        {
            foreach (var hostingModel in HostingModels)
            {
                var skipAncm = skip;
                if (hostingModel == HostingModel.InProcess)
                {
                    // Not supported
                    if (Tfm.Matches(Tfm.Net461, tfm) || Tfm.Matches(Tfm.NetCoreApp20, tfm))
                    {
                        continue;
                    }

                    if (!IISExpressAncmSchema.SupportsInProcessHosting)
                    {
                        skipAncm = skipAncm ?? IISExpressAncmSchema.SkipReason;
                    }
                }

                variants.Add(new TestVariant()
                {
                    Server = server,
                    Tfm = tfm,
                    ApplicationType = type,
                    Architecture = arch,
                    HostingModel = hostingModel,
                    Skip = skipAncm,
                });
            }
        }

        private void CheckForSkips(List<TestVariant> variants)
        {
            foreach (var variant in variants)
            {
                foreach (var skipPair in Skips)
                {
                    if (skipPair.Item1(variant))
                    {
                        variant.Skip = skipPair.Item2;
                        break;
                    }
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<object[]>)this).GetEnumerator();
        }

        // This is what Xunit MemberData expects
        public IEnumerator<object[]> GetEnumerator()
        {
            foreach (var v in Build())
            {
                yield return new[] { v };
            }
        }
    }
}
