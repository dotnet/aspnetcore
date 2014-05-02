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

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class CompilationResult
    {
        private readonly Type _type;

        private CompilationResult(string generatedCode, Type type, IEnumerable<CompilationMessage> messages)
        {
            _type = type;
            GeneratedCode = generatedCode;
            Messages = messages.ToList();
        }

        public IEnumerable<CompilationMessage> Messages { get; private set; }
        
        public string GeneratedCode { get; private set; }

        public Type CompiledType
        {
            get
            {
                if (_type == null)
                {
                    throw new CompilationFailedException(Messages, GeneratedCode);
                }

                return _type;
            }
        }

        public static CompilationResult Failed(string generatedCode, IEnumerable<CompilationMessage> messages)
        {
            return new CompilationResult(generatedCode, type: null, messages: messages);
        }

        public static CompilationResult Successful(string generatedCode, Type type)
        {
            return new CompilationResult(generatedCode, type, Enumerable.Empty<CompilationMessage>());
        }
    }
}
