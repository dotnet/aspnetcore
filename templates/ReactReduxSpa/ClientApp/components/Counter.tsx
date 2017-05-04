import * as React from 'react';
import { Link } from 'react-router';
import { connect } from 'react-redux';
import { ApplicationState }  from '../store';
import * as CounterStore from '../store/Counter';
import * as WeatherForecasts from '../store/WeatherForecasts';

type CounterProps = CounterStore.CounterState & typeof CounterStore.actionCreators;

class Counter extends React.Component<CounterProps, {}> {
    public render() {
        return <div>
            <h1>Counter</h1>

            <p>This is a simple example of a React component.</p>

            <p>Current count: <strong>{ this.props.count }</strong></p>

            <button onClick={ () => { this.props.increment() } }>Increment</button>
        </div>;
    }
}

// Wire up the React component to the Redux store
export default connect(
    (state: ApplicationState) => state.counter, // Selects which state properties are merged into the component's props
    CounterStore.actionCreators                 // Selects which action creators are merged into the component's props
)(Counter);
