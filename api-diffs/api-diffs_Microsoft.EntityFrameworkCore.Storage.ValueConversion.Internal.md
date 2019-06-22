# Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal

``` diff
-namespace Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal {
 {
-    public class CompositeValueConverter<TModel, TMiddle, TProvider> : ValueConverter<TModel, TProvider> {
 {
-        public CompositeValueConverter(ValueConverter converter1, ValueConverter converter2, ConverterMappingHints mappingHints = null);

-    }
-    public class StringCharConverter<TModel, TProvider> : ValueConverter<TModel, TProvider> {
 {
-        public StringCharConverter(Expression<Func<TModel, TProvider>> convertToProviderExpression, Expression<Func<TProvider, TModel>> convertFromProviderExpression, ConverterMappingHints mappingHints = null);

-        protected static Expression<Func<string, char>> ToChar();

-        protected static Expression<Func<char, string>> ToString();

-    }
-    public class StringDateTimeConverter<TModel, TProvider> : ValueConverter<TModel, TProvider> {
 {
-        protected static readonly ConverterMappingHints _defaultHints;

-        public StringDateTimeConverter(Expression<Func<TModel, TProvider>> convertToProviderExpression, Expression<Func<TProvider, TModel>> convertFromProviderExpression, ConverterMappingHints mappingHints = null);

-        protected static Expression<Func<string, DateTime>> ToDateTime();

-        protected static Expression<Func<DateTime, string>> ToString();

-    }
-    public class StringDateTimeOffsetConverter<TModel, TProvider> : ValueConverter<TModel, TProvider> {
 {
-        protected static readonly ConverterMappingHints _defaultHints;

-        public StringDateTimeOffsetConverter(Expression<Func<TModel, TProvider>> convertToProviderExpression, Expression<Func<TProvider, TModel>> convertFromProviderExpression, ConverterMappingHints mappingHints = null);

-        protected static Expression<Func<string, DateTimeOffset>> ToDateTimeOffset();

-        protected static Expression<Func<DateTimeOffset, string>> ToString();

-    }
-    public class StringEnumConverter<TModel, TProvider, TEnum> : ValueConverter<TModel, TProvider> where TEnum : struct, ValueType {
 {
-        public StringEnumConverter(Expression<Func<TModel, TProvider>> convertToProviderExpression, Expression<Func<TProvider, TModel>> convertFromProviderExpression, ConverterMappingHints mappingHints = null);

-        protected static Expression<Func<string, TEnum>> ToEnum();

-        protected static Expression<Func<TEnum, string>> ToString();

-    }
-    public class StringGuidConverter<TModel, TProvider> : ValueConverter<TModel, TProvider> {
 {
-        protected static readonly ConverterMappingHints _defaultHints;

-        public StringGuidConverter(Expression<Func<TModel, TProvider>> convertToProviderExpression, Expression<Func<TProvider, TModel>> convertFromProviderExpression, ConverterMappingHints mappingHints = null);

-        protected static Expression<Func<string, Guid>> ToGuid();

-        protected static Expression<Func<Guid, string>> ToString();

-    }
-    public class StringNumberConverter<TModel, TProvider, TNumber> : ValueConverter<TModel, TProvider> {
 {
-        protected static readonly ConverterMappingHints _defaultHints;

-        public StringNumberConverter(Expression<Func<TModel, TProvider>> convertToProviderExpression, Expression<Func<TProvider, TModel>> convertFromProviderExpression, ConverterMappingHints mappingHints = null);

-        protected static Expression<Func<string, TNumber>> ToNumber();

-        protected static Expression<Func<TNumber, string>> ToString();

-    }
-    public class StringTimeSpanConverter<TModel, TProvider> : ValueConverter<TModel, TProvider> {
 {
-        protected static readonly ConverterMappingHints _defaultHints;

-        public StringTimeSpanConverter(Expression<Func<TModel, TProvider>> convertToProviderExpression, Expression<Func<TProvider, TModel>> convertFromProviderExpression, ConverterMappingHints mappingHints = null);

-        protected static Expression<Func<TimeSpan, string>> ToString();

-        protected static Expression<Func<string, TimeSpan>> ToTimeSpan();

-    }
-}
```

