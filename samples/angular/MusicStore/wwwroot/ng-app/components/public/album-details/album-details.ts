import * as ng from 'angular2/core';
import * as router from 'angular2/router';
import { Http } from 'angular2/http';
import * as models from '../../../models/models';

@ng.Component({
  selector: 'album-details',
  templateUrl: './ng-app/components/public/album-details/album-details.html'
})
export class AlbumDetails {
    public albumData: models.Album;

    constructor(http: Http, routeParam: router.RouteParams) {
        http.get('/api/albums/' + routeParam.params['albumId']).subscribe(result => {
            this.albumData = result.json();
        });
    }
}
