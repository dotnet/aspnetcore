using System;

namespace AspNetCoreSdkTests.Util
{
    public class TempDir : IDisposable
    {
        public string Path { get; }

        public TempDir()
        {
            Path = IOUtil.GetTempDir();
        }

        public void Dispose()
        {
            IOUtil.DeleteDir(Path);
        }
    }
}
