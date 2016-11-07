using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace SocialWeather
{
    public class PipeWeatherStreamFormatter : IStreamFormatter<WeatherReport>
    {
        public async Task<WeatherReport> ReadAsync(Stream stream)
        {
            var sr = new StreamReader(stream);
            var line = await sr.ReadLineAsync();

            if (line == null)
            {
                return null;
            }

            var tokens = line.Split('|');
            int temperature;
            long reportTime = long.MinValue;
            Weather weather = (Weather)(-1);
            string zipCode = tokens.Length > 3 ? tokens[3] : string.Empty;

            if(tokens.Length == 0 || !int.TryParse(tokens[0], out temperature))
            {
                temperature = int.MinValue;
            }

            if (tokens.Length < 2 || !long.TryParse(tokens[1], out reportTime))
            {
                temperature = int.MinValue;
            }

            if (tokens.Length < 3 || !Enum.TryParse<Weather>(tokens[2], out weather))
            {
                weather = (Weather)(-1);
            }

            return new WeatherReport
            {
                Temperature = temperature,
                ReportTime = reportTime,
                Weather = weather,
                ZipCode = zipCode
            };
        }

        public async Task WriteAsync(WeatherReport report, Stream stream)
        {
            var sw = new StreamWriter(stream);
            var line = $"{report.Temperature}|{report.ReportTime}|{(int)report.Weather}|{report.ZipCode ?? string.Empty}";

            Encoding utf8 = Encoding.UTF8;
            var encodedBytes = utf8.GetBytes(line);
            var convertedBytes = Encoding.Convert(Encoding.UTF8, Encoding.ASCII, encodedBytes);

            await sw.WriteLineAsync(Encoding.ASCII.GetString(convertedBytes));
            await sw.FlushAsync();
        }
    }
}
