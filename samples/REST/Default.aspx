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
                <th>ID</th>
                <th>Name</th>
                <th>Surname</th>
                <th>BirthDate</th>
                <th>isAdmin</th>
            </tr>
            <tr ng-repeat='i in entries'>
                <td>{{i.id}}</td>
                <td>{{i.Name}}</td>
                <td>{{i.Surname}}</td>
                <td>{{i.BirthDate}}</td>
                <td>{{i.isAdmin}}</td>
                <td> <button ng-click="remove(i.id)">Remove</button></td>
            </tr>
        </table>
    </div>
</body>
<script>

    angular.module('NyanNG', ['ngResource'])
        .factory('userFactory', function ($resource) {
            return $resource('users/:id');
        })
        .controller('SampleController', function SampleController($scope, $filter, userFactory) {

            $scope.loadAll = function () {

                var entries = userFactory.query(function () {
                    $scope.entries = entries;
                });
            };

            $scope.remove = function (pId) {
                userFactory.delete({ id: pId }, function () {

                    $scope.entries = $filter('filter')($scope.entries, { id: '!' + pId });
                });
            };

            $scope.loadAll();

        });
</script>
</html>
