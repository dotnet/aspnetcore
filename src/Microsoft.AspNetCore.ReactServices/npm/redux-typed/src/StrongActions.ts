// Credits for the type detection technique: http://www.bluewire-technologies.com/2015/redux-actions-for-typescript/
import * as React from 'react';
import { Dispatch } from 'redux';
import './ObjectAssignPolyfill';

export interface ActionClass<T extends Action> {
    prototype: T;
}

export function typeName(name: string) {
    return function<T extends Action>(actionClass: ActionClass<T>) {
        // Although we could determine the type name using actionClass.prototype.constructor.name,
        // it's dangerous to do that because minifiers may interfere with it, and then serialized state
        // might not have the expected meaning after a recompile. So we explicitly ask for a name string.
        actionClass.prototype.type = name;
    }
}

export function isActionType<T extends Action>(action: Action, actionClass: ActionClass<T>): action is T {
    return action.type == actionClass.prototype.type;
}

// Middleware for transforming Typed Actions into plain actions
export const typedToPlain = (store: any) => (next: any) => (action: any) => {
    next((<any>Object).assign({}, action));
};

export abstract class Action {
    type: string;
    constructor() {
        // Make it an own-property (not a prototype property) so that it's included when JSON-serializing
        this.type = this.type;
    }
}

export interface Reducer<TState> extends Function {
    (state: TState, action: Action): TState;
}

export interface ActionCreatorGeneric<TState> extends Function {
    (dispatch: Dispatch, getState: () => TState): any;
}
