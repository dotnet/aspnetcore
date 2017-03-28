const ECHOENDPOINT_URL = `http://${document.location.host}/echo`;

function eachTransport(action) {
    let transportTypes = [
        signalR.TransportType.WebSockets,
        signalR.TransportType.ServerSentEvents,
        signalR.TransportType.LongPolling ];
   transportTypes.forEach(t => action(t));
}
