// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace AspNetCoreModule.Test.WebSocketClient
{
    public static class WebSocketConstants
    {
        public static int SMALL_LENGTH_FLAG = 126;
        public static int LARGE_LENGTH_FLAG = 127;

        public static byte SMALL_LENGTH_BYTE = 0XFE;
        public static byte LARGE_LENGTH_BYTE = 0XFF;

    }
}
