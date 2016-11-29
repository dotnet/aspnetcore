import * as ko from 'knockout';
import 'isomorphic-fetch';

interface WeatherForecast {
    dateFormatted: string;
    temperatureC: number;
    temperatureF: number;
    summary: string;
}

class FetchDataViewModel {
    public forecasts = ko.observableArray<WeatherForecast>();

    constructor() {
        fetch('/api/SampleData/WeatherForecasts')
            .then(response => response.json() as Promise<WeatherForecast[]>)
            .then(data => {
                this.forecasts(data);
            });
    }
}

export default { viewModel: FetchDataViewModel, template: require('./fetch-data.html') };
