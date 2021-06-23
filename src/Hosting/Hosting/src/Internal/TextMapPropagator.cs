// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Net;
using System.Collections.Generic;

namespace System.Diagnostics
{
    internal abstract class TextMapPropagator
    {
        public delegate bool PropagatorGetterCallback(object carrier, string fieldName, out string? value);

        public abstract IReadOnlyCollection<string> Fields { get; }

        // Inject

        public abstract bool Inject(Activity activity, object carrier, Action<object /* carrier */, string /* field name */, string /* value to inject */> setter);
        public abstract bool Inject(ActivityContext context, object carrier, Action<object /* carrier */, string /* field name */, string /* value to inject */> setter);
        public abstract bool Inject(IEnumerable<KeyValuePair<string, string?>> baggage, object carrier, Action<object /* carrier */, string /* field name */, string /* value to inject */> setter);

        // Extract

        public abstract bool Extract(object carrier, PropagatorGetterCallback getter, out string? id, out string? state);
        public abstract bool Extract(object carrier, PropagatorGetterCallback getter, out ActivityContext context);
        public abstract bool Extract(object carrier, PropagatorGetterCallback getter, out IEnumerable<KeyValuePair<string, string?>>? baggage);

        //
        // Static APIs
        //

        public static TextMapPropagator Default { get; set; } = CreateLegacyPropagator();

        // For Microsoft compatibility. e.g., it will propagate Baggage header name as "Correlation-Context" instead of "baggage".
        public static TextMapPropagator CreateLegacyPropagator() => new LegacyTextMapPropagator();

        // Suppress context propagation.
        public static TextMapPropagator CreateOutputSuppressionPropagator() => new OutputSuppressionPropagator();

        // propagate only root parent context and ignore any intermediate created context.
        public static TextMapPropagator CreatePassThroughPropagator() => new PassThroughPropagator();

        // Conform to the W3C specs https://www.w3.org/TR/trace-context/ & https://www.w3.org/TR/2020/WD-baggage-20201020/
        public static TextMapPropagator CreateW3CPropagator() => new W3CPropagator();

        //
        // Internal
        //

        internal const char Space = ' ';
        internal const char Tab = (char)9;
        internal const char Comma = ',';
        internal const char Semicolon = ';';

        internal const int MaxBaggageLength = 8192;
        internal const int MaxKeyValueLength = 4096;
        internal const int MaxBaggageItems = 180;

        internal const string TraceParent = "traceparent";
        internal const string RequestId = "Request-Id";
        internal const string TraceState = "tracestate";
        internal const string Baggage = "baggage";
        internal const string CorrelationContext = "Correlation-Context";

        internal static readonly char[] s_trimmingSpaceCharacters = new char[] { Space, Tab };

        internal static void InjectBaggage(object carrier, IEnumerable<KeyValuePair<string, string?>> baggage, Action<object /* carrier */, string /* field name */, string /* value to inject */> setter, bool injectAsW3C = false)
        {
            using (IEnumerator<KeyValuePair<string, string?>> e = baggage.GetEnumerator())
            {
                if (e.MoveNext())
                {
                    StringBuilder baggageList = new StringBuilder();
                    do
                    {
                        KeyValuePair<string, string?> item = e.Current;
                        baggageList.Append(WebUtility.UrlEncode(item.Key)).Append('=').Append(WebUtility.UrlEncode(item.Value)).Append(Comma);
                    }
                    while (e.MoveNext());
                    baggageList.Remove(baggageList.Length - 1, 1);
                    setter(carrier, injectAsW3C ? Baggage : CorrelationContext, baggageList.ToString());
                }
            }
        }

