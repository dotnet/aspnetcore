using System;
using System.IO;

namespace Microsoft.AspNetCore.NodeServices {
    // Makes it easier to pass script files to Node in a way that's sure to clean up after the process exits
    public sealed class StringAsTempFile : IDisposable {
        public string FileName { get; private set; }

        private bool _disposedValue;

        public StringAsTempFile(string content) {
            this.FileName = Path.GetTempFileName();
            File.WriteAllText(this.FileName, content);
        }

        private void DisposeImpl(bool disposing)
        {
            if (!_disposedValue) {
                if (disposing) {
                    // TODO: dispose managed state (managed objects).
                }

                File.Delete(this.FileName);

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            DisposeImpl(true);
            GC.SuppressFinalize(this);
        }

        ~StringAsTempFile() {
           DisposeImpl(false);
        }
    }
}
