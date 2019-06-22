# Microsoft.EntityFrameworkCore.ValueGeneration.Internal

``` diff
-namespace Microsoft.EntityFrameworkCore.ValueGeneration.Internal {
 {
-    public class BinaryValueGenerator : ValueGenerator<byte[]> {
 {
-        public BinaryValueGenerator(bool generateTemporaryValues);

-        public override bool GeneratesTemporaryValues { get; }

-        public override byte[] Next(EntityEntry entry);

-    }
-    public class DiscriminatorValueGenerator : ValueGenerator {
 {
-        public DiscriminatorValueGenerator(object discriminator);

-        public override bool GeneratesTemporaryValues { get; }

-        protected override object NextValue(EntityEntry entry);

-    }
-    public class StringValueGenerator : ValueGenerator<string> {
 {
-        public StringValueGenerator(bool generateTemporaryValues);

-        public override bool GeneratesTemporaryValues { get; }

-        public override string Next(EntityEntry entry);

-    }
-    public class TemporaryByteValueGenerator : TemporaryNumberValueGenerator<byte> {
 {
-        public TemporaryByteValueGenerator();

-        public override byte Next(EntityEntry entry);

-    }
-    public class TemporaryCharValueGenerator : TemporaryNumberValueGenerator<char> {
 {
-        public TemporaryCharValueGenerator();

-        public override char Next(EntityEntry entry);

-    }
-    public class TemporaryDateTimeOffsetValueGenerator : ValueGenerator<DateTimeOffset> {
 {
-        public TemporaryDateTimeOffsetValueGenerator();

-        public override bool GeneratesTemporaryValues { get; }

-        public override DateTimeOffset Next(EntityEntry entry);

-    }
-    public class TemporaryDateTimeValueGenerator : ValueGenerator<DateTime> {
 {
-        public TemporaryDateTimeValueGenerator();

-        public override bool GeneratesTemporaryValues { get; }

-        public override DateTime Next(EntityEntry entry);

-    }
-    public class TemporaryDecimalValueGenerator : TemporaryNumberValueGenerator<Decimal> {
 {
-        public TemporaryDecimalValueGenerator();

-        public override Decimal Next(EntityEntry entry);

-    }
-    public class TemporaryDoubleValueGenerator : TemporaryNumberValueGenerator<double> {
 {
-        public TemporaryDoubleValueGenerator();

-        public override double Next(EntityEntry entry);

-    }
-    public class TemporaryFloatValueGenerator : TemporaryNumberValueGenerator<float> {
 {
-        public TemporaryFloatValueGenerator();

-        public override float Next(EntityEntry entry);

-    }
-    public class TemporaryIntValueGenerator : TemporaryNumberValueGenerator<int> {
 {
-        public TemporaryIntValueGenerator();

-        public override int Next(EntityEntry entry);

-    }
-    public class TemporaryLongValueGenerator : TemporaryNumberValueGenerator<long> {
 {
-        public TemporaryLongValueGenerator();

-        public override long Next(EntityEntry entry);

-    }
-    public abstract class TemporaryNumberValueGenerator<TValue> : ValueGenerator<TValue> {
 {
-        protected TemporaryNumberValueGenerator();

-        public override bool GeneratesTemporaryValues { get; }

-    }
-    public class TemporaryNumberValueGeneratorFactory : ValueGeneratorFactory {
 {
-        public TemporaryNumberValueGeneratorFactory();

-        public override ValueGenerator Create(IProperty property);

-    }
-    public class TemporarySByteValueGenerator : TemporaryNumberValueGenerator<sbyte> {
 {
-        public TemporarySByteValueGenerator();

-        public override sbyte Next(EntityEntry entry);

-    }
-    public class TemporaryShortValueGenerator : TemporaryNumberValueGenerator<short> {
 {
-        public TemporaryShortValueGenerator();

-        public override short Next(EntityEntry entry);

-    }
-    public class TemporaryUIntValueGenerator : TemporaryNumberValueGenerator<uint> {
 {
-        public TemporaryUIntValueGenerator();

-        public override uint Next(EntityEntry entry);

-    }
-    public class TemporaryULongValueGenerator : TemporaryNumberValueGenerator<ulong> {
 {
-        public TemporaryULongValueGenerator();

-        public override ulong Next(EntityEntry entry);

-    }
-    public class TemporaryUShortValueGenerator : TemporaryNumberValueGenerator<ushort> {
 {
-        public TemporaryUShortValueGenerator();

-        public override ushort Next(EntityEntry entry);

-    }
-}
```

