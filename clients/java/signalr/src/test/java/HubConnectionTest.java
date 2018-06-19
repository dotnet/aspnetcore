// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import org.junit.Test;

import static org.junit.Assert.*;

public class HubConnectionTest {
    @Test
    public void testEmptyCollection() {
        HubConnection hubConnection = new HubConnection();
        assertTrue(hubConnection.methodToTest());
    }
}