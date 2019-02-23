importScripts('lib/signalr-webworker/signalr.js');

var connection = null;

onmessage = function (e) {
    if (connection === null) {
        connection = new signalR.HubConnectionBuilder()
            .withUrl(e.data + '/default')
            .build();

        connection.on('send', function (message) {
            postMessage('Received message: ' + message);
        });

        connection.start().then(function () {
            postMessage('connected');
        });
    } else if (connection.connectionState == signalR.HubConnectionState.Connected) {
        connection.invoke('send', e.data);
    } else {
        postMessage('Attempted to send message while disconnected.')
    }
};
