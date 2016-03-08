import * as ko from 'knockout';
import * as router from '../../router';

class AppLayoutViewModel {
    public route = router.instance().currentRoute;
}

export default { viewModel: AppLayoutViewModel, template: require('./app-layout.html') };
