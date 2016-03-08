import * as ko from 'knockout';

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
            .then(response => response.json())
            .then((data: WeatherForecast[]) => {
                this.forecasts(data);
            });
    }
}

export default { viewModel: FetchDataViewModel, template: require('./fetch-data.html') };
