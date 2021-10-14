// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

console.log("SignalR Functional Tests Loaded");

// Prereqs
import "es6-promise/dist/es6-promise.auto.js";
import "./LogBannerReporter";

// Tests
import "./ConnectionTests";
import "./HubConnectionTests";
import "./WebSocketTests";
import "./WebWorkerTests";
