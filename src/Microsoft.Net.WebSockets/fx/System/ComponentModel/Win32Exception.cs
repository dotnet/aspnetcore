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

//------------------------------------------------------------------------------
// <copyright file="HttpListenerException.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

#if DOTNET5_4

using System.Runtime.InteropServices;
using System.Text;

namespace System.ComponentModel
{
    internal class Win32Exception : ExternalException
    {
        /// <devdoc>
        ///    <para>Represents the Win32 error code associated with this exception. This 
        ///       field is read-only.</para>
        /// </devdoc>
        private readonly int nativeErrorCode;

        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='System.ComponentModel.Win32Exception'/> class with the last Win32 error 
        ///    that occured.</para>
        /// </devdoc>
        public Win32Exception()
            : this(Marshal.GetLastWin32Error())
        {
        }
        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='System.ComponentModel.Win32Exception'/> class with the specified error.</para>
        /// </devdoc>
        public Win32Exception(int error)
            : this(error, GetErrorMessage(error))
        {
        }
        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='System.ComponentModel.Win32Exception'/> class with the specified error and the 
        ///    specified detailed description.</para>
        /// </devdoc>
        public Win32Exception(int error, string message)
            : base(message)
        {
            nativeErrorCode = error;
        }

        /// <devdoc>
        ///     Initializes a new instance of the Exception class with a specified error message.
        ///     FxCop CA1032: Multiple constructors are required to correctly implement a custom exception.
        /// </devdoc>
        public Win32Exception(string message)
            : this(Marshal.GetLastWin32Error(), message)
        {
        }

        /// <devdoc>
        ///     Initializes a new instance of the Exception class with a specified error message and a 
        ///     reference to the inner exception that is the cause of this exception.
        ///     FxCop CA1032: Multiple constructors are required to correctly implement a custom exception.
        /// </devdoc>
        public Win32Exception(string message, Exception innerException)
            : base(message, innerException)
        {
            nativeErrorCode = Marshal.GetLastWin32Error();
        }


        /// <devdoc>
        ///    <para>Represents the Win32 error code associated with this exception. This 
        ///       field is read-only.</para>
        /// </devdoc>
        public int NativeErrorCode
        {
            get
            {
                return nativeErrorCode;
            }
        }

        private static string GetErrorMessage(int error)
        {
            //get the system error message...
            string errorMsg = "";
            StringBuilder sb = new StringBuilder(256);
            int result = SafeNativeMethods.FormatMessage(
                                        SafeNativeMethods.FORMAT_MESSAGE_IGNORE_INSERTS |
                                        SafeNativeMethods.FORMAT_MESSAGE_FROM_SYSTEM |
                                        SafeNativeMethods.FORMAT_MESSAGE_ARGUMENT_ARRAY,
                                        IntPtr.Zero, (uint)error, 0, sb, sb.Capacity + 1,
                                        null);
            if (result != 0)
            {
                int i = sb.Length;
                while (i > 0)
                {
                    char ch = sb[i - 1];
                    if (ch > 32 && ch != '.') break;
                    i--;
                }
                errorMsg = sb.ToString(0, i);
            }
            else
            {
                errorMsg = "Unknown error (0x" + Convert.ToString(error, 16) + ")";
            }

            return errorMsg;
        }
    }
}

#endif
