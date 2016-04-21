import * as ng from 'angular2/core';
import * as router from 'angular2/router';
import { Http } from 'angular2/http';
import * as models from '../../../models/models';

@ng.Component({
  selector: 'genres-list',
  templateUrl: './ng-app/components/public/genres-list/genres-list.html',
  directives: [router.ROUTER_DIRECTIVES]
})
export class GenresList {
    public genres: models.Genre[];

    constructor(http: Http) {
        http.get('/api/genres').subscribe(result => {
            this.genres = result.json();
        });
    }
}
