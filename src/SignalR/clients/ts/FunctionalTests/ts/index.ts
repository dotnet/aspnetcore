// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

console.log("SignalR Functional Tests Loaded");

// Prereqs
import "es6-promise/dist/es6-promise.auto.js";
import "./LogBannerReporter";

// Tests
import "./ConnectionTests";
import "./HubConnectionTests";
import "./WebSocketTests";
