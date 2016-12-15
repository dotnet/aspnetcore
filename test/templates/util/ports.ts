import * as portastic from 'portastic';
const pollInterval = 500;

export function waitUntilPortState(port: number, iface: string, isListening: boolean, timeoutMs: number, callback: (err: any) => void) {
    if (!(timeoutMs > 0)) {
        throw new Error(`Timed out waiting for port ${ port } to become ${ isListening ? 'in use' : 'free' }`);
    }

    portastic.test(port, iface).then(
        actuallyIsAvailable => {
            const actuallyIsListening = !actuallyIsAvailable;
            if (actuallyIsListening === isListening) {
                // Desired state is reached
                callback(null);
            } else {
                // Wait longer
                setTimeout(() => {
                    waitUntilPortState(port, iface, isListening, timeoutMs - pollInterval, callback);
                }, pollInterval);
            }
        },
        callback
    )
}
