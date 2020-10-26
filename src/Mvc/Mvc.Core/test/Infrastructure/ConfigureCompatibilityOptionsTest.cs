// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Infrastructure
{
    public class ConfigureCompatibilityOptionsTest
    {
        [Fact]
        public void PostConfigure_NoValueForProperty_DoesNothing()
        {
            // Arrange
            var configure = Create(CompatibilityVersion.Version_3_0, new Dictionary<string, object>());

            var options = new TestOptions();

            // Act
            configure.PostConfigure(Options.DefaultName, options);

            // Assert
            Assert.False(options.TestProperty);
        }

        [Fact]
        public void PostConfigure_ValueIsSet_DoesNothing()
        {
            // Arrange
            var configure = Create(
                CompatibilityVersion.Version_3_0,
                new Dictionary<string, object>
                {
                    { nameof(TestOptions.TestProperty), true },
                });

            var options = new TestOptions()
            {
                TestProperty = false,
            };

            // Act
            configure.PostConfigure(Options.DefaultName, options);

            // Assert
            Assert.False(options.TestProperty);
        }

        [Fact]
        public void PostConfigure_ValueNotSet_SetsValue()
        {
            // Arrange
            var configure = Create(
                CompatibilityVersion.Version_3_0,
                new Dictionary<string, object>
                {
                    { nameof(TestOptions.TestProperty), true },
                });

            var options = new TestOptions();

            // Act
            configure.PostConfigure(Options.DefaultName, options);

            // Assert
            Assert.True(options.TestProperty);
        }

        private static ConfigureCompatibilityOptions<TestOptions> Create(
            CompatibilityVersion version,
            IReadOnlyDictionary<string, object> defaultValues)
        {
            var compatibilityOptions = Options.Create(new MvcCompatibilityOptions() { CompatibilityVersion = version });
            return new TestConfigure(NullLoggerFactory.Instance, compatibilityOptions, defaultValues);
        }

        private class TestOptions : IEnumerable<ICompatibilitySwitch>
        {
            private readonly CompatibilitySwitch<bool> _testProperty;

            private readonly ICompatibilitySwitch[] _switches;

            public TestOptions()
            {
                _testProperty = new CompatibilitySwitch<bool>(nameof(TestProperty));
                _switches = new ICompatibilitySwitch[] { _testProperty };
            }

            public bool TestProperty
            {
                get => _testProperty.Value;
                set => _testProperty.Value = value;
            }

            public IEnumerator<ICompatibilitySwitch> GetEnumerator()
            {
                return ((IEnumerable<ICompatibilitySwitch>)_switches).GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => _switches.GetEnumerator();
        }

        private class TestConfigure : ConfigureCompatibilityOptions<TestOptions>
        {
            public TestConfigure(
                ILoggerFactory loggerFactory,
                IOptions<MvcCompatibilityOptions> compatibilityOptions,
                IReadOnlyDictionary<string, object> defaultValues)
                : base(loggerFactory, compatibilityOptions)
            {
                DefaultValues = defaultValues;
            }

            protected override IReadOnlyDictionary<string, object> DefaultValues { get; }
        }
    }
}
