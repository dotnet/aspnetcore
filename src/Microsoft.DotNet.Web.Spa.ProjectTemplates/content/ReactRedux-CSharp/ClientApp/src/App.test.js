import React from 'react';
import ReactDOM from 'react-dom';
import { Provider } from 'react-redux';
import { MemoryRouter } from 'react-router-dom';
import configureStore from 'redux-mock-store';
import App from './App';

it('renders without crashing', () => {
  const mockStore = configureStore([])({});

  const div = document.createElement('div');
  ReactDOM.render(
    <Provider store={mockStore}>
      <MemoryRouter>
        <App />
      </MemoryRouter>
    </Provider>, div);
});
