import { fetch, addTask } from 'domain-task';
import { typeName, isActionType, Action, Reducer } from 'redux-typed';
import { ActionCreator } from './';

// -----------------
// STATE - This defines the type of data maintained in the Redux store.

export interface WeatherForecastsState {
    isLoading: boolean;
    startDateIndex: number;
    forecasts: WeatherForecast[];
}

export interface WeatherForecast {
    dateFormatted: string;
    temperatureC: number;
    temperatureF: number;
    summary: string;
}

// -----------------
// ACTIONS - These are serializable (hence replayable) descriptions of state transitions.
// They do not themselves have any side-effects; they just describe something that is going to happen.
// Use @typeName and isActionType for type detection that works even after serialization/deserialization.

@typeName("REQUEST_WEATHER_FORECASTS")
class RequestWeatherForecasts extends Action {
    constructor(public startDateIndex: number) {
        super();
    }
}

@typeName("RECEIVE_WEATHER_FORECASTS")
class ReceiveWeatherForecasts extends Action {
    constructor(public startDateIndex: number, public forecasts: WeatherForecast[]) {
        super();
    }
}

// ----------------
// ACTION CREATORS - These are functions exposed to UI components that will trigger a state transition.
// They don't directly mutate state, but they can have external side-effects (such as loading data).

export const actionCreators = {
    requestWeatherForecasts: (startDateIndex: number): ActionCreator => (dispatch, getState) => {
        // Only load data if it's something we don't already have (and are not already loading)
        if (startDateIndex !== getState().weatherForecasts.startDateIndex) {
            let fetchTask = fetch(`/api/SampleData/WeatherForecasts?startDateIndex=${ startDateIndex }`)
                .then(response => response.json())
                .then((data: WeatherForecast[]) => {
                    dispatch(new ReceiveWeatherForecasts(startDateIndex, data));
                });

            addTask(fetchTask); // Ensure server-side prerendering waits for this to complete
            dispatch(new RequestWeatherForecasts(startDateIndex));
        }
    }
};

// ----------------
// REDUCER - For a given state and action, returns the new state. To support time travel, this must not mutate the old state.
const unloadedState: WeatherForecastsState = { startDateIndex: null, forecasts: [], isLoading: false };
export const reducer: Reducer<WeatherForecastsState> = (state, action) => {
    if (isActionType(action, RequestWeatherForecasts)) {
        return { startDateIndex: action.startDateIndex, isLoading: true, forecasts: state.forecasts };
    } else if (isActionType(action, ReceiveWeatherForecasts)) {
        // Only accept the incoming data if it matches the most recent request. This ensures we correctly
        // handle out-of-order responses.
        if (action.startDateIndex === state.startDateIndex) {
            return { startDateIndex: action.startDateIndex, forecasts: action.forecasts, isLoading: false };
        }
    }
    
    // For unrecognized actions (or in cases where actions have no effect), must return the existing state
    // (or default initial state if none was supplied)
    return state || unloadedState;
};
