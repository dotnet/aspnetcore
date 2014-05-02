// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

// -----------------------------------------------------------------------
// <copyright file="SSPIAuthType.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.AspNet.Security.Windows
{
    internal class SSPIAuthType : ISSPIInterface
    {
        private static volatile SecurityPackageInfoClass[] _securityPackages;

        public SecurityPackageInfoClass[] SecurityPackages
        {
            get
            {
                return _securityPackages;
            }
            set
            {
                _securityPackages = value;
            }
        }

        public int EnumerateSecurityPackages(out int pkgnum, out SafeFreeContextBuffer pkgArray)
        {
            GlobalLog.Print("SSPIAuthType::EnumerateSecurityPackages()");
            return SafeFreeContextBuffer.EnumeratePackages(out pkgnum, out pkgArray);
        }

        public int AcquireCredentialsHandle(string moduleName, CredentialUse usage, ref AuthIdentity authdata, out SafeFreeCredentials outCredential)
        {
            return SafeFreeCredentials.AcquireCredentialsHandle(moduleName, usage, ref authdata, out outCredential);
        }

        public int AcquireCredentialsHandle(string moduleName, CredentialUse usage, ref SafeSspiAuthDataHandle authdata, out SafeFreeCredentials outCredential)
        {
            return SafeFreeCredentials.AcquireCredentialsHandle(moduleName, usage, ref authdata, out outCredential);
        }

        public int AcquireDefaultCredential(string moduleName, CredentialUse usage, out SafeFreeCredentials outCredential)
        {
            return SafeFreeCredentials.AcquireDefaultCredential(moduleName, usage, out outCredential);
        }

        public int AcquireCredentialsHandle(string moduleName, CredentialUse usage, ref SecureCredential authdata, out SafeFreeCredentials outCredential)
        {
            return SafeFreeCredentials.AcquireCredentialsHandle(moduleName, usage, ref authdata, out outCredential);
        }

        public int AcceptSecurityContext(ref SafeFreeCredentials credential, ref SafeDeleteContext context, SecurityBuffer inputBuffer, ContextFlags inFlags, Endianness endianness, SecurityBuffer outputBuffer, ref ContextFlags outFlags)
        {
            return SafeDeleteContext.AcceptSecurityContext(ref credential, ref context, inFlags, endianness, inputBuffer, null, outputBuffer, ref outFlags);
        }

        public int AcceptSecurityContext(SafeFreeCredentials credential, ref SafeDeleteContext context, SecurityBuffer[] inputBuffers, ContextFlags inFlags, Endianness endianness, SecurityBuffer outputBuffer, ref ContextFlags outFlags)
        {
            return SafeDeleteContext.AcceptSecurityContext(ref credential, ref context, inFlags, endianness, null, inputBuffers, outputBuffer, ref outFlags);
        }

        public int InitializeSecurityContext(ref SafeFreeCredentials credential, ref SafeDeleteContext context, string targetName, ContextFlags inFlags, Endianness endianness, SecurityBuffer inputBuffer, SecurityBuffer outputBuffer, ref ContextFlags outFlags)
        {
            return SafeDeleteContext.InitializeSecurityContext(ref credential, ref context, targetName, inFlags, endianness, inputBuffer, null, outputBuffer, ref outFlags);
        }

        public int InitializeSecurityContext(SafeFreeCredentials credential, ref SafeDeleteContext context, string targetName, ContextFlags inFlags, Endianness endianness, SecurityBuffer[] inputBuffers, SecurityBuffer outputBuffer, ref ContextFlags outFlags)
        {
            return SafeDeleteContext.InitializeSecurityContext(ref credential, ref context, targetName, inFlags, endianness, null, inputBuffers, outputBuffer, ref outFlags);
        }

        public int EncryptMessage(SafeDeleteContext context, SecurityBufferDescriptor inputOutput, uint sequenceNumber)
        {
            int status = (int)SecurityStatus.InvalidHandle;
            bool b = false;

            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                context.DangerousAddRef(ref b);
            }
            catch (Exception e)
            {
                if (b)
                {
                    context.DangerousRelease();
                    b = false;
                }
                if (!(e is ObjectDisposedException))
                {
                    throw;
                }
            }
            finally
            {
                if (b)
                {
                    status = UnsafeNclNativeMethods.NativeNTSSPI.EncryptMessage(ref context._handle, 0, inputOutput, sequenceNumber);
                    context.DangerousRelease();
                }
            }
            return status;
        }

        public unsafe int DecryptMessage(SafeDeleteContext context, SecurityBufferDescriptor inputOutput, uint sequenceNumber)
        {
            int status = (int)SecurityStatus.InvalidHandle;
            bool b = false;
            uint qop = 0;

            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                context.DangerousAddRef(ref b);
            }
            catch (Exception e)
            {
                if (b)
                {
                    context.DangerousRelease();
                    b = false;
                }
                if (!(e is ObjectDisposedException))
                {
                    throw;
                }
            }
            finally
            {
                if (b)
                {
                    status = UnsafeNclNativeMethods.NativeNTSSPI.DecryptMessage(ref context._handle, inputOutput, sequenceNumber, &qop);
                    context.DangerousRelease();
                }
            }

            const uint SECQOP_WRAP_NO_ENCRYPT = 0x80000001;
            if (status == 0 && qop == SECQOP_WRAP_NO_ENCRYPT)
            {
                GlobalLog.Assert("NativeNTSSPI.DecryptMessage", "Expected qop = 0, returned value = " + qop.ToString("x", CultureInfo.InvariantCulture));
                throw new InvalidOperationException(SR.GetString(SR.net_auth_message_not_encrypted));
            }

            return status;
        }

        public int MakeSignature(SafeDeleteContext context, SecurityBufferDescriptor inputOutput, uint sequenceNumber)
        {
            int status = (int)SecurityStatus.InvalidHandle;
            bool b = false;

            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                context.DangerousAddRef(ref b);
            }
            catch (Exception e)
            {
                if (b)
                {
                    context.DangerousRelease();
                    b = false;
                }
                if (!(e is ObjectDisposedException))
                {
                    throw;
                }
            }
            finally
            {
                if (b)
                {
                    const uint SECQOP_WRAP_NO_ENCRYPT = 0x80000001;
                    status = UnsafeNclNativeMethods.NativeNTSSPI.EncryptMessage(ref context._handle, SECQOP_WRAP_NO_ENCRYPT, inputOutput, sequenceNumber);
                    context.DangerousRelease();
                }
            }
            return status;
        }

        public unsafe int VerifySignature(SafeDeleteContext context, SecurityBufferDescriptor inputOutput, uint sequenceNumber)
        {
            int status = (int)SecurityStatus.InvalidHandle;
            bool b = false;

            uint qop = 0;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                context.DangerousAddRef(ref b);
            }
            catch (Exception e)
            {
                if (b)
                {
                    context.DangerousRelease();
                    b = false;
                }
                if (!(e is ObjectDisposedException))
                {
                    throw;
                }
            }
            finally
            {
                if (b)
                {
                    status = UnsafeNclNativeMethods.NativeNTSSPI.DecryptMessage(ref context._handle, inputOutput, sequenceNumber, &qop);
                    context.DangerousRelease();
                }
            }

            return status;
        }

        public int QueryContextChannelBinding(SafeDeleteContext context, ContextAttribute attribute, out SafeFreeContextBufferChannelBinding binding)
        {
            // Querying an auth SSP for a CBT doesn't make sense
            binding = null;
            throw new NotSupportedException();
        }

        public unsafe int QueryContextAttributes(SafeDeleteContext context, ContextAttribute attribute, byte[] buffer, Type handleType, out SafeHandle refHandle)
        {
            refHandle = null;
            if (handleType != null)
            {
                if (handleType == typeof(SafeFreeContextBuffer))
                {
                    refHandle = SafeFreeContextBuffer.CreateEmptyHandle();
                }
                else if (handleType == typeof(SafeFreeCertContext))
                {
                    refHandle = new SafeFreeCertContext();
                }
                else
                {
                    throw new ArgumentException(SR.GetString(SR.SSPIInvalidHandleType, handleType.FullName), "handleType");
                }
            }

            fixed (byte* bufferPtr = buffer)
            {
                return SafeFreeContextBuffer.QueryContextAttributes(context, attribute, bufferPtr, refHandle);
            }
        }

        public int SetContextAttributes(SafeDeleteContext context, ContextAttribute attribute, byte[] buffer)
        {
            throw new NotImplementedException();
        }

        public int QuerySecurityContextToken(SafeDeleteContext phContext, out SafeCloseHandle phToken)
        {
            return GetSecurityContextToken(phContext, out phToken);
        }

        public int CompleteAuthToken(ref SafeDeleteContext refContext, SecurityBuffer[] inputBuffers)
        {
            return SafeDeleteContext.CompleteAuthToken(ref refContext, inputBuffers);
        }

        private static int GetSecurityContextToken(SafeDeleteContext phContext, out SafeCloseHandle safeHandle)
        {
            int status = (int)SecurityStatus.InvalidHandle;
            bool b = false;
            safeHandle = null;

            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                phContext.DangerousAddRef(ref b);
            }
            catch (Exception e)
            {
                if (b)
                {
                    phContext.DangerousRelease();
                    b = false;
                }
                if (!(e is ObjectDisposedException))
                {
                    throw;
                }
            }
            finally
            {
                if (b)
                {
                    status = UnsafeNclNativeMethods.SafeNetHandles.QuerySecurityContextToken(ref phContext._handle, out safeHandle);
                    phContext.DangerousRelease();
                }
            }

            return status;
        }
    }
}
