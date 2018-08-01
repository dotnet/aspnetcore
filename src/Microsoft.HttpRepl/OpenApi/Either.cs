// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.HttpRepl.OpenApi
{
    public class Either<TOption1, TOption2>
    {
        public Either(TOption1 option1)
        {
            Option1 = option1;
            IsOption1 = true;
        }

        public Either(TOption2 option2)
        {
            Option2 = option2;
            IsOption1 = false;
        }

        public bool IsOption1 { get; }

        public TOption1 Option1 { get; }

        public TOption2 Option2 { get; }

        public static implicit operator Either<TOption1, TOption2>(TOption1 value)
        {
            return new Either<TOption1, TOption2>(value);
        }

        public static implicit operator Either<TOption1, TOption2>(TOption2 value)
        {
            return new Either<TOption1, TOption2>(value);
        }
    }
}
