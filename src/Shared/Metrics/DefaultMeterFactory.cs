// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Metrics;
using System.Linq;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Metrics;

// TODO: Remove when Metrics DI intergration package is available https://github.com/dotnet/aspnetcore/issues/47618
internal sealed class DefaultMeterFactory : IMeterFactory
{
    private readonly IOptions<MetricsOptions> _options;
    private readonly IMeterRegistry _meterRegistry;
    private readonly Dictionary<MeterKey, Meter> _meters = new Dictionary<MeterKey, Meter>();

    public DefaultMeterFactory(IOptions<MetricsOptions> options, IMeterRegistry meterRegistry)
    {
        _options = options;
        _meterRegistry = meterRegistry;
    }

    public Meter CreateMeter(string name)
    {
        return CreateMeterCore(name, version: null, defaultTags: null);
    }

    public Meter CreateMeter(MeterOptions options)
    {
        return CreateMeterCore(options.Name, options.Version, options.DefaultTags);
    }

    private Meter CreateMeterCore(string name, string? version, IList<KeyValuePair<string, object?>>? defaultTags)
    {
        var tags = defaultTags?.ToArray();
        if (tags != null)
        {
            Array.Sort(tags, (t1, t2) => string.Compare(t1.Key, t2.Key, StringComparison.Ordinal));
        }
        var key = new MeterKey(name, version, tags);

        if (_meters.TryGetValue(key, out var meter))
        {
            return meter;
        }

        // TODO: Configure meter with default tags.
        meter = new Meter(name, version);
        _meters[key] = meter;
        _meterRegistry.Add(meter);

        return meter;
    }

    private readonly struct MeterKey : IEquatable<MeterKey>
    {
        public MeterKey(string name, string? version, KeyValuePair<string, object?>[]? defaultTags)
        {
            Name = name;
            Version = version;
            DefaultTags = defaultTags;
        }

        public string Name { get; }
        public string? Version { get; }
        public IList<KeyValuePair<string, object?>>? DefaultTags { get; }

        public bool Equals(MeterKey other)
        {
            return Name == other.Name
                && Version == other.Version
                && TagsEqual(other);
        }

        private bool TagsEqual(MeterKey other)
        {
            if (DefaultTags is null && other.DefaultTags is null)
            {
                return true;
            }
            if (DefaultTags is not null && other.DefaultTags is not null && DefaultTags.SequenceEqual(other.DefaultTags))
            {
                return true;
            }
            return false;
        }

        public override bool Equals(object? obj)
        {
            return obj is MeterKey key && Equals(key);
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(Name);
            hashCode.Add(Version);
            if (DefaultTags is not null)
            {
                foreach (var item in DefaultTags)
                {
                    hashCode.Add(item);
                }
            }

            return hashCode.ToHashCode();
        }
    }
}
