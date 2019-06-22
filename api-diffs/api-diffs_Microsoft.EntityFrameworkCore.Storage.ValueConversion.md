# Microsoft.EntityFrameworkCore.Storage.ValueConversion

``` diff
-namespace Microsoft.EntityFrameworkCore.Storage.ValueConversion {
 {
-    public class BoolToStringConverter : BoolToTwoValuesConverter<string> {
 {
-        public BoolToStringConverter(string falseValue, string trueValue, ConverterMappingHints mappingHints = null);

-        public static ValueConverterInfo DefaultInfo { get; }

-    }
-    public class BoolToTwoValuesConverter<TProvider> : ValueConverter<bool, TProvider> {
 {
-        public BoolToTwoValuesConverter(TProvider falseValue, TProvider trueValue, Expression<Func<TProvider, bool>> fromProvider = null, ConverterMappingHints mappingHints = null);

-    }
-    public class BoolToZeroOneConverter<TProvider> : BoolToTwoValuesConverter<TProvider> {
 {
-        public BoolToZeroOneConverter(ConverterMappingHints mappingHints = null);

-        public static ValueConverterInfo DefaultInfo { get; }

-    }
-    public class BytesToStringConverter : ValueConverter<byte[], string> {
 {
-        public BytesToStringConverter(ConverterMappingHints mappingHints = null);

-        public static ValueConverterInfo DefaultInfo { get; }

-    }
-    public class CastingConverter<TModel, TProvider> : ValueConverter<TModel, TProvider> {
 {
-        public CastingConverter(ConverterMappingHints mappingHints = null);

-        public static ValueConverterInfo DefaultInfo { get; }

-    }
-    public class CharToStringConverter : StringCharConverter<char, string> {
 {
-        public CharToStringConverter(ConverterMappingHints mappingHints = null);

-        public static ValueConverterInfo DefaultInfo { get; }

-    }
-    public class ConverterMappingHints {
 {
-        public ConverterMappingHints(Nullable<int> size = default(Nullable<int>), Nullable<int> precision = default(Nullable<int>), Nullable<int> scale = default(Nullable<int>), Nullable<bool> unicode = default(Nullable<bool>), Func<IProperty, IEntityType, ValueGenerator> valueGeneratorFactory = null);

-        public virtual Nullable<bool> IsUnicode { get; }

-        public virtual Nullable<int> Precision { get; }

-        public virtual Nullable<int> Scale { get; }

-        public virtual Nullable<int> Size { get; }

-        public virtual Func<IProperty, IEntityType, ValueGenerator> ValueGeneratorFactory { get; }

-        public virtual ConverterMappingHints With(ConverterMappingHints hints);

-    }
-    public class DateTimeOffsetToBinaryConverter : ValueConverter<DateTimeOffset, long> {
 {
-        public DateTimeOffsetToBinaryConverter(ConverterMappingHints mappingHints = null);

-        public static ValueConverterInfo DefaultInfo { get; }

-    }
-    public class DateTimeOffsetToBytesConverter : ValueConverter<DateTimeOffset, byte[]> {
 {
-        public DateTimeOffsetToBytesConverter(ConverterMappingHints mappingHints = null);

-        public static ValueConverterInfo DefaultInfo { get; }

-    }
-    public class DateTimeOffsetToStringConverter : StringDateTimeOffsetConverter<DateTimeOffset, string> {
 {
-        public DateTimeOffsetToStringConverter(ConverterMappingHints mappingHints = null);

-        public static ValueConverterInfo DefaultInfo { get; }

-    }
-    public class DateTimeToBinaryConverter : ValueConverter<DateTime, long> {
 {
-        public DateTimeToBinaryConverter(ConverterMappingHints mappingHints = null);

-        public static ValueConverterInfo DefaultInfo { get; }

-    }
-    public class DateTimeToStringConverter : StringDateTimeConverter<DateTime, string> {
 {
-        public DateTimeToStringConverter(ConverterMappingHints mappingHints = null);

-        public static ValueConverterInfo DefaultInfo { get; }

-    }
-    public class DateTimeToTicksConverter : ValueConverter<DateTime, long> {
 {
-        public DateTimeToTicksConverter(ConverterMappingHints mappingHints = null);

-    }
-    public class EnumToNumberConverter<TEnum, TNumber> : ValueConverter<TEnum, TNumber> where TEnum : struct, ValueType where TNumber : struct, ValueType {
 {
-        public EnumToNumberConverter(ConverterMappingHints mappingHints = null);

-        public static ValueConverterInfo DefaultInfo { get; }

-    }
-    public class EnumToStringConverter<TEnum> : StringEnumConverter<TEnum, string, TEnum> where TEnum : struct, ValueType {
 {
-        public EnumToStringConverter(ConverterMappingHints mappingHints = null);

-        public static ValueConverterInfo DefaultInfo { get; }

-    }
-    public class GuidToBytesConverter : ValueConverter<Guid, byte[]> {
 {
-        public GuidToBytesConverter(ConverterMappingHints mappingHints = null);

-        public static ValueConverterInfo DefaultInfo { get; }

-    }
-    public class GuidToStringConverter : StringGuidConverter<Guid, string> {
 {
-        public GuidToStringConverter(ConverterMappingHints mappingHints = null);

-        public static ValueConverterInfo DefaultInfo { get; }

-    }
-    public interface IValueConverterSelector {
 {
-        IEnumerable<ValueConverterInfo> Select(Type modelClrType, Type providerClrType = null);

-    }
-    public class NumberToBytesConverter<TNumber> : ValueConverter<TNumber, byte[]> {
 {
-        public NumberToBytesConverter(ConverterMappingHints mappingHints = null);

-        public static ValueConverterInfo DefaultInfo { get; }

-    }
-    public class NumberToStringConverter<TNumber> : StringNumberConverter<TNumber, string, TNumber> {
 {
-        public NumberToStringConverter(ConverterMappingHints mappingHints = null);

-        public static ValueConverterInfo DefaultInfo { get; }

-    }
-    public class RelationalConverterMappingHints : ConverterMappingHints {
 {
-        public RelationalConverterMappingHints(Nullable<int> size = default(Nullable<int>), Nullable<int> precision = default(Nullable<int>), Nullable<int> scale = default(Nullable<int>), Nullable<bool> unicode = default(Nullable<bool>), Nullable<bool> fixedLength = default(Nullable<bool>), Func<IProperty, IEntityType, ValueGenerator> valueGeneratorFactory = null);

-        public virtual Nullable<bool> IsFixedLength { get; }

-        public override ConverterMappingHints With(ConverterMappingHints hints);

-    }
-    public class StringToBoolConverter : ValueConverter<string, bool> {
 {
-        public StringToBoolConverter(ConverterMappingHints mappingHints = null);

-        public static ValueConverterInfo DefaultInfo { get; }

-    }
-    public class StringToBytesConverter : ValueConverter<string, byte[]> {
 {
-        public StringToBytesConverter(Encoding encoding, ConverterMappingHints mappingHints = null);

-        public static ValueConverterInfo DefaultInfo { get; }

-    }
-    public class StringToCharConverter : StringCharConverter<string, char> {
 {
-        public StringToCharConverter(ConverterMappingHints mappingHints = null);

-        public static ValueConverterInfo DefaultInfo { get; }

-    }
-    public class StringToDateTimeConverter : StringDateTimeConverter<string, DateTime> {
 {
-        public StringToDateTimeConverter(ConverterMappingHints mappingHints = null);

-        public static ValueConverterInfo DefaultInfo { get; }

-    }
-    public class StringToDateTimeOffsetConverter : StringDateTimeOffsetConverter<string, DateTimeOffset> {
 {
-        public StringToDateTimeOffsetConverter(ConverterMappingHints mappingHints = null);

-        public static ValueConverterInfo DefaultInfo { get; }

-    }
-    public class StringToEnumConverter<TEnum> : StringEnumConverter<string, TEnum, TEnum> where TEnum : struct, ValueType {
 {
-        public StringToEnumConverter(ConverterMappingHints mappingHints = null);

-        public static ValueConverterInfo DefaultInfo { get; }

-    }
-    public class StringToGuidConverter : StringGuidConverter<string, Guid> {
 {
-        public StringToGuidConverter(ConverterMappingHints mappingHints = null);

-        public static ValueConverterInfo DefaultInfo { get; }

-    }
-    public class StringToNumberConverter<TNumber> : StringNumberConverter<string, TNumber, TNumber> {
 {
-        public StringToNumberConverter(ConverterMappingHints mappingHints = null);

-        public static ValueConverterInfo DefaultInfo { get; }

-    }
-    public class StringToTimeSpanConverter : StringTimeSpanConverter<string, TimeSpan> {
 {
-        public StringToTimeSpanConverter(ConverterMappingHints mappingHints = null);

-        public static ValueConverterInfo DefaultInfo { get; }

-    }
-    public class TimeSpanToStringConverter : StringTimeSpanConverter<TimeSpan, string> {
 {
-        public TimeSpanToStringConverter(ConverterMappingHints mappingHints = null);

-        public static ValueConverterInfo DefaultInfo { get; }

-    }
-    public class TimeSpanToTicksConverter : ValueConverter<TimeSpan, long> {
 {
-        public TimeSpanToTicksConverter(ConverterMappingHints mappingHints = null);

-        public static ValueConverterInfo DefaultInfo { get; }

-    }
-    public abstract class ValueConverter {
 {
-        protected ValueConverter(LambdaExpression convertToProviderExpression, LambdaExpression convertFromProviderExpression, ConverterMappingHints mappingHints = null);

-        public abstract Func<object, object> ConvertFromProvider { get; }

-        public virtual LambdaExpression ConvertFromProviderExpression { get; }

-        public abstract Func<object, object> ConvertToProvider { get; }

-        public virtual LambdaExpression ConvertToProviderExpression { get; }

-        public virtual ConverterMappingHints MappingHints { get; }

-        public abstract Type ModelClrType { get; }

-        public abstract Type ProviderClrType { get; }

-        protected static Type CheckTypeSupported(Type type, Type converterType, params Type[] supportedTypes);

-        public virtual ValueConverter ComposeWith(ValueConverter secondConverter);

-    }
-    public class ValueConverter<TModel, TProvider> : ValueConverter {
 {
-        public ValueConverter(Expression<Func<TModel, TProvider>> convertToProviderExpression, Expression<Func<TProvider, TModel>> convertFromProviderExpression, ConverterMappingHints mappingHints = null);

-        public override Func<object, object> ConvertFromProvider { get; }

-        public virtual new Expression<Func<TProvider, TModel>> ConvertFromProviderExpression { get; }

-        public override Func<object, object> ConvertToProvider { get; }

-        public virtual new Expression<Func<TModel, TProvider>> ConvertToProviderExpression { get; }

-        public override Type ModelClrType { get; }

-        public override Type ProviderClrType { get; }

-    }
-    public readonly struct ValueConverterInfo {
 {
-        public ValueConverterInfo(Type modelClrType, Type providerClrType, Func<ValueConverterInfo, ValueConverter> factory, ConverterMappingHints mappingHints = null);

-        public ConverterMappingHints MappingHints { get; }

-        public Type ModelClrType { get; }

-        public Type ProviderClrType { get; }

-        public ValueConverter Create();

-    }
-    public class ValueConverterSelector : IValueConverterSelector {
 {
-        public ValueConverterSelector(ValueConverterSelectorDependencies dependencies);

-        protected virtual ValueConverterSelectorDependencies Dependencies { get; }

-        public virtual IEnumerable<ValueConverterInfo> Select(Type modelClrType, Type providerClrType = null);

-    }
-    public sealed class ValueConverterSelectorDependencies {
 {
-        public ValueConverterSelectorDependencies();

-    }
-}
```

