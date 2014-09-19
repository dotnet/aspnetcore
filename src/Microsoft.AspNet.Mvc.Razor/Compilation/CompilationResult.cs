// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNet.FileSystems;

namespace Microsoft.AspNet.Mvc.Razor
{
    /// <summary>
    /// Represents the result of compilation.
    /// </summary>
    public class CompilationResult
    {
        private Type _type;

        /// <summary>
        /// Creates a new instance of <see cref="CompilationResult"/>.
        /// </summary>
        protected CompilationResult()
        {
        }

        /// <summary>
        /// Gets the path of the Razor file that was compiled.
        /// </summary>
        public string FilePath
        {
            get
            {
                if (File != null)
                {
                    return File.PhysicalPath;
                }
                return null;
            }
        }

        /// <summary>
        /// Gets a sequence of <see cref="CompilationMessage"/> instances encountered during compilation.
        /// </summary>
        public IEnumerable<CompilationMessage> Messages { get; private set; }

        /// <summary>
        /// Gets (or sets in derived types) the generated C# content that was compiled.
        /// </summary>
        public string CompiledContent { get; protected set; }

        /// <summary>
        /// Gets (or sets in derived types) the type produced as a result of compilation.
        /// </summary>
        /// <exception cref="CompilationFailedException">An error occured during compilation.</exception>
        public Type CompiledType
        {
            get
            {
                if (_type == null)
                {
                    throw CreateCompilationFailedException();
                }

                return _type;
            }
            protected set
            {
                _type = value;
            }
        }

        private IFileInfo File { get; set; }

        /// <summary>
        /// Creates a <see cref="CompilationResult"/> that represents a failure in compilation.
        /// </summary>
        /// <param name="fileInfo">The <see cref="IFileInfo"/> for the Razor file that was compiled.</param>
        /// <param name="compilationContent">The generated C# content to be compiled.</param>
        /// <param name="messages">The sequence of failure messages encountered during compilation.</param>
        /// <returns>A CompilationResult instance representing a failure.</returns>
        public static CompilationResult Failed([NotNull] IFileInfo file,
                                               [NotNull] string compilationContent,
                                               [NotNull] IEnumerable<CompilationMessage> messages)
        {
            return new CompilationResult
            {
                File = file,
                CompiledContent = compilationContent,
                Messages = messages,
            };
        }

        /// <summary>
        /// Creates a <see cref="CompilationResult"/> that represents a success in compilation.
        /// </summary>
        /// <param name="type">The compiled type.</param>
        /// <returns>A CompilationResult instance representing a success.</returns>
        public static CompilationResult Successful([NotNull] Type type)
        {
            return new CompilationResult
            {
                CompiledType = type
            };
        }

        private CompilationFailedException CreateCompilationFailedException()
        {
            var fileContent = ReadContent(File);
            return new CompilationFailedException(FilePath, fileContent, CompiledContent, Messages);
        }

        private static string ReadContent(IFileInfo file)
        {
            try
            {
                using (var stream = file.CreateReadStream())
                {
                    using (var reader = new StreamReader(stream))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
            catch (Exception)
            {
                // Don't throw if reading the file fails.
                return string.Empty;
            }
        }
    }
}
