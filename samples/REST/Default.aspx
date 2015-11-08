<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="Nyan.Samples.REST.Default" %>

<!DOCTYPE html>

<html ng-app="NyanNG">
<head>
    <script src="https://cdnjs.cloudflare.com/ajax/libs/angular.js/1.4.7/angular.js"></script>
    <script src="https://cdnjs.cloudflare.com/ajax/libs/angular.js/1.4.7//angular-resource.js"></script>
    <title>AngularJS Sample</title>
</head>
<body>
    <div ng-controller="SampleController">
        <table>
            <tr>
                <td>
                    <table>
                        <tr>
                            <th>ID</th>
                            <th>Name</th>
                            <th>Surname</th>
                        </tr>
                        <tr ng-repeat='i in entries' ng-click="get(i.id)">
                            <td>{{i.id}}</td>
                            <td>{{i.Name}}</td>
                            <td>{{i.Surname}}</td>
                        </tr>
                    </table>
                </td>
                <td>

                    <button ng-click="make(1)">Make 1</button>
                    <button ng-click="make(10)">Make 10</button>
                    <button ng-click="make(50)">Make 50</button>
                    <button ng-click="wipe()">Wipe</button>


                    <button ng-click="new()">New</button>
                    <button ng-click="delete(selected.id)" ng-if="(selected.id != 0) && selected">Delete</button>
                    <button ng-click="edit()" ng-if="!editing && selected">Edit</button>
                    <button ng-click="save()" ng-if="editing && selected">Save</button>
                    <button ng-click="cancel()" ng-if="editing && selected">Cancel</button>

                    <div ng-if="selected">
                        <table ng-if="!editing">
                            <tr>
                                <td>ID</td>
                                <td>{{selected.id}}</td>
                            </tr>
                            <tr>
                                <td>Name</td>
                                <td>{{selected.Name}}</td>
                            </tr>
                            <tr>
                                <td>Surname</td>
                                <td>{{selected.Surname}}</td>
                            </tr>
                            <tr>
                                <td>isAdmin</td>
                                <td>{{selected.isAdmin}}</td>
                            </tr>
                            <tr>
                                <td>BirthDate</td>
                                <td>{{selected.BirthDate}}</td>
                            </tr>
                        </table>
                        <table ng-if="editing">
                            <tr>
                                <td>ID</td>
                                <td>{{selected.id}}</td>
                            </tr>
                            <tr>
                                <td>Name</td>
                                <td>
                                    <input type="text" ng-model="selected.Name" /></td>
                            </tr>
                            <tr>
                                <td>Surname</td>
                                <td>
                                    <input type="text" ng-model="selected.Surname" /></td>
                            </tr>
                            <tr>
                                <td>isAdmin</td>
                                <td>
                                    <input type="checkbox" ng-model="selected.isAdmin" /></td>
                            </tr>
                            <tr>
                                <td>BirthDate</td>
                                <td>
                                    <input type="text" ng-model="selected.BirthDate" /></td>
                            </tr>
                        </table>
                    </div>
                </td>
            </tr>
        </table>
    </div>
</body>
<script>

    angular.module('NyanNG', ['ngResource'])

        .factory('userFactory', function ($resource) {
            return $resource('api/users/:id');
        })

        .controller('SampleController', function SampleController($scope, $filter, $http, userFactory) {

            $scope.editing = false;

            $scope.query = function () {

                var entries = userFactory.query(function () {
                    $scope.entries = entries;
                });
            };

            $scope.delete = function (pId) {
                userFactory.delete({ id: pId }, function () {
                    $scope.entries = $filter('filter')($scope.entries, { id: '!' + pId });
                    delete $scope.selected;
                });
            };

            $scope.save = function () {
                userFactory.save($scope.selected, function () {
                    delete $scope.selected;
                    $scope.query();
                });
            };

            $scope.get = function (pId) {
                var entry = userFactory.get({ id: pId }, function () {
                    $scope.selected = entry;
                    $scope.editing = false;
                });
            };

            $scope.new = function () {
                var entry = userFactory.get({ id: 'new' }, function () {
                    $scope.selected = entry;
                    $scope.editing = true;
                });
            };

            $scope.make = function (quantity) {
                $http.get("api/users/make/" + quantity)
                    .success(function (response) {
                        $scope.query();
                    });

            }

            $scope.wipe = function (quantity) {
                $http.get("api/users/wipe")
                    .success(function (response) {
                        $scope.query();
                    });

            }

            $scope.cancel = function () {
                delete $scope.selected;
            };

            $scope.edit = function () {
                $scope.editing = true;
            };

            $scope.query();

        });
</script>
</html>
