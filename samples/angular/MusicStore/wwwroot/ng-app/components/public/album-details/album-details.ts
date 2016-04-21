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
        // Workaround for RC1 bug. This can be removed with ASP.NET Core 1.0 RC2.
        let isServerSide = typeof window === 'undefined';
        let options: any = isServerSide ? { headers: { Connection: 'keep-alive' } } : null;
        
        http.get('/api/albums/' + routeParam.params['albumId'], options).subscribe(result => {
            this.albumData = result.json();
        });
    }
}
