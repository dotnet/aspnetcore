import React, { Component } from 'react';
////#if (IndividualLocalAuth)
import authService from './api-authorization/AuthorizeService'
////#endif

export class FetchData extends Component {
  static displayName = FetchData.name;

  constructor(props) {
    super(props);
    this.state = { forecasts: [], loading: true };

    ////#if (IndividualLocalAuth)
    authService.getAccessToken()
      .then(token =>
        fetch('api/SampleData/WeatherForecasts', {
          headers: FetchData.getHeaders(token)
        }))
      .then(response => response.json())
      .then(data => {
        this.setState({ forecasts: data, loading: false });
      });
    ////#else
    fetch('api/SampleData/WeatherForecasts')
      .then(response => response.json())
      .then(data => {
        this.setState({ forecasts: data, loading: false });
      });
    ////#endif
  }

  static renderForecastsTable(forecasts) {
    return (
      <table className='table table-striped'>
        <thead>
          <tr>
            <th>Date</th>
            <th>Temp. (C)</th>
            <th>Temp. (F)</th>
            <th>Summary</th>
          </tr>
        </thead>
        <tbody>
          {forecasts.map(forecast =>
            <tr key={forecast.dateFormatted}>
              <td>{forecast.dateFormatted}</td>
              <td>{forecast.temperatureC}</td>
              <td>{forecast.temperatureF}</td>
              <td>{forecast.summary}</td>
            </tr>
          )}
        </tbody>
      </table>
    );
  }

  render() {
    let contents = this.state.loading
      ? <p><em>Loading...</em></p>
      : FetchData.renderForecastsTable(this.state.forecasts);

    return (
      <div>
        <h1>Weather forecast</h1>
        <p>This component demonstrates fetching data from the server.</p>
        {contents}
      </div>
    );
  }
////#if (IndividualLocalAuth)

  static getHeaders(token) {
    return !token ? {} : { 'Authorization': `Bearer ${token}` };
  }
////#endif
}
