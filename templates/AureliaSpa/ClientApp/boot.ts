import { Aurelia } from 'aurelia-framework';
import 'bootstrap/dist/css/bootstrap.css';
import 'bootstrap';

export function configure(aurelia: Aurelia) {
    aurelia.use.standardConfiguration();
    if (window.location.host.includes('localhost')) {
        aurelia.use.developmentLogging();
    }
    aurelia.start().then(() => aurelia.setRoot('app/components/app/app'));
}
