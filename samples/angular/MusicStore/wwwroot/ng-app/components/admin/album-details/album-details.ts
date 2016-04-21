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
        // Workaround for RC1 bug. This can be removed with ASP.NET Core 1.0 RC2.
        let isServerSide = typeof window === 'undefined';
        let options: any = isServerSide ? { headers: { Connection: 'keep-alive' } } : null;

        http.get('/api/albums/' + routeParam.params['albumId'], options).subscribe(result => {
            this.albumData = result.json();
        });
    }
}
