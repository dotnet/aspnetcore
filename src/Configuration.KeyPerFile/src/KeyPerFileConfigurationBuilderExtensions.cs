using System;
using System.IO;
using Microsoft.Extensions.Configuration.KeyPerFile;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// Extension methods for registering <see cref="KeyPerFileConfigurationProvider"/> with <see cref="IConfigurationBuilder"/>.
    /// </summary>
    public static class KeyPerFileConfigurationBuilderExtensions
    {
        /// <summary>
        /// Adds configuration using files from a directory. File names are used as the key,
        /// file contents are used as the value.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="directoryPath">The path to the directory.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder AddKeyPerFile(this IConfigurationBuilder builder, string directoryPath)
            => builder.AddKeyPerFile(directoryPath, optional: false, reloadOnChange: false);

        /// <summary>
        /// Adds configuration using files from a directory. File names are used as the key,
        /// file contents are used as the value.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="directoryPath">The path to the directory.</param>
        /// <param name="optional">Whether the directory is optional.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder AddKeyPerFile(this IConfigurationBuilder builder, string directoryPath, bool optional)
            => builder.AddKeyPerFile(directoryPath, optional, reloadOnChange: false);

        /// <summary>
        /// Adds configuration using files from a directory. File names are used as the key,
        /// file contents are used as the value.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="directoryPath">The path to the directory.</param>
        /// <param name="optional">Whether the directory is optional.</param>
        /// <param name="reloadOnChange">Whether the configuration should be reloaded if the files are changed, added or removed.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder AddKeyPerFile(this IConfigurationBuilder builder, string directoryPath, bool optional, bool reloadOnChange)
            => builder.AddKeyPerFile(source =>
            {
                // Only try to set the file provider if its not optional or the directory exists 
                if (!optional || Directory.Exists(directoryPath))
                {
                    source.FileProvider = new PhysicalFileProvider(directoryPath);
                }
                source.Optional = optional;
                source.ReloadOnChange = reloadOnChange;
            });

        /// <summary>
        /// Adds configuration using files from a directory. File names are used as the key,
        /// file contents are used as the value.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="configureSource">Configures the source.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder AddKeyPerFile(this IConfigurationBuilder builder, Action<KeyPerFileConfigurationSource> configureSource)
            => builder.Add(configureSource);
    }
}
