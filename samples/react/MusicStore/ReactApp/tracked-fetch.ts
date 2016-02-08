import isomorphicFetch from 'isomorphic-fetch';
import { addTask } from './domain-tasks';

export function fetch(url: string): Promise<any> {
    // TODO: Find some way to supply the base URL via domain context
    var promise = isomorphicFetch('http://localhost:5000' + url, {
        headers: {
            Connection: 'keep-alive'
        }
    });
    addTask(promise);
    return promise;
}
