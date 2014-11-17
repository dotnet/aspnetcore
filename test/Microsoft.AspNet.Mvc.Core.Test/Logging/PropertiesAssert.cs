using System;
using System.Linq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Logging
{
    public static class PropertiesAssert
    {
        /// <summary>
        /// Given two types, compares their properties and asserts true if they have the same property names.
        /// </summary>
        /// <param name="original">The original type to compare against.</param>
        /// <param name="shadow">The shadow type whose properties will be compared against the original.</param>
        /// <param name="exclude">Properties that exist in the original type but not the shadow.</param>
        /// <param name="include">Properties that are in the shadow type but not in the original.</param>
        public static void PropertiesAreTheSame(Type original, Type shadow, string[] exclude = null, string[] include = null)
        {
            var originalProperties = original.GetProperties().Where(p => !exclude?.Contains(p.Name) ?? true)
                .Select(p => p.Name);
            if (include != null)
            {
                originalProperties = originalProperties.Concat(include.ToList());
            }
            originalProperties = originalProperties.OrderBy(n => n);
            
            // Message is a property on all ILoggerStructures
            var shadowProperties = shadow.GetProperties().Where(p => !string.Equals("Message", p.Name))
                .Select(p => p.Name).OrderBy(n => n);

            Assert.True(originalProperties.SequenceEqual(shadowProperties));
        }
    }
}