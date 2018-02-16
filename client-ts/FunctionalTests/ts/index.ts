// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import "es6-promise/dist/es6-promise.auto.js";

// Load SignalR
import { getParameterByName } from "./Utils";

const minified = getParameterByName("release") === "true" ? ".min" : "";
document.write(
    '<script type="text/javascript" src="lib/signalr/signalr' + minified + '.js"><\/script>' +
    '<script type="text/javascript" src="lib/signalr/signalr-protocol-msgpack' + minified + '.js"><\/script>');

import "./ConnectionTests";
import "./HubConnectionTests";
import "./WebDriverReporter";
import "./WebSocketTests";
