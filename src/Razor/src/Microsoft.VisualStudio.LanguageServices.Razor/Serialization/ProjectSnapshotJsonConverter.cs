// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Newtonsoft.Json;

namespace Microsoft.VisualStudio.LanguageServices.Razor.Serialization
{
    // We can't truly serialize a snapshot because it has access to a Workspace Project\
    //
    // Instead we serialize to a ProjectSnapshotHandle and then use that to re-create the snapshot
    // inside the remote host.
    internal class ProjectSnapshotJsonConverter : JsonConverter
    {
        public static readonly ProjectSnapshotJsonConverter Instance = new ProjectSnapshotJsonConverter();

        public override bool CanRead => false;

        public override bool CanWrite => true;
        
        public override bool CanConvert(Type objectType)
        {
            return typeof(ProjectSnapshot).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotSupportedException();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var project = (ProjectSnapshot)value;
            var handle = new ProjectSnapshotHandle(project.FilePath, project.Configuration, project.WorkspaceProject?.Id);

            ProjectSnapshotHandleJsonConverter.Instance.WriteJson(writer, handle, serializer);
        }
    }
}
