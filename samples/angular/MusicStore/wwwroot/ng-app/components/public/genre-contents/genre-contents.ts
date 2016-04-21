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
        http.get(`/api/genres/${ routeParam.params['genreId'] }/albums`).subscribe(result => {
            this.albums = result.json();
        });
    }
}
