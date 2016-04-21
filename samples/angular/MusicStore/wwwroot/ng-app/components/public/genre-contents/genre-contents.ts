import * as ng from 'angular2/core';
import * as router from 'angular2/router';
import { Http } from 'angular2/http';
import * as models from '../../../models/models';
import { AlbumTile } from '../album-tile/album-tile';

@ng.Component({
  selector: 'genre-contents',
  templateUrl: './ng-app/components/public/genre-contents/genre-contents.html',
  directives: [AlbumTile]
})
export class GenreContents {
    public albums: models.Album[];

    constructor(http: Http, routeParam: router.RouteParams) {
        // Workaround for RC1 bug. This can be removed with ASP.NET Core 1.0 RC2.
        let isServerSide = typeof window === 'undefined';
        let options: any = isServerSide ? { headers: { Connection: 'keep-alive' } } : null;
        
        http.get(`/api/genres/${ routeParam.params['genreId'] }/albums`, options).subscribe(result => {
            this.albums = result.json();
        });
    }
}
