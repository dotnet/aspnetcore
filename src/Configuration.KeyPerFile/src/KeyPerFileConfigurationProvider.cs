using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Extensions.Configuration.KeyPerFile
{
    /// <summary>
    /// A <see cref="ConfigurationProvider"/> that uses a directory's files as configuration key/values.
    /// </summary>
    public class KeyPerFileConfigurationProvider : ConfigurationProvider
    {
        KeyPerFileConfigurationSource Source { get; set; }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="source">The settings.</param>
        public KeyPerFileConfigurationProvider(KeyPerFileConfigurationSource source)
            => Source = source ?? throw new ArgumentNullException(nameof(source));

        private static string NormalizeKey(string key)
            => key.Replace("__", ConfigurationPath.KeyDelimiter);

        private static string TrimNewLine(string value)
            => value.EndsWith(Environment.NewLine)
                ? value.Substring(0, value.Length - Environment.NewLine.Length)
                : value;

        /// <summary>
        /// Loads the docker secrets.
        /// </summary>
        public override void Load()
        {
            Data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (Source.FileProvider == null)
            {
                if (Source.Optional)
                {
                    return;
                }
                else
                {
                    throw new DirectoryNotFoundException("A non-null file provider for the directory is required when this source is not optional.");
                }
            }

            var directory = Source.FileProvider.GetDirectoryContents("/");
            if (!directory.Exists && !Source.Optional)
            {
                throw new DirectoryNotFoundException("The root directory for the FileProvider doesn't exist and is not optional.");
            }

            foreach (var file in directory)
            {
                if (file.IsDirectory)
                {
                    continue;
                }

                using (var stream = file.CreateReadStream())
                using (var streamReader = new StreamReader(stream))
                {
                    if (Source.IgnoreCondition == null || !Source.IgnoreCondition(file.Name))
                    {
                        Data.Add(NormalizeKey(file.Name), TrimNewLine(streamReader.ReadToEnd()));
                    }
                }
            }
        }
    }
}
