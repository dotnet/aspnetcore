export interface CircuitHandler {
    /** Invoked when a server connection is established or re-established after a connection failure.
     */
    onConnectionUp?() : void;

    /** Invoked when a server connection is dropped.
     * @param {Error} error Optionally argument containing the error that caused the connection to close (if any).
     */
    onConnectionDown?(error?: Error): void;
}
