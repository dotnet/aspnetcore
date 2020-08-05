using System;
using System.IO;

namespace Microsoft.AspNetCore.Components.Web.Extensions
{
    public interface IFileListEntry
    {
        string? Name { get; }

        DateTime? LastModified { get; }

        int Size { get; }

        string? Type { get; }

        Stream Data { get; }

        event EventHandler OnDataRead;
    }
}
