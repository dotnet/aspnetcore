import * as ng from 'angular2/core';
import { Http } from 'angular2/http';

@ng.Component({
  selector: 'fetch-data',
  template: require('./fetch-data.html')
})
export class FetchData {
    public forecasts: WeatherForecast[];

    constructor(http: Http) {
        // Workaround for RC1 bug. This can be removed with ASP.NET Core 1.0 RC2.
        let isServerSide = typeof window === 'undefined';
        let options: any = isServerSide ? { headers: { Connection: 'keep-alive' } } : null;

        http.get('/api/SampleData/WeatherForecasts', options).subscribe(result => {
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
