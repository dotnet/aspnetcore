using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PersisitentConnection
{
    public interface IStreamFormatter<T>
    {
        Task<T> ReadAsync(Stream stream);
        Task WriteAsync(T value, Stream stream);
    }
}
