import * as ng from 'angular2/core';
import { Http } from 'angular2/http';

@ng.Component({
  selector: 'fetch-data',
  template: require('./fetch-data.html')
})
export class FetchData {
    public forecasts: WeatherForecast[];

    constructor(http: Http) {
        // TODO: Switch to relative URL once angular-universal supports them
        // https://github.com/angular/universal/issues/348
        http.get('http://localhost:5000/api/SampleData/WeatherForecasts', {
            headers: <any>{ Connection: 'keep-alive' } // Workaround for RC1 bug. TODO: Remove this after updating to RC2
        }).subscribe(result => {
            this.forecasts = result.json();
        });
    }
}

interface WeatherForecast {
    dateFormatted: string;
    temperatureC: number;
    temperatureF: number;
    summary: string;
}
