// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace SocialWeather.Pipe;

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
        var reportTime = long.MinValue;
        var weather = (Weather)(-1);
        var zipCode = tokens.Length > 3 ? tokens[3] : string.Empty;

        if (tokens.Length == 0 || !int.TryParse(tokens[0], out temperature))
        {
            temperature = int.MinValue;
        }

        if (tokens.Length < 2 || !Enum.TryParse<Weather>(tokens[1], out weather))
        {
            weather = (Weather)(-1);
        }

        if (tokens.Length < 3 || !long.TryParse(tokens[2], out reportTime))
        {
            reportTime = int.MinValue;
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

        var utf8 = Encoding.UTF8;
        var encodedBytes = utf8.GetBytes(line);
        var convertedBytes = Encoding.Convert(Encoding.UTF8, Encoding.ASCII, encodedBytes);

        await sw.WriteLineAsync(Encoding.ASCII.GetString(convertedBytes));
        await sw.FlushAsync();
    }
}
