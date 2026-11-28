# Copyright (c) .NET Foundation. All rights reserved.
# Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

# Keep reflection-based fields and constructors for SignalR protocol messages serialized/deserialized by Gson
-keepclassmembers class com.microsoft.signalr.HandshakeRequestMessage {
    <fields>;
    <init>(...);
}

-keepclassmembers class com.microsoft.signalr.HandshakeResponseMessage {
    <fields>;
    <init>(...);
}

-keepclassmembers class * extends com.microsoft.signalr.HubMessage {
    <fields>;
    <init>(...);
}
