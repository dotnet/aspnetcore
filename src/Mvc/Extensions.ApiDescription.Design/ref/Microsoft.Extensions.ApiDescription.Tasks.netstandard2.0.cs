// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.ApiDescription.Tasks
{
    public partial class DownloadFile : Microsoft.Build.Utilities.Task, Microsoft.Build.Framework.ICancelableTask, Microsoft.Build.Framework.ITask
    {
        public DownloadFile() { }
        [Microsoft.Build.Framework.RequiredAttribute]
        public string DestinationPath { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool Overwrite { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int TimeoutSeconds { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        [Microsoft.Build.Framework.RequiredAttribute]
        public string Uri { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public void Cancel() { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public static System.Threading.Tasks.Task DownloadAsync(string uri, string destinationPath, System.Net.Http.HttpClient httpClient, System.Threading.CancellationToken cancellationToken, Microsoft.Build.Utilities.TaskLoggingHelper log, int timeoutSeconds) { throw null; }
        public override bool Execute() { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task<bool> ExecuteAsync() { throw null; }
    }
    public partial class GetCurrentItems : Microsoft.Build.Utilities.Task
    {
        public GetCurrentItems() { }
        [Microsoft.Build.Framework.RequiredAttribute]
        public string Input { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        [Microsoft.Build.Framework.OutputAttribute]
        public Microsoft.Build.Framework.ITaskItem[] Outputs { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public override bool Execute() { throw null; }
    }
    public partial class GetFileReferenceMetadata : Microsoft.Build.Utilities.Task
    {
        public GetFileReferenceMetadata() { }
        [Microsoft.Build.Framework.RequiredAttribute]
        public string Extension { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        [Microsoft.Build.Framework.RequiredAttribute]
        public Microsoft.Build.Framework.ITaskItem[] Inputs { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        [Microsoft.Build.Framework.RequiredAttribute]
        public string Namespace { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string OutputDirectory { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        [Microsoft.Build.Framework.OutputAttribute]
        public Microsoft.Build.Framework.ITaskItem[] Outputs { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public override bool Execute() { throw null; }
    }
    public partial class GetProjectReferenceMetadata : Microsoft.Build.Utilities.Task
    {
        public GetProjectReferenceMetadata() { }
        public string DocumentDirectory { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        [Microsoft.Build.Framework.RequiredAttribute]
        public Microsoft.Build.Framework.ITaskItem[] Inputs { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        [Microsoft.Build.Framework.OutputAttribute]
        public Microsoft.Build.Framework.ITaskItem[] Outputs { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public override bool Execute() { throw null; }
    }
    public partial class GetUriReferenceMetadata : Microsoft.Build.Utilities.Task
    {
        public GetUriReferenceMetadata() { }
        public string DocumentDirectory { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        [Microsoft.Build.Framework.RequiredAttribute]
        public Microsoft.Build.Framework.ITaskItem[] Inputs { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        [Microsoft.Build.Framework.OutputAttribute]
        public Microsoft.Build.Framework.ITaskItem[] Outputs { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public override bool Execute() { throw null; }
    }
    public static partial class MetadataSerializer
    {
        public static Microsoft.Build.Framework.ITaskItem DeserializeMetadata(string value) { throw null; }
        public static string SerializeMetadata(Microsoft.Build.Framework.ITaskItem item) { throw null; }
        public static void SetMetadata(Microsoft.Build.Framework.ITaskItem item, string key, string value) { }
    }
}
