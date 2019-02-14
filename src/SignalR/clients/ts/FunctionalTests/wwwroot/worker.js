importScripts('lib/signalr-webworker/signalr.js');

var connection = null;

onmessage = function (e) {
    if (connection === null) {
        postMessage('initialized');

        connection = new signalR.HubConnectionBuilder()
            .withUrl(e.data + '/testhub')
            .build();

        connection.on('message', function (message) {
            postMessage('Received message: ' + message);
        });

        connection.start().then(function () {
            postMessage('connected');
        });
    } else if (connection.connectionState == signalR.HubConnectionState.Connected) {
        connection.invoke('invokeWithString', e.data);
    } else {
        postMessage('Attempted to send message while disconnected.')
    }
};
