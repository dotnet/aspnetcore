import * as domain from 'domain';
import * as domainContext from 'domain-context';

// Not using symbols, because this may need to run in a version of Node.js that doesn't support them
const domainTasksStateKey = '__DOMAIN_TASKS';
const domainTaskBaseUrlStateKey = '__DOMAIN_TASK_INTERNAL_FETCH_BASEURL__DO_NOT_REFERENCE_THIS__';

let noDomainBaseUrl: string;

export function addTask(task: PromiseLike<any>) {
    if (task && domain.active) {
        const state = domainContext.get(domainTasksStateKey) as DomainTasksState;
        if (state) {
            state.numRemainingTasks++;
            task.then(() => {
                // The application may have other listeners chained to this promise *after*
                // this listener, which may in turn register further tasks. Since we don't 
                // want the combined task to complete until all the handlers for child tasks
                // have finished, delay the response to give time for more tasks to be added
                // synchronously.
                setTimeout(() => {
                    state.numRemainingTasks--;
                    if (state.numRemainingTasks === 0 && !state.hasIssuedSuccessCallback) {
                        state.hasIssuedSuccessCallback = true;
                        setTimeout(() => {
                            state.completionCallback(/* error */ null);
                        }, 0);
                    }
                }, 0);
            }, (error) => {
                state.completionCallback(error);
            });
        }
    }
}

export function run<T>(codeToRun: () => T, completionCallback: (error: any) => void): T {
    let synchronousResult: T;
    domainContext.runInNewDomain(() => {
        const state: DomainTasksState = {
            numRemainingTasks: 0,
            hasIssuedSuccessCallback: false,
            completionCallback: domain.active.bind(completionCallback)
        };

        try {
            domainContext.set(domainTasksStateKey, state);
            synchronousResult = codeToRun();

            // If no tasks were registered synchronously, then we're done already
            if (state.numRemainingTasks === 0 && !state.hasIssuedSuccessCallback) {
                state.hasIssuedSuccessCallback = true;
                setTimeout(() => {
                    state.completionCallback(/* error */ null);
                }, 0);
            }
        } catch(ex) {
            state.completionCallback(ex);
        }
    });

    return synchronousResult;
}

export function baseUrl(url?: string): string {
    if (url) {
        if (domain.active) {
            // There's an active domain (e.g., in Node.js), so associate the base URL with it
            domainContext.set(domainTaskBaseUrlStateKey, url);
        } else {
            // There's no active domain (e.g., in browser), so there's just one shared base URL
            noDomainBaseUrl = url;
        }
    }

    return domain.active ? domainContext.get(domainTaskBaseUrlStateKey) : noDomainBaseUrl;
}

interface DomainTasksState {
    numRemainingTasks: number;
    hasIssuedSuccessCallback: boolean;
    completionCallback: (error: any) => void;
}