        internal static bool TryExtractBaggage(string baggagestring, out IEnumerable<KeyValuePair<string, string?>>? baggage)
        {
            baggage = null;
            int baggageLength = -1;
            List<KeyValuePair<string, string?>>? baggageList = null;

            if (string.IsNullOrEmpty(baggagestring))
            {
                return true;
            }

            int currentIndex = 0;

            do
            {
                // Skip spaces
                while (currentIndex < baggagestring.Length && (baggagestring[currentIndex] == Space || baggagestring[currentIndex] == Tab)) { currentIndex++; }

                if (currentIndex >= baggagestring.Length) { break; } // No Key exist

                int keyStart = currentIndex;

                // Search end of the key
                while (currentIndex < baggagestring.Length && baggagestring[currentIndex] != Space && baggagestring[currentIndex] != Tab && baggagestring[currentIndex] != '=') { currentIndex++; }

                if (currentIndex >= baggagestring.Length) { break; }

                int keyEnd = currentIndex;

                if (baggagestring[currentIndex] != '=')
                {
                    // Skip Spaces
                    while (currentIndex < baggagestring.Length && (baggagestring[currentIndex] == Space || baggagestring[currentIndex] == Tab)) { currentIndex++; }

                    if (currentIndex >= baggagestring.Length) { break; } // Wrong key format
                }

                if (baggagestring[currentIndex] != '=') { break; } // wrong key format.

                currentIndex++;

                // Skip spaces
                while (currentIndex < baggagestring.Length && (baggagestring[currentIndex] == Space || baggagestring[currentIndex] == Tab)) { currentIndex++; }

                if (currentIndex >= baggagestring.Length) { break; } // Wrong value format

                int valueStart = currentIndex;

                // Search end of the value
                while (currentIndex < baggagestring.Length && baggagestring[currentIndex] != Space && baggagestring[currentIndex] != Tab &&
                       baggagestring[currentIndex] != Comma && baggagestring[currentIndex] != Semicolon)
                { currentIndex++; }

                if (keyStart < keyEnd && valueStart < currentIndex)
                {
                    int keyValueLength = (keyEnd - keyStart) + (currentIndex - valueStart);
                    if (keyValueLength > MaxKeyValueLength || keyValueLength + baggageLength >= MaxBaggageLength)
                    {
                        break;
                    }

                    if (baggageList is null)
                    {
                        baggageList = new();
                    }

                    baggageLength += keyValueLength;

                    // Insert in reverse order for asp.net compatability.
                    baggageList.Insert(0, new KeyValuePair<string, string?>(
                                                WebUtility.UrlDecode(baggagestring.Substring(keyStart, keyEnd - keyStart)).Trim(s_trimmingSpaceCharacters),
                                                WebUtility.UrlDecode(baggagestring.Substring(valueStart, currentIndex - valueStart)).Trim(s_trimmingSpaceCharacters)));

                    if (baggageList.Count >= MaxBaggageItems)
                    {
                        break;
                    }
                }

                // Skip to end of values
                while (currentIndex < baggagestring.Length && baggagestring[currentIndex] != Comma) { currentIndex++; }

                currentIndex++; // Move to next key-value entry
            } while (currentIndex < baggagestring.Length);

            baggage = baggageList;
            return baggageList != null;
        }

        internal static bool InjectContext(ActivityContext context, object carrier, Action<object /* carrier */, string /* field name */, string /* value to inject */> setter)
        {
            if (context == default || setter is null || context.TraceId == default || context.SpanId == default)
            {
                return false;
            }

            Span<char> traceParent = stackalloc char[55];
            traceParent[0] = '0';
            traceParent[1] = '0';
            traceParent[2] = '-';
            traceParent[35] = '-';
            traceParent[52] = '-';
            CopyStringToSpan(context.TraceId.ToHexString(), traceParent.Slice(3, 32));
            CopyStringToSpan(context.SpanId.ToHexString(), traceParent.Slice(36, 16));
            HexConverter.ToCharsBuffer((byte)(context.TraceFlags & ActivityTraceFlags.Recorded), traceParent.Slice(53, 2), 0, HexConverter.Casing.Lower);

            setter(carrier, TraceParent, traceParent.ToString());

            string? tracestateStr = context.TraceState;
            if (tracestateStr?.Length > 0)
            {
                setter(carrier, TraceState, tracestateStr);
            }

            return true;
        }

        internal static bool LegacyExtract(object carrier, PropagatorGetterCallback getter, out string? id, out string? state)
        {
            if (getter is null)
            {
                id = null;
                state = null;
                return false;
            }

            getter(carrier, TraceParent, out id);
            if (id is null)
            {
                getter(carrier, RequestId, out id);
            }

            getter(carrier, TraceState, out state);
            return true;
        }

        internal static bool LegacyExtract(object carrier, PropagatorGetterCallback getter, out ActivityContext context)
        {
            context = default;

            if (getter is null)
            {
                return false;
            }

            getter(carrier, TraceParent, out string? traceParent);
            getter(carrier, TraceState, out string? traceState);

            return ActivityContext.TryParse(traceParent, traceState, out context);
        }

        internal static bool LegacyExtract(object carrier, PropagatorGetterCallback getter, out IEnumerable<KeyValuePair<string, string?>>? baggage)
        {
            baggage = null;
            if (getter is null)
            {
                return false;
            }

            getter(carrier, Baggage, out string? theBaggage);
            if (theBaggage is null || !TryExtractBaggage(theBaggage, out baggage))
            {
                getter(carrier, CorrelationContext, out theBaggage);
                if (theBaggage is not null)
                {
                    TryExtractBaggage(theBaggage, out baggage);
                }
            }

            return true;
        }

