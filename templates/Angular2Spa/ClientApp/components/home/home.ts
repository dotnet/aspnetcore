import * as ng from 'angular2/core';
import { carouselItems } from '../../data/CarouselItems';
import { linkLists } from '../../data/HomepageLinkLists';

@ng.Component({
  selector: 'home'
})
@ng.View({
  template: require('./home.html')
})
export class Home {
    public carouselItems = carouselItems;
    public linkLists = linkLists;
}
