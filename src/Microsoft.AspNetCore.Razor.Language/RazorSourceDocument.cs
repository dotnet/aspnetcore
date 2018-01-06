// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.AspNetCore.Razor.Language
{
    /// <summary>
    /// The Razor template source.
    /// </summary>
    public abstract class RazorSourceDocument
    {
        internal const int LargeObjectHeapLimitInChars = 40 * 1024; // 40K Unicode chars is 80KB which is less than the large object heap limit.

        internal static readonly RazorSourceDocument[] EmptyArray = new RazorSourceDocument[0];

        /// <summary>
        /// Gets the encoding of the text in the original source document.
        /// </summary>
        /// <remarks>
        /// Depending on the method used to create a <see cref="RazorSourceDocument"/> the encoding may be used to
        /// read the file contents, or it may be solely informational. Refer to the documentation on the method
        /// used to create the <see cref="RazorSourceDocument"/> for details.
        /// </remarks>
        public abstract Encoding Encoding { get; }

        /// <summary>
        /// Gets the file path of the orginal source document.
        /// </summary>
        /// <remarks>
        /// The file path may be either an absolute path or project-relative path. An absolute path is required
        /// to generate debuggable assemblies.
        /// </remarks>
        public abstract string FilePath { get; }

        /// <summary>
        /// Gets the project-relative path to the source file. May be <c>null</c>.
        /// </summary>
        /// <remarks>
        /// The relative path (if provided) is used for display (error messages). The project-relative path may also
        /// be used to embed checksums of the original source documents to support runtime recompilation of Razor code.
        /// </remarks>
        public virtual string RelativePath => null;

        /// <summary>
        /// Gets a character at given position.
        /// </summary>
        /// <param name="position">The position to get the character from.</param>
        public abstract char this[int position] { get; }

        /// <summary>
        /// Gets the length of the text in characters.
        /// </summary>
        public abstract int Length { get; }

        /// <summary>
        /// Gets the <see cref="RazorSourceLineCollection"/>.
        /// </summary>
        public abstract RazorSourceLineCollection Lines { get; }

        /// <summary>
        /// Copies a range of characters from the <see cref="RazorSourceDocument"/> to the specified <paramref name="destination"/>.
        /// </summary>
        /// <param name="sourceIndex">The index of the first character in this instance to copy.</param>
        /// <param name="destination">The destination buffer.</param>
        /// <param name="destinationIndex">The index in destination at which the copy operation begins.</param>
        /// <param name="count">The number of characters in this instance to copy to destination.</param>
        public abstract void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count);

        /// <summary>
        /// Calculates the checksum for the <see cref="RazorSourceDocument"/>.
        /// </summary>
        /// <returns>The checksum.</returns>
        public abstract byte[] GetChecksum();

        /// <summary>
        /// Gets the name of the algorithm used to compute the checksum returned by <see cref="GetChecksum"/>.
        /// </summary>
        /// <remarks>
        /// This member did not exist in the 2.0 release, so it is possible for an implementation to return
        /// the wrong value (or <c>null</c>). Implementations of <see cref="RazorSourceDocument"/> should
        /// override this member and specify their choice of hash algorithm even if it is the same as the
        /// default (<c>SHA1</c>).
        /// </remarks>
        public virtual string GetChecksumAlgorithm()
        {
            return HashAlgorithmName.SHA1.Name;
        }

        /// <summary>
        /// Gets the file path in a format that should be used for display.
        /// </summary>
        /// <returns>The <see cref="RelativePath"/> if set, or the <see cref="FilePath"/>.</returns>
        public virtual string GetFilePathForDisplay()
        {
            return RelativePath ?? FilePath;
        }

        /// <summary>
        /// Reads the <see cref="RazorSourceDocument"/> from the specified <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to read from.</param>
        /// <param name="fileName">The file name of the template.</param>
        /// <returns>The <see cref="RazorSourceDocument"/>.</returns>
        public static RazorSourceDocument ReadFrom(Stream stream, string fileName)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            var properties = new RazorSourceDocumentProperties(fileName, relativePath: null);
            return new StreamSourceDocument(stream, null, properties);
        }

        /// <summary>
        /// Reads the <see cref="RazorSourceDocument"/> from the specified <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to read from.</param>
        /// <param name="fileName">The file name of the template.</param>
        /// <param name="encoding">The <see cref="System.Text.Encoding"/> to use to read the <paramref name="stream"/>.</param>
        /// <returns>The <see cref="RazorSourceDocument"/>.</returns>
        public static RazorSourceDocument ReadFrom(Stream stream, string fileName, Encoding encoding)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (encoding == null)
            {
                throw new ArgumentNullException(nameof(encoding));
            }

            var properties = new RazorSourceDocumentProperties(fileName, relativePath: null);
            return new StreamSourceDocument(stream, encoding, properties);
        }

        /// <summary>
        /// Reads the <see cref="RazorSourceDocument"/> from the specified <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to read from.</param>
        /// <param name="encoding">The <see cref="System.Text.Encoding"/> to use to read the <paramref name="stream"/>.</param>
        /// <param name="properties">Properties to configure the <see cref="RazorSourceDocument"/>.</param>
        /// <returns>The <see cref="RazorSourceDocument"/>.</returns>
        public static RazorSourceDocument ReadFrom(Stream stream, Encoding encoding, RazorSourceDocumentProperties properties)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (encoding == null)
            {
                throw new ArgumentNullException(nameof(encoding));
            }

            if (properties == null)
            {
                throw new ArgumentNullException(nameof(properties));
            }
            
            return new StreamSourceDocument(stream, encoding, properties);
        }

        /// <summary>
        /// Reads the <see cref="RazorSourceDocument"/> from the specified <paramref name="projectItem"/>.
        /// </summary>
        /// <param name="projectItem">The <see cref="RazorProjectItem"/> to read from.</param>
        /// <returns>The <see cref="RazorSourceDocument"/>.</returns>
        public static RazorSourceDocument ReadFrom(RazorProjectItem projectItem)
        {
            if (projectItem == null)
            {
                throw new ArgumentNullException(nameof(projectItem));
            }

            // ProjectItem.PhysicalPath is usually an absolute (rooted) path.
            var filePath = projectItem.PhysicalPath; 
            if (string.IsNullOrEmpty(filePath))
            {
                // Fall back to the relative path only if necessary.
                filePath = projectItem.FilePath;
            }

            using (var stream = projectItem.Read())
            {
                // Autodetect the encoding.
                return new StreamSourceDocument(stream, null, new RazorSourceDocumentProperties(filePath, projectItem.FilePath));
            }
        }

        /// <summary>
        /// Creates a <see cref="RazorSourceDocument"/> from the specified <paramref name="content"/>.
        /// </summary>
        /// <param name="content">The source document content.</param>
        /// <param name="fileName">The file name of the <see cref="RazorSourceDocument"/>.</param>
        /// <returns>The <see cref="RazorSourceDocument"/>.</returns>
        /// <remarks>Uses <see cref="System.Text.Encoding.UTF8" /></remarks>
        public static RazorSourceDocument Create(string content, string fileName)
            => Create(content, fileName, Encoding.UTF8);

        /// <summary>
        /// Creates a <see cref="RazorSourceDocument"/> from the specified <paramref name="content"/>.
        /// </summary>
        /// <param name="content">The source document content.</param>
        /// <param name="fileName">The file name of the <see cref="RazorSourceDocument"/>.</param>
        /// <param name="encoding">The <see cref="System.Text.Encoding"/> of the file <paramref name="content"/> was read from.</param>
        /// <returns>The <see cref="RazorSourceDocument"/>.</returns>
        public static RazorSourceDocument Create(string content, string fileName, Encoding encoding)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            if (encoding == null)
            {
                throw new ArgumentNullException(nameof(encoding));
            }

            var properties = new RazorSourceDocumentProperties(fileName, relativePath: null);
            return new StringSourceDocument(content, encoding, properties);
        }

        /// <summary>
        /// Creates a <see cref="RazorSourceDocument"/> from the specified <paramref name="content"/>.
        /// </summary>
        /// <param name="content">The source document content.</param>
        /// <param name="encoding">The encoding of the source document.</param>
        /// <param name="properties">Properties to configure the <see cref="RazorSourceDocument"/>.</param>
        /// <returns>The <see cref="RazorSourceDocument"/>.</returns>
        public static RazorSourceDocument Create(string content, Encoding encoding, RazorSourceDocumentProperties properties)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            if (encoding == null)
            {
                throw new ArgumentNullException(nameof(encoding));
            }

            if (properties == null)
            {
                throw new ArgumentNullException(nameof(properties));
            }

            return new StringSourceDocument(content, encoding, properties);
        }
    }
}
