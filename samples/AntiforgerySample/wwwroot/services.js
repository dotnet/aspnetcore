angular.module('TODO.services', []).
  factory('todoApi', function ($http) {

      var todoApi = {};

      todoApi.getItems = function () {
          return $http({
              method: 'GET',
              url: '/api/items'
          });
      }

      todoApi.create = function (item) {
          return $http({
              method: 'POST',
              url: '/api/items',
              data: item
          });
      };

      return todoApi;
  });