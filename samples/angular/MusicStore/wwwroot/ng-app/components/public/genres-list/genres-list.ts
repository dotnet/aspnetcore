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
        // Workaround for RC1 bug. This can be removed with ASP.NET Core 1.0 RC2.
        let isServerSide = typeof window === 'undefined';
        let options: any = isServerSide ? { headers: { Connection: 'keep-alive' } } : null;
        
        http.get('/api/genres', options).subscribe(result => {
            this.genres = result.json();
        });
    }
}
