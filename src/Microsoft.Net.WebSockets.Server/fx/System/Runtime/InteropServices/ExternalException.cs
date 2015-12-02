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

// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/*=============================================================================
**
** Class: ExternalException
**
**
** Purpose: Exception base class for all errors from Interop or Structured 
**          Exception Handling code.
**
**
=============================================================================*/

#if DOTNET5_4

namespace System.Runtime.InteropServices
{
    using System;
    using System.Globalization;

    // Base exception for COM Interop errors &; Structured Exception Handler
    // exceptions.
    // 
    internal class ExternalException : Exception
    {
        public ExternalException()
        {
            SetErrorCode(__HResults.E_FAIL);
        }

        public ExternalException(String message)
            : base(message)
        {
            SetErrorCode(__HResults.E_FAIL);
        }

        public ExternalException(String message, Exception inner)
            : base(message, inner)
        {
            SetErrorCode(__HResults.E_FAIL);
        }

        public ExternalException(String message, int errorCode)
            : base(message)
        {
            SetErrorCode(errorCode);
        }

        private void SetErrorCode(int errorCode)
        {
            HResult = ErrorCode;
        }

        private static class __HResults
        {
            internal const int E_FAIL = unchecked((int)0x80004005);
        }

        public virtual int ErrorCode
        {
            get
            {
                return HResult;
            }
        }

        public override String ToString()
        {
            String message = Message;
            String s;
            String _className = GetType().ToString();
            s = _className + " (0x" + HResult.ToString("X8", CultureInfo.InvariantCulture) + ")";

            if (!(String.IsNullOrEmpty(message)))
            {
                s = s + ": " + message;
            }

            Exception _innerException = InnerException;

            if (_innerException != null)
            {
                s = s + " ---> " + _innerException.ToString();
            }


            if (StackTrace != null)
                s += Environment.NewLine + StackTrace;

            return s;
        }
    }
}

#endif
