import * as ng from 'angular2/core';
import { Http } from 'angular2/http';
import { AlbumTile } from '../album-tile/album-tile';
import * as models from '../../../models/models';

@ng.Component({
  selector: 'home',
  templateUrl: './ng-app/components/public/home/home.html',
  directives: [AlbumTile]
})
export class Home {
    public mostPopular: models.Album[];

    constructor(http: Http) {
        // Workaround for RC1 bug. This can be removed with ASP.NET Core 1.0 RC2.
        let isServerSide = typeof window === 'undefined';
        let options: any = isServerSide ? { headers: { Connection: 'keep-alive' } } : null;

        http.get('/api/albums/mostPopular', options).subscribe(result => {
            this.mostPopular = result.json();
        });
    }
}
