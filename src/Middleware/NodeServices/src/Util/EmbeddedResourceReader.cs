using System;
using System.IO;
using System.Reflection;

namespace Microsoft.AspNetCore.NodeServices
{
    /// <summary>
    /// Contains methods for reading embedded resources.
    /// </summary>
    public static class EmbeddedResourceReader
    {
        /// <summary>
        /// Reads the specified embedded resource from a given assembly.
        /// </summary>
        /// <param name="assemblyContainingType">Any <see cref="Type"/> in the assembly whose resource is to be read.</param>
        /// <param name="path">The path of the resource to be read.</param>
        /// <returns>The contents of the resource.</returns>
        public static string Read(Type assemblyContainingType, string path)
        {
            var asm = assemblyContainingType.GetTypeInfo().Assembly;
            var embeddedResourceName = asm.GetName().Name + path.Replace("/", ".");

            using (var stream = asm.GetManifestResourceStream(embeddedResourceName))
            using (var sr = new StreamReader(stream))
            {
                return sr.ReadToEnd();
            }
        }
    }
}