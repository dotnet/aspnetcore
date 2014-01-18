// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;

namespace Microsoft.TestCommon
{
    public abstract class EnumHelperTestBase<TEnum> where TEnum : IComparable, IFormattable, IConvertible
    {
        private Func<TEnum, bool> _isDefined;
        private Action<TEnum, string> _validate;
        private TEnum _undefined;

        /// <summary>
        /// Helper to verify that we validate enums correctly when passed as arguments etc.
        /// </summary>
        /// <param name="isDefined">A Func used to validate that a value is defined.</param>
        /// <param name="validate">A Func used to validate that a value is definded of throw an exception.</param>
        /// <param name="undefined">An undefined value.</param>
        protected EnumHelperTestBase(Func<TEnum, bool> isDefined, Action<TEnum, string> validate, TEnum undefined)
        {
            _isDefined = isDefined;
            _validate = validate;
            _undefined = undefined;
        }

        [Fact]
        public void IsDefined_ReturnsTrueForDefinedValues()
        {
            Array values = Enum.GetValues(typeof(TEnum));
            foreach (object value in values)
            {
                if (ValueExistsForFramework((TEnum)value))
                {
                    Assert.True(_isDefined((TEnum)value));
                }
                else
                {
                    Assert.False(_isDefined((TEnum)value));
                }
            }
        }

        [Fact]
        public void IsDefined_ReturnsFalseForUndefinedValues()
        {
            Assert.False(_isDefined(_undefined));
        }

        [Fact]
        public void Validate_DoesNotThrowForDefinedValues()
        {
            Array values = Enum.GetValues(typeof(TEnum));
            foreach (object value in values)
            {
                if (ValueExistsForFramework((TEnum)value))
                {
                    _validate((TEnum)value, "parameter");
                }
            }
        }

        [Fact]
        public void Validate_ThrowsForUndefinedValues()
        {
            AssertForUndefinedValue(
                () => _validate(_undefined, "parameter"),
                "parameter",
                (int)Convert.ChangeType(_undefined, typeof(int)),
                typeof(TEnum),
                allowDerivedExceptions: false);
        }

        /// <summary>
        /// Override this if InvalidEnumArgument is not supported in the targetted platform
        /// </summary>
        /// <param name="testCode">A delegate to the code to be tested</param>
        /// <param name="paramName">The name of the parameter that should throw the exception</param>
        /// <param name="invalidValue">The expected invalid value that should appear in the message</param>
        /// <param name="enumType">The type of the enumeration</param>
        /// <param name="allowDerivedExceptions">Pass true to allow exceptions which derive from TException; pass false, otherwise</param>
        protected virtual void AssertForUndefinedValue(Action testCode, string parameterName, int invalidValue, Type enumType, bool allowDerivedExceptions = false)
        {
            Assert.ThrowsInvalidEnumArgument(
                testCode,
                parameterName,
                invalidValue,
                enumType,
                allowDerivedExceptions);
        }

        /// <summary>
        /// Override this to determine if a given enum value for an enum exists in a given framework
        /// </summary>
        /// <param name="value"></param>
        /// <returns>Wheter the value exists</returns>
        protected virtual bool ValueExistsForFramework(TEnum value)
        {
            return true;
        }
    }
}
