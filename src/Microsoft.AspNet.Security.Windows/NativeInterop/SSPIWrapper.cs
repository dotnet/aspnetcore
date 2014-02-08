//------------------------------------------------------------------------------
// <copyright file="_SSPIWrapper.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Microsoft.AspNet.Security.Windows
{        
    // From Schannel.h

    [StructLayout(LayoutKind.Sequential)]
    internal struct NegotiationInfo
    {
        // see SecPkgContext_NegotiationInfoW in <sspi.h>

        // [MarshalAs(UnmanagedType.LPStruct)] internal SecurityPackageInfo PackageInfo;
        internal IntPtr PackageInfo;
        internal uint NegotiationState;
        internal static readonly int Size = Marshal.SizeOf(typeof(NegotiationInfo));
        internal static readonly int NegotiationStateOffest = (int)Marshal.OffsetOf(typeof(NegotiationInfo), "NegotiationState");
    }

    // we keep it simple since we use this only to know if NTLM or
    // Kerberos are used in the context of a Negotiate handshake

    [StructLayout(LayoutKind.Sequential)]
    internal struct SecurityPackageInfo
    {
        // see SecPkgInfoW in <sspi.h>
        internal int Capabilities;
        internal short Version;
        internal short RPCID;
        internal int MaxToken;
        internal IntPtr Name;
        internal IntPtr Comment;

        internal static readonly int Size = Marshal.SizeOf(typeof(SecurityPackageInfo));
        internal static readonly int NameOffest = (int)Marshal.OffsetOf(typeof(SecurityPackageInfo), "Name");
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Bindings
    {
        // see SecPkgContext_Bindings in <sspi.h>
        internal int BindingsLength;
        internal IntPtr pBindings;
    }

    internal static class SSPIWrapper
    {
        internal static SecurityPackageInfoClass[] EnumerateSecurityPackages(ISSPIInterface secModule)
        {
            GlobalLog.Enter("EnumerateSecurityPackages");
            if (secModule.SecurityPackages == null)
            {
                lock (secModule)
                {
                    if (secModule.SecurityPackages == null)
                    {
                        int moduleCount = 0;
                        SafeFreeContextBuffer arrayBaseHandle = null;
                        try
                        {
                            int errorCode = secModule.EnumerateSecurityPackages(out moduleCount, out arrayBaseHandle);
                            GlobalLog.Print("SSPIWrapper::arrayBase: " + (arrayBaseHandle.DangerousGetHandle().ToString("x")));
                            if (errorCode != 0)
                            {
                                throw new Win32Exception(errorCode);
                            }
                            SecurityPackageInfoClass[] securityPackages = new SecurityPackageInfoClass[moduleCount];
                            if (Logging.On)
                            {
                                Logging.PrintInfo(Logging.Web, SR.GetString(SR.net_log_sspi_enumerating_security_packages));
                            }
                            int i;
                            for (i = 0; i < moduleCount; i++)
                            {
                                securityPackages[i] = new SecurityPackageInfoClass(arrayBaseHandle, i);
                                if (Logging.On)
                                {
                                    Logging.PrintInfo(Logging.Web, "    " + securityPackages[i].Name);
                                }
                            }
                            secModule.SecurityPackages = securityPackages;
                        }
                        finally
                        {
                            if (arrayBaseHandle != null)
                            {
                                arrayBaseHandle.Dispose();
                            }
                        }
                    }
                }
            }
            GlobalLog.Leave("EnumerateSecurityPackages");
            return secModule.SecurityPackages;
        }
        
        internal static SecurityPackageInfoClass GetVerifyPackageInfo(ISSPIInterface secModule, string packageName, bool throwIfMissing)
        {
            SecurityPackageInfoClass[] supportedSecurityPackages = EnumerateSecurityPackages(secModule);
            if (supportedSecurityPackages != null)
            {
                for (int i = 0; i < supportedSecurityPackages.Length; i++)
                {
                    if (string.Compare(supportedSecurityPackages[i].Name, packageName, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        return supportedSecurityPackages[i];
                    }
                }
            }

            if (Logging.On)
            {
                Logging.PrintInfo(Logging.Web, SR.GetString(SR.net_log_sspi_security_package_not_found, packageName));
            }

            // error
            if (throwIfMissing)
            {
                throw new NotSupportedException(SR.GetString(SR.net_securitypackagesupport));
            }

            return null;
        }

        public static SafeFreeCredentials AcquireDefaultCredential(ISSPIInterface secModule, string package, CredentialUse intent)
        {
            GlobalLog.Print("SSPIWrapper::AcquireDefaultCredential(): using " + package);
            if (Logging.On)
            {
                Logging.PrintInfo(Logging.Web,
               "AcquireDefaultCredential(" +
               "package = " + package + ", " +
               "intent  = " + intent + ")");
            }

            SafeFreeCredentials outCredential = null;
            int errorCode = secModule.AcquireDefaultCredential(package, intent, out outCredential);

            if (errorCode != 0)
            {
#if TRAVE
                GlobalLog.Print("SSPIWrapper::AcquireDefaultCredential(): error " + SecureChannel.MapSecurityStatus((uint)errorCode));
#endif
                if (Logging.On)
                {
                    Logging.PrintError(Logging.Web, SR.GetString(SR.net_log_operation_failed_with_error, "AcquireDefaultCredential()", String.Format(CultureInfo.CurrentCulture, "0X{0:X}", errorCode)));
                }
                throw new Win32Exception(errorCode);
            }
            return outCredential;
        }

        public static SafeFreeCredentials AcquireCredentialsHandle(ISSPIInterface secModule, string package, CredentialUse intent, ref AuthIdentity authdata)
        {
            GlobalLog.Print("SSPIWrapper::AcquireCredentialsHandle#2(): using " + package);

            if (Logging.On)
            {
                Logging.PrintInfo(Logging.Web,
               "AcquireCredentialsHandle(" +
               "package  = " + package + ", " +
               "intent   = " + intent + ", " +
               "authdata = " + authdata + ")");
            }

            SafeFreeCredentials credentialsHandle = null;
            int errorCode = secModule.AcquireCredentialsHandle(package,
                                                               intent,
                                                               ref authdata,
                                                               out credentialsHandle);

            if (errorCode != 0)
            {
                if (Logging.On)
                {
                    Logging.PrintError(Logging.Web, SR.GetString(SR.net_log_operation_failed_with_error, "AcquireCredentialsHandle()", String.Format(CultureInfo.CurrentCulture, "0X{0:X}", errorCode)));
                }
                throw new Win32Exception(errorCode);
            }
            return credentialsHandle;
        }

        public static SafeFreeCredentials AcquireCredentialsHandle(ISSPIInterface secModule, string package, CredentialUse intent, ref SafeSspiAuthDataHandle authdata)
        {
            if (Logging.On)
            {
                Logging.PrintInfo(Logging.Web,
               "AcquireCredentialsHandle(" +
               "package  = " + package + ", " +
               "intent   = " + intent + ", " +
               "authdata = " + authdata + ")");
            }

            SafeFreeCredentials credentialsHandle = null;
            int errorCode = secModule.AcquireCredentialsHandle(package, intent, ref authdata, out credentialsHandle);

            if (errorCode != 0)
            {
                if (Logging.On)
                {
                    Logging.PrintError(Logging.Web, SR.GetString(SR.net_log_operation_failed_with_error, "AcquireCredentialsHandle()", String.Format(CultureInfo.CurrentCulture, "0X{0:X}", errorCode)));
                }
                throw new Win32Exception(errorCode);
            }
            return credentialsHandle;
        }
        
        internal static int InitializeSecurityContext(ISSPIInterface secModule, SafeFreeCredentials credential, ref SafeDeleteContext context, string targetName, ContextFlags inFlags, Endianness datarep, SecurityBuffer[] inputBuffers, SecurityBuffer outputBuffer, ref ContextFlags outFlags)
        {
            if (Logging.On)
            {
                Logging.PrintInfo(Logging.Web,
               "InitializeSecurityContext(" +
               "credential = " + credential.ToString() + ", " +
               "context = " + ValidationHelper.ToString(context) + ", " +
               "targetName = " + targetName + ", " +
               "inFlags = " + inFlags + ")");
            }

            int errorCode = secModule.InitializeSecurityContext(credential, ref context, targetName, inFlags, datarep, inputBuffers, outputBuffer, ref outFlags);

            if (Logging.On)
            {
                Logging.PrintInfo(Logging.Web, SR.GetString(SR.net_log_sspi_security_context_input_buffers, "InitializeSecurityContext", (inputBuffers == null ? 0 : inputBuffers.Length), outputBuffer.size, (SecurityStatus)errorCode));
            }

            return errorCode;
        }

        internal static int AcceptSecurityContext(ISSPIInterface secModule, SafeFreeCredentials credential, ref SafeDeleteContext context, ContextFlags inFlags, Endianness datarep, SecurityBuffer[] inputBuffers, SecurityBuffer outputBuffer, ref ContextFlags outFlags)
        {
            if (Logging.On)
            {
                Logging.PrintInfo(Logging.Web,
               "AcceptSecurityContext(" +
               "credential = " + credential.ToString() + ", " +
               "context = " + ValidationHelper.ToString(context) + ", " +
               "inFlags = " + inFlags + ")");
            }

            int errorCode = secModule.AcceptSecurityContext(credential, ref context, inputBuffers, inFlags, datarep, outputBuffer, ref outFlags);

            if (Logging.On)
            {
                Logging.PrintInfo(Logging.Web, SR.GetString(SR.net_log_sspi_security_context_input_buffers, "AcceptSecurityContext", (inputBuffers == null ? 0 : inputBuffers.Length), outputBuffer.size, (SecurityStatus)errorCode));
            }

            return errorCode;
        }

        internal static int CompleteAuthToken(ISSPIInterface secModule, ref SafeDeleteContext context, SecurityBuffer[] inputBuffers)
        {
            int errorCode = secModule.CompleteAuthToken(ref context, inputBuffers);

            if (Logging.On)
            {
                Logging.PrintInfo(Logging.Web, SR.GetString(SR.net_log_operation_returned_something, "CompleteAuthToken()", (SecurityStatus)errorCode));
            }

            return errorCode;
        }

        public static int QuerySecurityContextToken(ISSPIInterface secModule, SafeDeleteContext context, out SafeCloseHandle token)
        {
            return secModule.QuerySecurityContextToken(context, out token);
        }

        public static SafeFreeContextBufferChannelBinding QueryContextChannelBinding(ISSPIInterface secModule, SafeDeleteContext securityContext, ContextAttribute contextAttribute)
        {
            GlobalLog.Enter("QueryContextChannelBinding", contextAttribute.ToString());

            SafeFreeContextBufferChannelBinding result;
            int errorCode = secModule.QueryContextChannelBinding(securityContext, contextAttribute, out result);
            if (errorCode != 0)
            {
                GlobalLog.Leave("QueryContextChannelBinding", "ERROR = " + ErrorDescription(errorCode));
                return null;
            }

            GlobalLog.Leave("QueryContextChannelBinding", ValidationHelper.HashString(result));
            return result;
        }

        public static object QueryContextAttributes(ISSPIInterface secModule, SafeDeleteContext securityContext, ContextAttribute contextAttribute)
        {
            int errorCode;
            return QueryContextAttributes(secModule, securityContext, contextAttribute, out errorCode);
        }

        public static object QueryContextAttributes(ISSPIInterface secModule, SafeDeleteContext securityContext, ContextAttribute contextAttribute, out int errorCode)
        {
            GlobalLog.Enter("QueryContextAttributes", contextAttribute.ToString());

            int nativeBlockSize = IntPtr.Size;
            Type handleType = null;

            switch (contextAttribute)
            {
                case ContextAttribute.Sizes:
                    nativeBlockSize = SecSizes.SizeOf;
                    break;
                case ContextAttribute.StreamSizes:
                    nativeBlockSize = StreamSizes.SizeOf;
                    break;

                case ContextAttribute.Names:
                    handleType = typeof(SafeFreeContextBuffer);
                    break;

                case ContextAttribute.PackageInfo:
                    handleType = typeof(SafeFreeContextBuffer);
                    break;

                case ContextAttribute.NegotiationInfo:
                    handleType = typeof(SafeFreeContextBuffer);
                    nativeBlockSize = Marshal.SizeOf(typeof(NegotiationInfo));
                    break;

                case ContextAttribute.ClientSpecifiedSpn:
                    handleType = typeof(SafeFreeContextBuffer);
                    break;

                case ContextAttribute.RemoteCertificate:
                    handleType = typeof(SafeFreeCertContext);
                    break;

                case ContextAttribute.LocalCertificate:
                    handleType = typeof(SafeFreeCertContext);
                    break;

                case ContextAttribute.IssuerListInfoEx:
                    nativeBlockSize = Marshal.SizeOf(typeof(IssuerListInfoEx));
                    handleType = typeof(SafeFreeContextBuffer);
                    break;

                case ContextAttribute.ConnectionInfo:
                    nativeBlockSize = Marshal.SizeOf(typeof(SslConnectionInfo));
                    break;

                default:
                    throw new ArgumentException(SR.GetString(SR.net_invalid_enum, "ContextAttribute"), "contextAttribute");
            }

            SafeHandle SspiHandle = null;
            object attribute = null;

            try
            {
                byte[] nativeBuffer = new byte[nativeBlockSize];
                errorCode = secModule.QueryContextAttributes(securityContext, contextAttribute, nativeBuffer, handleType, out SspiHandle);
                if (errorCode != 0)
                {
                    GlobalLog.Leave("Win32:QueryContextAttributes", "ERROR = " + ErrorDescription(errorCode));
                    return null;
                }

                switch (contextAttribute)
                {
                    case ContextAttribute.Sizes:
                        attribute = new SecSizes(nativeBuffer);
                        break;

                    case ContextAttribute.StreamSizes:
                        attribute = new StreamSizes(nativeBuffer);
                        break;

                    case ContextAttribute.Names:
                        attribute = Marshal.PtrToStringUni(SspiHandle.DangerousGetHandle());
                        break;

                    case ContextAttribute.PackageInfo:
                        attribute = new SecurityPackageInfoClass(SspiHandle, 0);
                        break;

                    case ContextAttribute.NegotiationInfo:
                        unsafe
                        {
                            fixed (void* ptr = nativeBuffer)
                            {
                                attribute = new NegotiationInfoClass(SspiHandle, Marshal.ReadInt32(new IntPtr(ptr), NegotiationInfo.NegotiationStateOffest));
                            }
                        }
                        break;

                    case ContextAttribute.ClientSpecifiedSpn:
                        attribute = Marshal.PtrToStringUni(SspiHandle.DangerousGetHandle());
                        break;

                    case ContextAttribute.LocalCertificate:
                        goto case ContextAttribute.RemoteCertificate;
                    case ContextAttribute.RemoteCertificate:
                        attribute = SspiHandle;
                        SspiHandle = null;
                        break;

                    case ContextAttribute.IssuerListInfoEx:
                        attribute = new IssuerListInfoEx(SspiHandle, nativeBuffer);
                        SspiHandle = null;
                        break;

                    case ContextAttribute.ConnectionInfo:
                        attribute = new SslConnectionInfo(nativeBuffer);
                        break;
                    default:
                        // will return null
                        break;
                }
            }
            finally
            {
                if (SspiHandle != null)
                {
                    SspiHandle.Dispose();
                }
            }
            GlobalLog.Leave("QueryContextAttributes", ValidationHelper.ToString(attribute));
            return attribute;
        }

        public static string ErrorDescription(int errorCode)
        {
            if (errorCode == -1)
            {
                return "An exception when invoking Win32 API";
            }
            switch ((SecurityStatus)errorCode)
            {
                case SecurityStatus.InvalidHandle:
                    return "Invalid handle";
                case SecurityStatus.InvalidToken:
                    return "Invalid token";
                case SecurityStatus.ContinueNeeded:
                    return "Continue needed";
                case SecurityStatus.IncompleteMessage:
                    return "Message incomplete";
                case SecurityStatus.WrongPrincipal:
                    return "Wrong principal";
                case SecurityStatus.TargetUnknown:
                    return "Target unknown";
                case SecurityStatus.PackageNotFound:
                    return "Package not found";
                case SecurityStatus.BufferNotEnough:
                    return "Buffer not enough";
                case SecurityStatus.MessageAltered:
                    return "Message altered";
                case SecurityStatus.UntrustedRoot:
                    return "Untrusted root";
                default:
                    return "0x" + errorCode.ToString("x", NumberFormatInfo.InvariantInfo);
            }
        }
    } // class SSPIWrapper
}
