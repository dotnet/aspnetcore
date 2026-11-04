'use strict';
angular.module('todoApp')
.controller('todoListCtrl', ['$scope', '$location', 'todoListSvc', 'adalAuthenticationService', function ($scope, $location, todoListSvc, adalService) {
    $scope.error = "";
    $scope.loadingMessage = "Loading...";
    $scope.todoList = null;
    $scope.editingInProgress = false;
    $scope.newTodoCaption = "";


    $scope.editInProgressTodo = {
        Description: "",
        ID: 0
    };

    

    $scope.editSwitch = function (todo) {
        todo.edit = !todo.edit;
        if (todo.edit) {
            $scope.editInProgressTodo.Description = todo.Description;
            $scope.editInProgressTodo.ID = todo.ID;
            $scope.editingInProgress = true;
        } else {
            $scope.editingInProgress = false;
        }
    };

    $scope.populate = function () {
        todoListSvc.getItems().success(function (results) {
            $scope.todoList = results;
            $scope.loadingMessage = "";
        }).error(function (err) {
            $scope.error = err;
            $scope.loadingMessage = "";
        })
    };
    $scope.delete = function (id) {
        todoListSvc.deleteItem(id).success(function (results) {
            $scope.loadingMessage = "";
            $scope.populate();
        }).error(function (err) {
            $scope.error = err;
            $scope.loadingMessage = "";
        })
    };
    $scope.update = function (todo) {
        todoListSvc.putItem($scope.editInProgressTodo).success(function (results) {
            $scope.loadingMsg = "";
            $scope.populate();
            $scope.editSwitch(todo);
        }).error(function (err) {
            $scope.error = err;
            $scope.loadingMessage = "";
        })
    };
    $scope.add = function () {

        todoListSvc.postItem({
            'Description': $scope.newTodoCaption,
            'Owner': adalService.userInfo.userName
        }).success(function (results) {
            $scope.loadingMsg = "";
            $scope.newTodoCaption = "";
            $scope.populate();
        }).error(function (err) {
            $scope.error = err;
            $scope.loadingMsg = "";
        })
    };
}]);