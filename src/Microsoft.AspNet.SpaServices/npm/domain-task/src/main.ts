import * as domain from 'domain';
import * as domainContext from 'domain-context';
const domainTasksStateKey = '__DOMAIN_TASKS';

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

interface DomainTasksState {
    numRemainingTasks: number;
    hasIssuedSuccessCallback: boolean;
    completionCallback: (error: any) => void;
}
