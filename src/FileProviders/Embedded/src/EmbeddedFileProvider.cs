// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Shared;
using Microsoft.Extensions.FileProviders.Embedded;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.FileProviders;

/// <summary>
/// Looks up files using embedded resources in the specified assembly.
/// This file provider is case sensitive.
/// </summary>
public class EmbeddedFileProvider : IFileProvider
{
    private static readonly char[] _invalidFileNameChars = Path.GetInvalidFileNameChars()
        .Where(c => c != '/' && c != '\\').ToArray();

    private readonly Assembly _assembly;
    private readonly string _baseNamespace;
    private readonly DateTimeOffset _lastModified;

    /// <summary>
    /// Initializes a new instance of the <see cref="EmbeddedFileProvider" /> class using the specified
    /// assembly with the base namespace defaulting to the assembly name.
    /// </summary>
    /// <param name="assembly">The assembly that contains the embedded resources.</param>
    public EmbeddedFileProvider(Assembly assembly)
        : this(assembly, assembly?.GetName()?.Name)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EmbeddedFileProvider" /> class using the specified
    /// assembly and base namespace.
    /// </summary>
    /// <param name="assembly">The assembly that contains the embedded resources.</param>
    /// <param name="baseNamespace">The base namespace that contains the embedded resources.</param>
    [UnconditionalSuppressMessage("SingleFile", "IL3000:Assembly.Location",
        Justification = "The code handles if the Assembly.Location is empty. Workaround https://github.com/dotnet/runtime/issues/83607")]
    public EmbeddedFileProvider(Assembly assembly, string? baseNamespace)
    {
        ArgumentNullThrowHelper.ThrowIfNull(assembly);

        _baseNamespace = string.IsNullOrEmpty(baseNamespace) ? string.Empty : baseNamespace + ".";
        _assembly = assembly;

        _lastModified = DateTimeOffset.UtcNow;

        var assemblyLocation = assembly.Location;
        if (!string.IsNullOrEmpty(assemblyLocation))
        {
            try
            {
                _lastModified = File.GetLastWriteTimeUtc(assemblyLocation);
            }
            catch (PathTooLongException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    /// <summary>
    /// Locates a file at the given path.
    /// </summary>
    /// <param name="subpath">The path that identifies the file. </param>
    /// <returns>
    /// The file information. Caller must check Exists property. A <see cref="NotFoundFileInfo" /> if the file could
    /// not be found.
    /// </returns>
    public IFileInfo GetFileInfo(string subpath)
    {
        if (string.IsNullOrEmpty(subpath))
        {
            return new NotFoundFileInfo(subpath);
        }

        var builder = new StringBuilder(_baseNamespace.Length + subpath.Length);
        builder.Append(_baseNamespace);

        // Relative paths starting with a leading slash okay
        if (subpath.StartsWith("/", StringComparison.Ordinal))
        {
            subpath = subpath.Substring(1, subpath.Length - 1);
        }

        // Make valid everett id from directory name
        // The call to this method also replaces directory separator chars to dots
        var everettId = MakeValidEverettIdentifier(Path.GetDirectoryName(subpath));

        // if directory name was empty, everett id is empty as well
        if (!string.IsNullOrEmpty(everettId))
        {
            builder.Append(everettId);
            builder.Append('.');
        }

        // Append file name of path
        builder.Append(Path.GetFileName(subpath));

        var resourcePath = builder.ToString();
        if (HasInvalidPathChars(resourcePath))
        {
            return new NotFoundFileInfo(resourcePath);
        }

        var name = Path.GetFileName(subpath);
        if (_assembly.GetManifestResourceInfo(resourcePath) == null)
        {
            return new NotFoundFileInfo(name);
        }

        return new EmbeddedResourceFileInfo(_assembly, resourcePath, name, _lastModified);
    }

    /// <summary>
    /// Enumerate a directory at the given path, if any.
    /// This file provider uses a flat directory structure. Everything under the base namespace is considered to be one
    /// directory.
    /// </summary>
    /// <param name="subpath">The path that identifies the directory</param>
    /// <returns>
    /// Contents of the directory. Caller must check Exists property. A <see cref="NotFoundDirectoryContents" /> if no
    /// resources were found that match <paramref name="subpath" />
    /// </returns>
    public IDirectoryContents GetDirectoryContents(string subpath)
    {
        // The file name is assumed to be the remainder of the resource name.
        if (subpath == null)
        {
            return NotFoundDirectoryContents.Singleton;
        }

        // EmbeddedFileProvider only supports a flat file structure at the base namespace.
        if (subpath.Length != 0 && !string.Equals(subpath, "/", StringComparison.Ordinal))
        {
            return NotFoundDirectoryContents.Singleton;
        }

        var entries = new List<IFileInfo>();

        // TODO: The list of resources in an assembly isn't going to change. Consider caching.
        var resources = _assembly.GetManifestResourceNames();
        for (var i = 0; i < resources.Length; i++)
        {
            var resourceName = resources[i];
            if (resourceName.StartsWith(_baseNamespace, StringComparison.Ordinal))
            {
                entries.Add(new EmbeddedResourceFileInfo(
                    _assembly,
                    resourceName,
                    resourceName.Substring(_baseNamespace.Length),
                    _lastModified));
            }
        }

        return new EnumerableDirectoryContents(entries);
    }

    /// <summary>
    /// Embedded files do not change.
    /// </summary>
    /// <param name="pattern">This parameter is ignored</param>
    /// <returns>A <see cref="NullChangeToken" /></returns>
    public IChangeToken Watch(string pattern)
    {
        return NullChangeToken.Singleton;
    }

    private static bool HasInvalidPathChars(string path)
    {
        return path.IndexOfAny(_invalidFileNameChars) != -1;
    }

    #region Helper methods

    /// <summary>
    /// Is the character a valid first Everett identifier character?
    /// </summary>
    private static bool IsValidEverettIdFirstChar(char c)
    {
        return
            char.IsLetter(c) ||
            CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.ConnectorPunctuation;
    }

    /// <summary>
    /// Is the character a valid Everett identifier character?
    /// </summary>
    private static bool IsValidEverettIdChar(char c)
    {
        var cat = CharUnicodeInfo.GetUnicodeCategory(c);

        return
            char.IsLetterOrDigit(c) ||
            cat == UnicodeCategory.ConnectorPunctuation ||
            cat == UnicodeCategory.NonSpacingMark ||
            cat == UnicodeCategory.SpacingCombiningMark ||
            cat == UnicodeCategory.EnclosingMark;
    }

    /// <summary>
    /// Make a folder subname into an Everett-compatible identifier 
    /// </summary>
    private static void MakeValidEverettSubFolderIdentifier(StringBuilder builder, string subName)
    {
        if (string.IsNullOrEmpty(subName)) { return; }

        // the first character has stronger restrictions than the rest
        if (IsValidEverettIdFirstChar(subName[0]))
        {
            builder.Append(subName[0]);
        }
        else
        {
            builder.Append('_');
            if (IsValidEverettIdChar(subName[0]))
            {
                // if it is a valid subsequent character, prepend an underscore to it
                builder.Append(subName[0]);
            }
        }

        // process the rest of the subname
        for (var i = 1; i < subName.Length; i++)
        {
            if (!IsValidEverettIdChar(subName[i]))
            {
                builder.Append('_');
            }
            else
            {
                builder.Append(subName[i]);
            }
        }
    }

    /// <summary>
    /// Make a folder name into an Everett-compatible identifier
    /// </summary>
    internal static void MakeValidEverettFolderIdentifier(StringBuilder builder, string name)
    {
        if (string.IsNullOrEmpty(name)) { return; }

        // store the original length for use later
        var length = builder.Length;

        // split folder name into subnames separated by '.', if any
        var subNames = name.Split('.');

        // convert each subname separately
        MakeValidEverettSubFolderIdentifier(builder, subNames[0]);

        for (var i = 1; i < subNames.Length; i++)
        {
            builder.Append('.');
            MakeValidEverettSubFolderIdentifier(builder, subNames[i]);
        }

        // folder name cannot be a single underscore - add another underscore to it
        if ((builder.Length - length) == 1 && builder[length] == '_')
        {
            builder.Append('_');
        }
    }

    /// <summary>
    /// This method is provided for compatibility with Everett which used to convert parts of resource names into
    /// valid identifiers
    /// </summary>
    private static string? MakeValidEverettIdentifier(string? name)
    {
        if (string.IsNullOrEmpty(name)) { return name; }

        var everettId = new StringBuilder(name.Length);

        // split the name into folder names
        var subNames = name.Split(new[] { '/', '\\' });

        // convert every folder name
        MakeValidEverettFolderIdentifier(everettId, subNames[0]);

        for (var i = 1; i < subNames.Length; i++)
        {
            everettId.Append('.');
            MakeValidEverettFolderIdentifier(everettId, subNames[i]);
        }

        return everettId.ToString();
    }

    #endregion
}
