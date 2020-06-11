// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.StackTrace.Sources
{
    internal partial class ExceptionDetails
    {
        public ExceptionDetails() { }
        public System.Exception Error { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string ErrorMessage { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Collections.Generic.IEnumerable<Microsoft.Extensions.StackTrace.Sources.StackFrameSourceCodeInfo> StackFrames { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    internal partial class ExceptionDetailsProvider
    {
        public ExceptionDetailsProvider(Microsoft.Extensions.FileProviders.IFileProvider fileProvider, int sourceCodeLineCount) { }
        public System.Collections.Generic.IEnumerable<Microsoft.Extensions.StackTrace.Sources.ExceptionDetails> GetDetails(System.Exception exception) { throw null; }
        internal Microsoft.Extensions.StackTrace.Sources.StackFrameSourceCodeInfo GetStackFrameSourceCodeInfo(string method, string filePath, int lineNumber) { throw null; }
        internal void ReadFrameContent(Microsoft.Extensions.StackTrace.Sources.StackFrameSourceCodeInfo frame, System.Collections.Generic.IEnumerable<string> allLines, int errorStartLineNumberInFile, int errorEndLineNumberInFile) { }
    }
    internal partial class StackFrameSourceCodeInfo
    {
        public StackFrameSourceCodeInfo() { }
        public System.Collections.Generic.IEnumerable<string> ContextCode { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string ErrorDetails { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string File { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string Function { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int Line { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Collections.Generic.IEnumerable<string> PostContextCode { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Collections.Generic.IEnumerable<string> PreContextCode { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int PreContextLine { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
}
