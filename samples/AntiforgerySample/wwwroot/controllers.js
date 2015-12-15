angular.module('TODO.controllers', []).
controller('todoController', function ($scope, todoApi) {
    $scope.itemList = [];
    $scope.item = {};

    $scope.refresh = function (item) {
        todoApi.getItems().success(function (response) {
            $scope.itemList = response;
        });
    };

    $scope.create = function (item) {
        todoApi.create(item).success(function (response) {
            $scope.item = {};
            $scope.refresh();
        });
    };

    // Load initial items
    $scope.refresh();
});