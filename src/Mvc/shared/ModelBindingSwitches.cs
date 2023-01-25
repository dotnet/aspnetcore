// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    internal static class ModelBindingSwitches
    {
        private const int DefaultMaxRecursionDepth = 32;
        private const int DefaultMaxCollectionSize = 1024;

        internal const string MaxCollectionSize_ConfigKeyName = "Microsoft.AspNetCore.Mvc.ModelBinding.MaxCollectionSize";
        internal const string MaxRecursionDepth_ConfigKeyName = "Microsoft.AspNetCore.Mvc.ModelBinding.MaxRecursionDepth";
        internal const string MaxValidationDepth_ConfigKeyName = "Microsoft.AspNetCore.Mvc.ModelBinding.MaxValidationDepth";

        internal const string MaxModelStateValidationDepth_ConfigKeyName = "Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary.MaxValidationDepth";
        internal const string MaxStateDepth_ConfigKeyName = "Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary.MaxStateDepth";

        private static int? _maxModelStateValidationDepth;
        private static int? _maxValidationDepth;
        private static int? _maxStateDepth;
        private static int? _maxRecursionDepth;
        private static int? _maxCollectionSize;

        public static int MaxValidationDepth
        {
            get
            {
                if (!_maxValidationDepth.HasValue)
                {
                    var validationDepth = AppContext.GetData(MaxValidationDepth_ConfigKeyName);

                    _maxValidationDepth = (validationDepth is int validationDepthInt && validationDepthInt > 0) ?
                        validationDepthInt :
                        DefaultMaxRecursionDepth;
                }

                return _maxValidationDepth.Value;
            }
        }

        public static int MaxRecursionDepth
        {
            get
            {
                if (!_maxRecursionDepth.HasValue)
                {
                    var recursionDepth = AppContext.GetData(MaxRecursionDepth_ConfigKeyName);

                    _maxRecursionDepth = (recursionDepth is int recursionDepthInt && recursionDepthInt > 0) ?
                        recursionDepthInt :
                        DefaultMaxRecursionDepth;
                }

                return _maxRecursionDepth.Value;
            }
        }

        public static int MaxCollectionSize
        {
            get
            {
                if (!_maxCollectionSize.HasValue)
                {
                    var collectionSize = AppContext.GetData(MaxCollectionSize_ConfigKeyName);

                    _maxCollectionSize = (collectionSize is int collectionSizeInt && collectionSizeInt > 0) ?
                        collectionSizeInt :
                        DefaultMaxCollectionSize;
                }

                return _maxCollectionSize.Value;
            }
        }

        // Switches ModelStateDictionary-specific

        public static int MaxStateDepth
        {
            get
            {
                if (!_maxStateDepth.HasValue)
                {
                    var stateDepth = AppContext.GetData(MaxStateDepth_ConfigKeyName);

                    _maxStateDepth = (stateDepth is int stateDepthInt && stateDepthInt > 0) ?
                        stateDepthInt :
                        // Fallback to the general Recursion Depth switch
                        MaxRecursionDepth;
                }

                return _maxStateDepth.Value;
            }
        }

        public static int MaxModelStateValidationDepth
        {
            get
            {
                if (!_maxModelStateValidationDepth.HasValue)
                {
                    var validationDepth = AppContext.GetData(MaxModelStateValidationDepth_ConfigKeyName);

                    _maxModelStateValidationDepth = (validationDepth is int validationDepthInt && validationDepthInt > 0) ?
                        validationDepthInt :
                        // Fallback to the general Validation switch
                        MaxValidationDepth;
                }

                return _maxModelStateValidationDepth.Value;
            }
        }
    }
}
