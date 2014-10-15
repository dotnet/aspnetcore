// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Http.Headers;
using System.Text;

namespace System.Net.Http.Formatting.Mocks
{
    public class MockMediaTypeFormatter : MediaTypeFormatter
    {
        private bool _canWriteAnyTypes = true;
        public bool CallBase { get; set; }
        public Func<Type, bool> CanReadTypeCallback { get; set; }
        public Func<Type, bool> CanWriteTypeCallback { get; set; }

        public bool CanWriteAnyTypesReturn
        {
            get { return _canWriteAnyTypes; }
            set { _canWriteAnyTypes = value; }
        }

        public override bool CanReadType(Type type)
        {
            if (!CallBase && CanReadTypeCallback == null)
            {
                throw new InvalidOperationException("CallBase or CanReadTypeCallback must be set first.");
            }

            return CanReadTypeCallback != null ? CanReadTypeCallback(type) : true;
        }

        public override bool CanWriteType(Type type)
        {
            if (!CallBase && CanWriteTypeCallback == null)
            {
                throw new InvalidOperationException("CallBase or CanWriteTypeCallback must be set first.");
            }

            return CanWriteTypeCallback != null ? CanWriteTypeCallback(type) : true;
        }

        public new Encoding SelectCharacterEncoding(HttpContentHeaders contentHeaders)
        {
            return base.SelectCharacterEncoding(contentHeaders);
        }
    }
}
