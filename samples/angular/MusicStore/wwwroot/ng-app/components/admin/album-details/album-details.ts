import * as ng from 'angular2/core';
import * as router from 'angular2/router';
import * as models from '../../../models/models';
import { Http, HTTP_BINDINGS } from 'angular2/http';
import { AlbumDeletePrompt } from '../album-delete-prompt/album-delete-prompt';

@ng.Component({
  selector: 'album-details',
  templateUrl: './ng-app/components/admin/album-details/album-details.html',
  directives: [router.ROUTER_DIRECTIVES, AlbumDeletePrompt]
})
export class AlbumDetails {
    public albumData: models.Album;

    constructor(http: Http, routeParam: router.RouteParams) {
        http.get('/api/albums/' + routeParam.params['albumId']).subscribe(result => {
            this.albumData = result.json();
        });
    }
}
