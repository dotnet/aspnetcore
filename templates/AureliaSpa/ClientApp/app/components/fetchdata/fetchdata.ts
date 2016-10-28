// The following line is a workaround for aurelia-fetch-client requiring the type UrlSearchParams
// to exist in global scope, but that type not being declared in any public @types/* package.
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
