/// <reference path="../../../../node_modules/aurelia-fetch-client/doc/whatwg-fetch.d.ts" />
/// <reference path="../../../../node_modules/aurelia-fetch-client/doc/url.d.ts" />
import { HttpClient } from 'aurelia-fetch-client';
import { inject } from 'aurelia-framework';

@inject(HttpClient)
export class Fetchdata {
    public forecasts: WeatherForecast[];

    constructor(http: HttpClient) {
        http.fetch('/api/SampleData/WeatherForecasts')
            .then(result => result.json())
            .then(data => {
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
