// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

public interface HubProtocol {
    String getName();
    int getVersion();
    TransferFormat getTransferFormat();
    InvocationMessage[] parseMessages(String message);
    String writeMessage(InvocationMessage message);
}

