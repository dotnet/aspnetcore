using System;
using System.IO;

namespace Microsoft.AspNetCore.NodeServices
{
    /// <summary>
    /// Makes it easier to pass script files to Node in a way that's sure to clean up after the process exits.
    /// </summary>
    public sealed class StringAsTempFile : IDisposable
    {
        private bool _disposedValue;

        /// <summary>
        /// Create a new instance of <see cref="StringAsTempFile"/>.
        /// </summary>
        /// <param name="content">The contents of the temporary file to be created.</param>
        public StringAsTempFile(string content)
        {
            FileName = Path.GetTempFileName();
            File.WriteAllText(FileName, content);
        }

        /// <summary>
        /// Specifies the filename of the temporary file.
        /// </summary>
        public string FileName { get; }

        /// <summary>
        /// Disposes the instance and deletes the associated temporary file.
        /// </summary>
        public void Dispose()
        {
            DisposeImpl(true);
            GC.SuppressFinalize(this);
        }

        private void DisposeImpl(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // Would dispose managed state here, if there was any
                }

                File.Delete(FileName);

                _disposedValue = true;
            }
        }

        /// <summary>
        /// Implements the finalization part of the IDisposable pattern by calling Dispose(false).
        /// </summary>
        ~StringAsTempFile()
        {
            DisposeImpl(false);
        }
    }
}