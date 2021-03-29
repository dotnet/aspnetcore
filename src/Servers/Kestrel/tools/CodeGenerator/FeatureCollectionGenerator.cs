// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeGenerator
{
    public static class FeatureCollectionGenerator
    {
        public static string GenerateFile(string namespaceName, string className, string[] allFeatures, string[] implementedFeatures, string[] skipResetFeatures, string extraUsings, string fallbackFeatures)
        {
            // NOTE: This list MUST always match the set of feature interfaces implemented by TransportConnection.
            // See also: src/Kestrel/Http/TransportConnection.FeatureCollection.cs
            var features = allFeatures.Select((type, index) => new KnownFeature
            {
                Name = type,
                Index = index
            });

            return $@"// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
{extraUsings}

#nullable enable

namespace {namespaceName}
{{
    internal partial class {className} : IFeatureCollection{Each(implementedFeatures.Where(f => !skipResetFeatures.Contains(f)), feature => $@",
                              {new string(' ', className.Length)}{feature}")}
    {{
        // Implemented features{Each(implementedFeatures.Where(f => !skipResetFeatures.Contains(f)), feature => $@"
        internal protected {feature}? _current{feature};")}{Each(implementedFeatures.Where(f => skipResetFeatures.Contains(f)), feature => $@"
        internal protected {feature}? _current{feature};")}{(allFeatures.Where(f => !implementedFeatures.Contains(f)).FirstOrDefault() is not null ? @"

        // Other reserved feature slots" : "")}{Each(allFeatures.Where(f => !implementedFeatures.Contains(f)), feature => $@"
        internal protected {feature}? _current{feature};")}

        private int _featureRevision;

        private List<KeyValuePair<Type, object>>? MaybeExtra;

        private void FastReset()
        {{{Each(implementedFeatures.Where(f => !skipResetFeatures.Contains(f)), feature => $@"
            _current{feature} = this;")}
{Each(allFeatures.Where(f => !implementedFeatures.Contains(f)), feature => $@"
            _current{feature} = null;")}
        }}

        // Internal for testing
        internal void ResetFeatureCollection()
        {{
            FastReset();
            MaybeExtra?.Clear();
            _featureRevision++;
        }}

        private object? ExtraFeatureGet(Type key)
        {{
            if (MaybeExtra == null)
            {{
                return null;
            }}
            for (var i = 0; i < MaybeExtra.Count; i++)
            {{
                var kv = MaybeExtra[i];
                if (kv.Key == key)
                {{
                    return kv.Value;
                }}
            }}
            return null;
        }}

        private void ExtraFeatureSet(Type key, object? value)
        {{
            if (value == null)
            {{
                if (MaybeExtra == null)
                {{
                    return;
                }}
                for (var i = 0; i < MaybeExtra.Count; i++)
                {{
                    if (MaybeExtra[i].Key == key)
                    {{
                        MaybeExtra.RemoveAt(i);
                        return;
                    }}
                }}
            }}
            else
            {{
                if (MaybeExtra == null)
                {{
                    MaybeExtra = new List<KeyValuePair<Type, object>>(2);
                }}
                for (var i = 0; i < MaybeExtra.Count; i++)
                {{
                    if (MaybeExtra[i].Key == key)
                    {{
                        MaybeExtra[i] = new KeyValuePair<Type, object>(key, value);
                        return;
                    }}
                }}
                MaybeExtra.Add(new KeyValuePair<Type, object>(key, value));
            }}
        }}

        bool IFeatureCollection.IsReadOnly => false;

        int IFeatureCollection.Revision => _featureRevision;

        object? IFeatureCollection.this[Type key]
        {{
            get
            {{
                object? feature = null;{Each(features, feature => $@"
                {(feature.Index != 0 ? "else " : "")}if (key == typeof({feature.Name}))
                {{
                    feature = _current{feature.Name};
                }}")}
                else if (MaybeExtra != null)
                {{
                    feature = ExtraFeatureGet(key);
                }}

                return feature{(string.IsNullOrEmpty(fallbackFeatures) ? "" : $" ?? {fallbackFeatures}[key]")};
            }}

            set
            {{
                _featureRevision++;
{Each(features, feature => $@"
                {(feature.Index != 0 ? "else " : "")}if (key == typeof({feature.Name}))
                {{
                    _current{feature.Name} = ({feature.Name}?)value;
                }}")}
                else
                {{
                    ExtraFeatureSet(key, value);
                }}
            }}
        }}

        TFeature? IFeatureCollection.Get<TFeature>() where TFeature : default
        {{
            // Using Unsafe.As for the cast due to https://github.com/dotnet/runtime/issues/49614
            // The type of TFeature is confirmed by the typeof() check and the As cast only accepts
            // that type; however the Jit does not eliminate a regular cast in a shared generic.

            TFeature? feature = default;{Each(features, feature => $@"
            {(feature.Index != 0 ? "else " : "")}if (typeof(TFeature) == typeof({feature.Name}))
            {{
                feature = Unsafe.As<{feature.Name}?, TFeature?>(ref _current{feature.Name});
            }}")}
            else if (MaybeExtra != null)
            {{
                feature = (TFeature?)(ExtraFeatureGet(typeof(TFeature)));
            }}{(string.IsNullOrEmpty(fallbackFeatures) ? "" : $@"

            if (feature == null)
            {{
                feature = {fallbackFeatures}.Get<TFeature>();
            }}")}

            return feature;
        }}

        void IFeatureCollection.Set<TFeature>(TFeature? feature) where TFeature : default
        {{
            // Using Unsafe.As for the cast due to https://github.com/dotnet/runtime/issues/49614
            // The type of TFeature is confirmed by the typeof() check and the As cast only accepts
            // that type; however the Jit does not eliminate a regular cast in a shared generic.

            _featureRevision++;{Each(features, feature => $@"
            {(feature.Index != 0 ? "else " : "")}if (typeof(TFeature) == typeof({feature.Name}))
            {{
                _current{feature.Name} = Unsafe.As<TFeature?, {feature.Name}?>(ref feature);
            }}")}
            else
            {{
                ExtraFeatureSet(typeof(TFeature), feature);
            }}
        }}

        private IEnumerable<KeyValuePair<Type, object>> FastEnumerable()
        {{{Each(features, feature => $@"
            if (_current{feature.Name} != null)
            {{
                yield return new KeyValuePair<Type, object>(typeof({feature.Name}), _current{feature.Name});
            }}")}

            if (MaybeExtra != null)
            {{
                foreach (var item in MaybeExtra)
                {{
                    yield return item;
                }}
            }}
        }}

        IEnumerator<KeyValuePair<Type, object>> IEnumerable<KeyValuePair<Type, object>>.GetEnumerator() => FastEnumerable().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => FastEnumerable().GetEnumerator();
    }}
}}
";
        }

        static string Each<T>(IEnumerable<T> values, Func<T, string> formatter)
        {
            return values.Any() ? values.Select(formatter).Aggregate((a, b) => a + b) : "";
        }

        private class KnownFeature
        {
            public string Name;
            public int Index;
        }
    }
}
