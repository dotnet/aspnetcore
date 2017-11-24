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
        const response = await fetch('api/SampleData/WeatherForecasts'); 
        this.forecasts = await response.json();
    }
}
