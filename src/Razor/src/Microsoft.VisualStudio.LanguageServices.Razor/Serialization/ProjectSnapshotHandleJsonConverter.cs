// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.VisualStudio.LanguageServices.Razor.Serialization
{
    internal class ProjectSnapshotHandleJsonConverter : JsonConverter
    {
        public static readonly ProjectSnapshotHandleJsonConverter Instance = new ProjectSnapshotHandleJsonConverter();

        public override bool CanConvert(Type objectType)
        {
            return typeof(ProjectSnapshotHandle).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.StartObject)
            {
                return null;
            }

            var obj = JObject.Load(reader);
            var filePath = obj[nameof(ProjectSnapshotHandle.FilePath)].Value<string>();
            var configuration = obj[nameof(ProjectSnapshotHandle.Configuration)].ToObject<RazorConfiguration>(serializer);
            
            var id = obj[nameof(ProjectSnapshotHandle.WorkspaceProjectId)].Value<string>();
            var workspaceProjectId = id == null ? null : ProjectId.CreateFromSerialized(Guid.Parse(id));

            return new ProjectSnapshotHandle(filePath, configuration, workspaceProjectId);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var handle = (ProjectSnapshotHandle)value;

            writer.WriteStartObject();

            writer.WritePropertyName(nameof(ProjectSnapshotHandle.FilePath));
            writer.WriteValue(handle.FilePath);

            if (handle.Configuration == null)
            {
                writer.WritePropertyName(nameof(ProjectSnapshotHandle.Configuration));
                writer.WriteNull();
            }
            else
            {
                writer.WritePropertyName(nameof(ProjectSnapshotHandle.Configuration));
                serializer.Serialize(writer, handle.Configuration);
            }

            if (handle.WorkspaceProjectId == null)
            {
                writer.WritePropertyName(nameof(ProjectSnapshotHandle.WorkspaceProjectId));
                writer.WriteNull();
            }
            else
            {
                writer.WritePropertyName(nameof(ProjectSnapshotHandle.WorkspaceProjectId));
                writer.WriteValue(handle.WorkspaceProjectId.Id);
            }

            writer.WriteEndObject();
        }
    }
}
