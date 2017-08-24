const ECHOENDPOINT_URL = `http://${document.location.host}/echo`;

function getTransportTypes() {
    let transportTypes = [ signalR.TransportType.WebSockets ];
    if (typeof (EventSource) !== "undefined") {
        transportTypes.push(signalR.TransportType.ServerSentEvents);
    }
    transportTypes.push(signalR.TransportType.LongPolling);

    return transportTypes;
}

function eachTransport(action) {
    getTransportTypes().forEach(t => action(t));
}

function eachTransportAndProtocol(action) {
    let protocols = [
        new signalR.JsonHubProtocol(),
        new signalRMsgPack.MessagePackHubProtocol()
    ];
    getTransportTypes().forEach(t =>
        protocols.forEach(p => action(t, p)));
}