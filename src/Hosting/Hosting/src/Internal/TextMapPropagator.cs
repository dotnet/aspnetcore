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

        public static TextMapPropagator DefaultPropagator { get; set; } = CreateLegacyPropagator();

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

        internal const string TraceParent = "traceparent";
        internal const string RequestId = "Request-Id";
        internal const string TraceState = "tracestate";
        internal const string Baggage = "baggage";
        internal const string CorrelationContext = "Correlation-Context";
        internal static readonly char[] EqualSignSeparator = new[] { '=' };
        internal static readonly char[] CommaSignSeparator = new[] { ',' };
        internal const int MaxBaggageLength = 8192;
        internal const int MaxBaggageItems = 180;

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
                        baggageList.Append(WebUtility.UrlEncode(item.Key)).Append('=').Append(WebUtility.UrlEncode(item.Value)).Append(',');
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

            foreach (string pair in baggagestring.Split(CommaSignSeparator))
            {
                baggageLength += pair.Length + 1; // pair and comma

                if (baggageLength >= MaxBaggageLength || baggageList?.Count >= MaxBaggageItems)
                {
                    break;
                }

                if (pair.IndexOf('=') < 0)
                {
                    continue;
                }

                var parts = pair.Split(EqualSignSeparator, 2);
                if (parts.Length != 2)
                {
                    continue;
                }

                var key = WebUtility.UrlDecode(parts[0]);
                var value = WebUtility.UrlDecode(parts[1]);

                if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value))
                {
                    continue;
                }

                if (baggageList is null)
                {
                    baggageList = new();
                }

                baggageList.Add(new KeyValuePair<string, string?>(key, value));
            }

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
