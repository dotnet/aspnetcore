// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Server.Kestrel.Networking
{
    public class UvLoopHandle : UvHandle
    {
        public void Init(Libuv uv)
        {
            CreateHandle(uv, uv.loop_size());
            _uv.loop_init(this);
        }

        public int Run(int mode = 0)
        {
            return _uv.run(this, mode);
        }

        public void Stop()
        {
            _uv.stop(this);
        }

        protected override bool ReleaseHandle()
        {
            var memory = this.handle;
            if (memory != IntPtr.Zero)
            {
                _uv.loop_close(this);
                handle = IntPtr.Zero;
                DestroyHandle(memory);
            }
            return true;
        }

    }
}
