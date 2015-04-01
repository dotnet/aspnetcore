// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.JsonPatch.Exceptions
{
    public class JsonPatchException : Exception
    {
        public new Exception InnerException { get; internal set; }

        public object AffectedObject { get; private set; }

        private string _message = "";
        public override string Message
        {
            get
            {
                return _message;
            }

        }

        public JsonPatchException()
        {

        }

        public JsonPatchException(string message, Exception innerException)
        {
            _message = message;
            InnerException = innerException;
        }
    }
}