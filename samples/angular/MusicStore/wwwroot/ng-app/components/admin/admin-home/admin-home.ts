import * as ng from 'angular2/core';
import * as router from 'angular2/router';
import { AlbumsList } from '../albums-list/albums-list';
import { AlbumDetails } from '../album-details/album-details';
import { AlbumEdit } from '../album-edit/album-edit';

@ng.Component({
  selector: 'admin-home',
  templateUrl: './ng-app/components/admin/admin-home/admin-home.html',
  directives: [router.ROUTER_DIRECTIVES]
})
@router.RouteConfig([
  { path: 'albums', name: 'Albums', component: AlbumsList },
  { path: 'album/details/:albumId', name: 'AlbumDetails', component: AlbumDetails },
  { path: 'album/edit/:albumId', name: 'AlbumEdit', component: AlbumEdit }
])
export class AdminHome {

}
