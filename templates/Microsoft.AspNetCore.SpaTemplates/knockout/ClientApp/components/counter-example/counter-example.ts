import * as ko from 'knockout';

class CounterExampleViewModel {
    public currentCount = ko.observable(0);

    public incrementCounter() {
        let prevCount = this.currentCount();
        this.currentCount(prevCount + 1);
    }
}

export default { viewModel: CounterExampleViewModel, template: require('./counter-example.html') };
