import * as portastic from 'portastic';
const pollInterval = 500;

export function waitUntilPortState(port: number, isOpen: boolean, timeoutMs: number, callback: (err: any) => void) {
    if (!(timeoutMs > 0)) {
        throw new Error(`Timed out after ${ timeoutMs }ms waiting for port ${ port } to become ${ isOpen ? 'free' : 'in use' }`);
    }

    portastic.test(port).then(
        actualIsOpenState => {
            if (actualIsOpenState === isOpen) {
                // Desired state is reached
                callback(null);
            } else {
                // Wait longer
                setTimeout(() => {
                    waitUntilPortState(port, isOpen, timeoutMs - pollInterval, callback);
                }, pollInterval);
            }
        },
        callback
    )
}
