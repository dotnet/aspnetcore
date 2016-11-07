using System.IO;
using System.Threading.Tasks;

namespace SocialWeather
{
    public interface IStreamFormatter<T>
    {
        Task<T> ReadAsync(Stream stream);
        Task WriteAsync(T value, Stream stream);
    }
}
