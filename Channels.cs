using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ConsoleApplication2
{
    class Program
    {
        static void Main(string[] args)
        {
            var desiredFeed = "vnext";

            var configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NuGet", "NuGet.config");
            var configFile = XDocument.Load(configPath);
            
            var configuration = configFile.Root;

            var keyPrefix = "ASP.NET 5";
            var keyFormat = keyPrefix + " ({0})";
            var aspNet5FeedNames = new[] { "vnext", "volatile", "release" };

            // Ensure all the ASP.NET 5 feeds are configured
            var packageSources = configuration.Descendants("packageSources").FirstOrDefault();
            if (packageSources == null)
            {
                packageSources = new XElement("packageSources");
                configuration.Add(packageSources);
            }

            var feedUrlFormat = "https://www.myget.org/F/aspnet{0}/api/v2";
            var currentAspNet5Feeds = packageSources.Descendants("add")
                .Where(el => el.Attributes("key").Any(key => key.Value.StartsWith(keyPrefix)));
            foreach (var feed in aspNet5FeedNames)
            {
                var fullFeedName = string.Format(keyFormat, feed);
                if (!currentAspNet5Feeds.Any(el => el.Attribute("key").Value == fullFeedName))
                {
                    var newFeed = new XElement("add");
                    newFeed.Add(new XAttribute("key", fullFeedName));
                    newFeed.Add(new XAttribute("value", string.Format(feedUrlFormat, feed)));
                    packageSources.Add(newFeed);
                }
            }

            // Disable all the ASP.NET 5 feeds other than the one selected
            var disabledPackageSources = configuration.Descendants("disabledPackageSources").FirstOrDefault();
            if (disabledPackageSources == null)
            {
                disabledPackageSources = new XElement("disabledPackageSources");
                configuration.Add(disabledPackageSources);
            }
            
            var currentDisabledAspNet5Feeds = disabledPackageSources.Descendants("add")
                .Where(el => el.Attributes("key").Any(key => key.Value.StartsWith(keyPrefix)))
                .ToList();
            foreach (var disabledFeed in currentDisabledAspNet5Feeds)
            {
                disabledFeed.Remove();
            }

            foreach (var feed in aspNet5FeedNames)
            {
                if (feed != desiredFeed)
                {
                    var fullFeedName = string.Format(keyFormat, feed);
                    var disabledFeed = new XElement("add");
                    disabledFeed.Add(new XAttribute("key", string.Format(keyFormat, feed)));
                    disabledFeed.Add(new XAttribute("value", true));
                    disabledPackageSources.Add(disabledFeed);
                }
            }

            // Save!
            configFile.Save(configPath);
        }
    }
}
