using System;
using Microsoft.AspNet.Builder;
using Microsoft.Framework.ConfigurationModel;
using System.Globalization;
using System.Collections.Generic;

namespace Kestrel
{
    public class ServerInformation : IServerInformation
    {
        public ServerInformation()
        {
            Addresses = new List<ServerAddress>();
        }

        public void Initialize(IConfiguration configuration)
        {
            string urls;
            if (!configuration.TryGet("server.urls", out urls))
            {
                urls = "http://+:5000/";
            }
            foreach (var url in urls.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string scheme;
                string host;
                int port;
                string path;
                if (DeconstructUrl(url, out scheme, out host, out port, out path))
                {
                    Addresses.Add(
                        new ServerAddress
                        {
                            Scheme = scheme,
                            Host = host,
                            Port = port,
                            Path = path
                        });
                }
            }
        }

        public string Name
        {
            get
            {
                return "Kestrel";
            }
        }

        public IList<ServerAddress> Addresses { get; private set; }

        internal static bool DeconstructUrl(
           string url,
           out string scheme,
           out string host,
           out int port,
           out string path)
        {
            url = url ?? string.Empty;

            int delimiterStart1 = url.IndexOf("://", StringComparison.Ordinal);
            if (delimiterStart1 < 0)
            {
                scheme = null;
                host = null;
                port = 0;
                path = null;
                return false;
            }
            int delimiterEnd1 = delimiterStart1 + "://".Length;

            int delimiterStart3 = url.IndexOf("/", delimiterEnd1, StringComparison.Ordinal);
            if (delimiterStart3 < 0)
            {
                delimiterStart3 = url.Length;
            }
            int delimiterStart2 = url.LastIndexOf(":", delimiterStart3 - 1, delimiterStart3 - delimiterEnd1, StringComparison.Ordinal);
            int delimiterEnd2 = delimiterStart2 + ":".Length;
            if (delimiterStart2 < 0)
            {
                delimiterStart2 = delimiterStart3;
                delimiterEnd2 = delimiterStart3;
            }

            scheme = url.Substring(0, delimiterStart1);
            string portString = url.Substring(delimiterEnd2, delimiterStart3 - delimiterEnd2);
            int portNumber;
            if (int.TryParse(portString, NumberStyles.Integer, CultureInfo.InvariantCulture, out portNumber))
            {
                host = url.Substring(delimiterEnd1, delimiterStart2 - delimiterEnd1);
                port = portNumber;
            }
            else
            {
                if (string.Equals(scheme, "http", StringComparison.OrdinalIgnoreCase))
                {
                    port = 80;
                }
                else if (string.Equals(scheme, "https", StringComparison.OrdinalIgnoreCase))
                {
                    port = 443;
                }
                else
                {
                    port = 0;
                }
                host = url.Substring(delimiterEnd1, delimiterStart3 - delimiterEnd1);
            }
            path = url.Substring(delimiterStart3);
            return true;
        }
    }
}
