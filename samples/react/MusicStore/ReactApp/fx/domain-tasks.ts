const domain = require('domain') as any;
const domainContext = require('domain-context') as any;
const domainTasksStateKey = '__DOMAIN_TASKS';

export function addTask(task: PromiseLike<any>) {
    if (task && domain.active) {
        const state = domainContext.get(domainTasksStateKey) as DomainTasksState;
        if (state) {
            state.numRemainingTasks++;
            task.then(() => {
                // The application may have other listeners chained to this promise *after*
                // this listener. Since we don't want the combined task to complete until
                // all the handlers for child tasks have finished, delay the following by
                // one tick.
                setTimeout(() => {
                    state.numRemainingTasks--;
                    if (state.numRemainingTasks === 0) {
                        state.triggerResolved();
                    }                    
                }, 0);
            }, state.triggerRejected);
        }
    }
}

export function run(codeToRun: () => void): Promise<void> {
    return new Promise((resolve, reject) => {
        domainContext.runInNewDomain(() => {
            const state: DomainTasksState = {
                numRemainingTasks: 0,
                triggerResolved: resolve,
                triggerRejected: reject
            };
            domainContext.set(domainTasksStateKey, state);
            codeToRun();
            
            // If no tasks were registered synchronously, then we're done already
            if (state.numRemainingTasks === 0) {
                resolve();
            }
        });
    }) as any as Promise<void>;
}

interface DomainTasksState {
    numRemainingTasks: number;
    triggerResolved: () => void;
    triggerRejected: () => void;
}
