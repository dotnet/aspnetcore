export interface IHttpClient {
    get(url: string, headers?: Map<string, string>): Promise<string>;
    post(url: string, content: string, headers?: Map<string, string>): Promise<string>;
}

export class HttpClient implements IHttpClient {
    get(url: string, headers?: Map<string, string>): Promise<string> {
        return this.xhr("GET", url, headers);
    }

    post(url: string, content: string, headers?: Map<string, string>): Promise<string> {
        return this.xhr("POST", url, headers, content);
    }

    private xhr(method: string, url: string, headers?: Map<string, string>, content?: string): Promise<string> {
        return new Promise<string>((resolve, reject) => {
            let xhr = new XMLHttpRequest();

            xhr.open(method, url, true);

            if (headers) {
                headers.forEach((value, header) => xhr.setRequestHeader(header, value));
            }

            xhr.send(content);
            xhr.onload = () => {
                if (xhr.status >= 200 && xhr.status < 300) {
                    resolve(xhr.response);
                }
                else {
                    reject({
                        status: xhr.status,
                        statusText: xhr.statusText
                    });
                }
            };

            xhr.onerror = () => {
                reject({
                    status: xhr.status,
                    statusText: xhr.statusText
                });
            };
        });
    }
}