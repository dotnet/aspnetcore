import Vue from 'vue';
import { Component } from 'vue-property-decorator';

interface WeatherForecast {
    dateFormatted: string;
    temperatureC: number;
    temperatureF: number;
    summary: string;
}

@Component
export default class FetchDataComponent extends Vue {
    forecasts: WeatherForecast[] = [];

    async mounted() {
        try {
            let response = await fetch('api/SampleData/WeatherForecasts'); 
            let data = await response.json();
            this.forecasts = data;
         }
        catch (err) {
            console.log(err);
        }
    }
}
