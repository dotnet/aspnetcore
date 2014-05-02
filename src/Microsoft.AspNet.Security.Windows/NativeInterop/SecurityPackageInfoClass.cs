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
// <copyright file="SecurityPackageInfoClass.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Microsoft.AspNet.Security.Windows
{
    internal class SecurityPackageInfoClass
    {
        private int _capabilities = 0;
        private short _version = 0;
        private short _rpcid = 0;
        private int _maxToken = 0;
        private string _name = null;
        private string _comment = null;

        /*
         *  This is to support SSL under semi trusted environment.
         *  Note that it is only for SSL with no client cert
         */
        internal SecurityPackageInfoClass(SafeHandle safeHandle, int index)
        {
            if (safeHandle.IsInvalid)
            {
                GlobalLog.Print("SecurityPackageInfoClass::.ctor() the pointer is invalid: " + (safeHandle.DangerousGetHandle()).ToString("x"));
                return;
            }
            IntPtr unmanagedAddress = IntPtrHelper.Add(safeHandle.DangerousGetHandle(), SecurityPackageInfo.Size * index);
            GlobalLog.Print("SecurityPackageInfoClass::.ctor() unmanagedPointer: " + ((long)unmanagedAddress).ToString("x"));

            _capabilities = Marshal.ReadInt32(unmanagedAddress, (int)Marshal.OffsetOf(typeof(SecurityPackageInfo), "Capabilities"));
            _version = Marshal.ReadInt16(unmanagedAddress, (int)Marshal.OffsetOf(typeof(SecurityPackageInfo), "Version"));
            _rpcid = Marshal.ReadInt16(unmanagedAddress, (int)Marshal.OffsetOf(typeof(SecurityPackageInfo), "RPCID"));
            _maxToken = Marshal.ReadInt32(unmanagedAddress, (int)Marshal.OffsetOf(typeof(SecurityPackageInfo), "MaxToken"));

            IntPtr unmanagedString;

            unmanagedString = Marshal.ReadIntPtr(unmanagedAddress, (int)Marshal.OffsetOf(typeof(SecurityPackageInfo), "Name"));
            if (unmanagedString != IntPtr.Zero)
            {
                _name = Marshal.PtrToStringUni(unmanagedString);
                GlobalLog.Print("Name: " + Name);
            }

            unmanagedString = Marshal.ReadIntPtr(unmanagedAddress, (int)Marshal.OffsetOf(typeof(SecurityPackageInfo), "Comment"));
            if (unmanagedString != IntPtr.Zero)
            {
                _comment = Marshal.PtrToStringUni(unmanagedString);
                GlobalLog.Print("Comment: " + _comment);
            }

            GlobalLog.Print("SecurityPackageInfoClass::.ctor(): " + ToString());
        }

        internal int MaxToken
        {
            get { return _maxToken; }
        }

        internal string Name
        {
            get { return _name; }
        }

        public override string ToString()
        {
            return "Capabilities:" + String.Format(CultureInfo.InvariantCulture, "0x{0:x}", _capabilities)
                + " Version:" + _version.ToString(NumberFormatInfo.InvariantInfo)
                + " RPCID:" + _rpcid.ToString(NumberFormatInfo.InvariantInfo)
                + " MaxToken:" + MaxToken.ToString(NumberFormatInfo.InvariantInfo)
                + " Name:" + ((Name == null) ? "(null)" : Name)
                + " Comment:" + ((_comment == null) ? "(null)" : _comment);
        }
    }
}
