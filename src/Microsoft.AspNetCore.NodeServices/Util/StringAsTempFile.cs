using System;
using System.IO;

namespace Microsoft.AspNetCore.NodeServices
{
    // Makes it easier to pass script files to Node in a way that's sure to clean up after the process exits
    public sealed class StringAsTempFile : IDisposable
    {
        private bool _disposedValue;

        public StringAsTempFile(string content)
        {
            FileName = Path.GetTempFileName();
            File.WriteAllText(FileName, content);
        }

        public string FileName { get; }

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
                    // TODO: dispose managed state (managed objects).
                }

                File.Delete(FileName);

                _disposedValue = true;
            }
        }

        ~StringAsTempFile()
        {
            DisposeImpl(false);
        }
    }
}