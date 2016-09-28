import { typeName, isActionType, Action, Reducer } from 'redux-typed';
import { ActionCreator } from './';

// -----------------
// STATE - This defines the type of data maintained in the Redux store.

export interface CounterState {
    count: number;
}

// -----------------
// ACTIONS - These are serializable (hence replayable) descriptions of state transitions.
// They do not themselves have any side-effects; they just describe something that is going to happen.
// Use @typeName and isActionType for type detection that works even after serialization/deserialization.

@typeName("INCREMENT_COUNT")
class IncrementCount extends Action {
}

// ----------------
// ACTION CREATORS - These are functions exposed to UI components that will trigger a state transition.
// They don't directly mutate state, but they can have external side-effects (such as loading data).

export const actionCreators = {
    increment: (): ActionCreator => (dispatch, getState) => {
        dispatch(new IncrementCount());
    }
};

// ----------------
// REDUCER - For a given state and action, returns the new state. To support time travel, this must not mutate the old state.
export const reducer: Reducer<CounterState> = (state, action) => {
    if (isActionType(action, IncrementCount)) {
        return { count: state.count + 1 };
    }

    // For unrecognized actions (or in cases where actions have no effect), must return the existing state
    //  (or default initial state if none was supplied)
    return state || { count: 0 };
};