        internal static void CopyStringToSpan(string s, Span<char> span)
        {
            Debug.Assert(s is not null);
            Debug.Assert(s.Length == span.Length);

            for (int i = 0; i < s.Length; i++)
            {
                span[i] = s[i];
            }
        }
    }

    internal class LegacyTextMapPropagator : TextMapPropagator
    {
        //
        // Fields
        //

        public override IReadOnlyCollection<string> Fields { get; } = new HashSet<string>() { TraceParent, RequestId, TraceState, Baggage, CorrelationContext };

        //
        // Inject
        //

        public override bool Inject(Activity activity, object carrier, Action<object /* carrier */, string /* field name */, string /* value to inject */> setter)
        {
            if (activity is null || setter == default)
            {
                return false;
            }

            string? id = activity.Id;
            if (id is null)
            {
                return false;
            }

            if (activity.IdFormat == ActivityIdFormat.W3C)
            {
                setter(carrier, TraceParent, id);
                if (activity.TraceStateString is not null)
                {
                    setter(carrier, TraceState, activity.TraceStateString);
                }
            }
            else
            {
                setter(carrier, RequestId, id);
            }

            InjectBaggage(carrier, activity.Baggage, setter);

            return true;
        }

        public override bool Inject(ActivityContext context, object carrier, Action<object /* carrier */, string /* field name */, string /* value to inject */> setter) =>
                                InjectContext(context, carrier, setter);

        public override bool Inject(IEnumerable<KeyValuePair<string, string?>> baggage, object carrier, Action<object /* carrier */, string /* field name */, string /* value to inject */> setter)
        {
            if (setter is null)
            {
                return false;
            }

            if (baggage is null)
            {
                return true; // nothing need to be done
            }

            InjectBaggage(carrier, baggage, setter);
            return true;
        }

        //
        // Extract
        //

        public override bool Extract(object carrier, PropagatorGetterCallback getter, out string? id, out string? state) =>
                                LegacyExtract(carrier, getter, out id, out state);

        public override bool Extract(object carrier, PropagatorGetterCallback getter, out ActivityContext context) =>
                                LegacyExtract(carrier, getter, out context);

        public override bool Extract(object carrier, PropagatorGetterCallback getter, out IEnumerable<KeyValuePair<string, string?>>? baggage) =>
                                LegacyExtract(carrier, getter, out baggage);
    }

    internal class PassThroughPropagator : TextMapPropagator
    {
        //
        // Fields
        //
        public override IReadOnlyCollection<string> Fields { get; } = new HashSet<string>() { TraceParent, RequestId, TraceState, Baggage, CorrelationContext };

        // Inject

        public override bool Inject(Activity activity, object carrier, Action<object /* carrier */, string /* field name */, string /* value to inject */> setter)
        {
            GetRootId(out string? parentId, out string? traceState, out bool isW3c);
            if (parentId is null)
            {
                return true;
            }

            setter(carrier, isW3c ? TraceParent : RequestId, parentId);

            if (traceState is not null)
            {
                setter(carrier, TraceState, traceState);
            }

            return true;
        }

        public override bool Inject(ActivityContext context, object carrier, Action<object /* carrier */, string /* field name */, string /* value to inject */> setter) =>
            Inject((Activity)null!, carrier, setter);

        public override bool Inject(IEnumerable<KeyValuePair<string, string?>> baggage, object carrier, Action<object /* carrier */, string /* field name */, string /* value to inject */> setter)
        {
            IEnumerable<KeyValuePair<string, string?>>? parentBaggage = GetRootBaggage();

            if (parentBaggage is not null)
            {
                InjectBaggage(carrier, parentBaggage, setter);
            }

            return true;
        }

        //
        // Extract
        //

        public override bool Extract(object carrier, PropagatorGetterCallback getter, out string? id, out string? state) =>
                                LegacyExtract(carrier, getter, out id, out state);

        public override bool Extract(object carrier, PropagatorGetterCallback getter, out ActivityContext context) =>
                                LegacyExtract(carrier, getter, out context);

        public override bool Extract(object carrier, PropagatorGetterCallback getter, out IEnumerable<KeyValuePair<string, string?>>? baggage) =>
                                LegacyExtract(carrier, getter, out baggage);

