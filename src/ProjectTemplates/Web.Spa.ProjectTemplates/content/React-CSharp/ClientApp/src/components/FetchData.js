import React, { useEffect, useState } from 'react';
////#if (IndividualLocalAuth)
import authService from './api-authorization/AuthorizeService'
////#endif

const FetchData = () => {
  const [forecasts, setForecasts] = useState([]);
  const [loading, setLoading] = useState(true);

  const populateWeatherData = async () => {
////#if (IndividualLocalAuth)
    const token = await authService.getAccessToken();
    const response = await fetch('weatherforecast', {
      headers: !token ? {} : { 'Authorization': `Bearer ${token}` }
    });
    const data = await response.json();
    setForecasts(data);
    setLoading(false);
////#else
    const response = await fetch('weatherforecast');
    const data = await response.json();
    setForecasts(data);
    setLoading(false);
////#endif
  }

  useEffect(() => {
    if (loading) {
      populateWeatherData();
    }
  });

  const renderForecastsTable = (forecasts) => {
    return (
      <table className='table table-striped' aria-labelledby="tabelLabel">
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
            <tr key={forecast.date}>
              <td>{forecast.date}</td>
              <td>{forecast.temperatureC}</td>
              <td>{forecast.temperatureF}</td>
              <td>{forecast.summary}</td>
            </tr>
          )}
        </tbody>
      </table>
    );
  };

  let contents = loading
    ? <p><em>Loading...</em></p>
    : renderForecastsTable(forecasts);

  return (
    <div>
      <h1 id="tabelLabel" >Weather forecast</h1>
      <p>This component demonstrates fetching data from the server.</p>
      {contents}
    </div>
  );
};

export default FetchData;
