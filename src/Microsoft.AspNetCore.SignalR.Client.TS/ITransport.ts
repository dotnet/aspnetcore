interface ITransport {
    connect(url: string, queryString: string): Promise<void>;
    send(data: string): Promise<void>;
    stop(): void;
}