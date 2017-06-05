import { ITransport, TransportType } from "../Microsoft.AspNetCore.SignalR.Client.TS/Transports"

export function eachTransport(action: (transport: TransportType) => void) {
    let transportTypes = [
        TransportType.WebSockets,
        TransportType.ServerSentEvents,
        TransportType.LongPolling ];
    transportTypes.forEach(t => action(t));
};