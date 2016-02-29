import * as ng from 'angular2/core';
import fetch from 'isomorphic-fetch';

@ng.Component({
  selector: 'fetch-data'
})
@ng.View({
  template: require('./fetch-data.html')
})
export class FetchData {
    public forecasts: WeatherForecast[];
    
    constructor() {
        fetch('/api/SampleData/WeatherForecasts')
            .then(response => response.json())
            .then((data: WeatherForecast[]) => {
                this.forecasts = data;
            });
    }
}

interface WeatherForecast {
    dateFormatted: string;
    temperatureC: number;
    temperatureF: number;
    summary: string;
}
