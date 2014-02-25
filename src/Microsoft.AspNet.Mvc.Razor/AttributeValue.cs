using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class AttributeValue
    {
        public AttributeValue(PositionTagged<string> prefix, PositionTagged<object> value, bool literal)
        {
            Prefix = prefix;
            Value = value;
            Literal = literal;
        }

        public PositionTagged<string> Prefix { get; private set; }

        public PositionTagged<object> Value { get; private set; }

        public bool Literal { get; private set; }

        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "We are using tuples here to avoid dependencies from Razor to WebPages")]
        public static AttributeValue FromTuple(Tuple<Tuple<string, int>, Tuple<object, int>, bool> value)
        {
            return new AttributeValue(value.Item1, value.Item2, value.Item3);
        }

        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "We are using tuples here to avoid dependencies from Razor to WebPages")]
        public static AttributeValue FromTuple(Tuple<Tuple<string, int>, Tuple<string, int>, bool> value)
        {
            return new AttributeValue(value.Item1, new PositionTagged<object>(value.Item2.Item1, value.Item2.Item2), value.Item3);
        }

        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "We are using tuples here to avoid dependencies from Razor to WebPages")]
        public static implicit operator AttributeValue(Tuple<Tuple<string, int>, Tuple<object, int>, bool> value)
        {
            return FromTuple(value);
        }

        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "We are using tuples here to avoid dependencies from Razor to WebPages")]
        public static implicit operator AttributeValue(Tuple<Tuple<string, int>, Tuple<string, int>, bool> value)
        {
            return FromTuple(value);
        }
    }
}
