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

        public virtual void Dispose()
        {
            IOUtil.DeleteDir(Path);
        }
    }
}
