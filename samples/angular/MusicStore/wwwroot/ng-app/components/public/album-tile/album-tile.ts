import * as ng from 'angular2/core';
import * as router from 'angular2/router';
import * as models from '../../../models/models';

@ng.Component({
  selector: 'album-tile',
  properties: ['albumData'],
  templateUrl: './ng-app/components/public/album-tile/album-tile.html',
  directives: [router.ROUTER_DIRECTIVES]
})
export class AlbumTile {
}
