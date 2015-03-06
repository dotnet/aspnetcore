// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.OptionsModel;
using Xunit;

namespace Microsoft.AspNet.Mvc.TagHelpers.Test
{
    public class TagHelperOptionsCollectionExtensionsTest
    {
        public static TheoryData ConfigureForm_GetsOptionsFromConfigurationCorrectly_Data
        {
            get
            {
                return new TheoryData<string, bool?>
                {
                    { "true", true },
                    { "false", false },
                    { "True", true },
                    { "False", false },
                    { "TRue", true },
                    { "FAlse", false },
                    { null, null }
                };
            }
        }

        [Theory]
        [MemberData(nameof(ConfigureForm_GetsOptionsFromConfigurationCorrectly_Data))]
        public void ConfigureForm_GetsOptionsFromConfigurationCorrectly(string configValue, bool? expectedValue)
        {
            // Arrange
            var configValues = new Dictionary<string, string>
            {
                { $"{nameof(FormTagHelperOptions.GenerateAntiForgeryToken)}", configValue }
            };
            var config = new Configuration(new MemoryConfigurationSource(configValues));
            var services = new ServiceCollection().AddOptions();
            services.ConfigureTagHelpers().ConfigureForm(config);
            var serviceProvider = services.BuildServiceProvider();

            // Act
            var options = serviceProvider.GetService<IOptions<FormTagHelperOptions>>().Options;

            // Assert
            Assert.Equal(expectedValue, options.GenerateAntiForgeryToken);
        }
    }
}