        private static void GetRootId(out string? parentId, out string? traceState, out bool isW3c)
        {
            Activity? activity = Activity.Current;
            if (activity is null)
            {
                parentId = null;
                traceState = null;
                isW3c = false;
                return;
            }

            while (activity is not null && activity.Parent is not null)
            {
                activity = activity.Parent;
            }

            traceState = activity?.TraceStateString;
            parentId = activity?.ParentId ?? activity?.Id;
            isW3c = activity?.IdFormat == ActivityIdFormat.W3C;
        }

        private static IEnumerable<KeyValuePair<string, string?>>? GetRootBaggage()
        {
            Activity? activity = Activity.Current;
            if (activity is null)
            {
                return null;
            }

            while (activity is not null && activity.Parent is not null)
            {
                activity = activity.Parent;
            }

            return activity?.Baggage;
        }
    }

    internal class OutputSuppressionPropagator : TextMapPropagator
    {
        //
        // Fields
        //
        public override IReadOnlyCollection<string> Fields { get; } = new HashSet<string>() { TraceParent, RequestId, TraceState, Baggage, CorrelationContext };

        // Inject

        public override bool Inject(Activity activity, object carrier, Action<object /* carrier */, string /* field name */, string /* value to inject */> setter) => true;
        public override bool Inject(ActivityContext context, object carrier, Action<object /* carrier */, string /* field name */, string /* value to inject */> setter) => true;
        public override bool Inject(IEnumerable<KeyValuePair<string, string?>> baggage, object carrier, Action<object /* carrier */, string /* field name */, string /* value to inject */> setter) => true;

        // Extract

        public override bool Extract(object carrier, PropagatorGetterCallback getter, out string? id, out string? state) =>
                                LegacyExtract(carrier, getter, out id, out state);

        public override bool Extract(object carrier, PropagatorGetterCallback getter, out ActivityContext context) =>
                                LegacyExtract(carrier, getter, out context);

        public override bool Extract(object carrier, PropagatorGetterCallback getter, out IEnumerable<KeyValuePair<string, string?>>? baggage) =>
                                LegacyExtract(carrier, getter, out baggage);
    }

    internal class W3CPropagator : TextMapPropagator
    {
        //
        // Fields
        //

        public override IReadOnlyCollection<string> Fields { get; } = new HashSet<string>() { TraceParent, TraceState, Baggage };

        //
        // Inject
        //

        public override bool Inject(Activity activity, object carrier, Action<object /* carrier */, string /* field name */, string /* value to inject */> setter)
        {
            if (activity is null || setter == default || activity.IdFormat != ActivityIdFormat.W3C)
            {
                return false;
            }

            string? id = activity.Id;
            if (id is null)
            {
                return false;
            }

            setter(carrier, TraceParent, id);
            if (activity.TraceStateString is not null)
            {
                setter(carrier, TraceState, activity.TraceStateString);
            }

            InjectBaggage(carrier, activity.Baggage, setter, injectAsW3C: true);

            return true;
        }

        public override bool Inject(ActivityContext context, object carrier, Action<object /* carrier */, string /* field name */, string /* value to inject */> setter) =>
                                InjectContext(context, carrier, setter);

        public override bool Inject(IEnumerable<KeyValuePair<string, string?>> baggage, object carrier, Action<object /* carrier */, string /* field name */, string /* value to inject */> setter)
        {
            if (setter is null)
            {
                return false;
            }

            if (baggage is null)
            {
                return true; // nothing need to be done
            }

            InjectBaggage(carrier, baggage, setter, true);
            return true;
        }

        //
        // Extract
        //

        public override bool Extract(object carrier, PropagatorGetterCallback getter, out string? id, out string? state)
        {
            if (getter is null)
            {
                id = null;
                state = null;
                return false;
            }

            getter(carrier, TraceParent, out id);
            getter(carrier, TraceState, out state);

            if (id is not null && !ActivityContext.TryParse(id, state, out ActivityContext context))
            {
                return false;
            }

            return true;
        }

        public override bool Extract(object carrier, PropagatorGetterCallback getter, out ActivityContext context) =>
                                LegacyExtract(carrier, getter, out context);

        public override bool Extract(object carrier, PropagatorGetterCallback getter, out IEnumerable<KeyValuePair<string, string?>>? baggage)
        {
            baggage = null;
            if (getter is null)
            {
                return false;
            }

            getter(carrier, Baggage, out string? theBaggage);

            if (theBaggage is not null)
            {
                return TryExtractBaggage(theBaggage, out baggage);
            }

            return true;
        }
    }
}
