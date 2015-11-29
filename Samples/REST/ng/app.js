var app = angular.module('NyanNG', ['ui.router', 'ngNyanStack', 'ngNyanLogToaster']);

app
    .config([
        '$locationProvider', '$stateProvider', 'nyanStackProvider', '$httpProvider',
        function ($locationProvider, $stateProvider, nyanStackProvider, $httpProvider) {

            $httpProvider.defaults.useXDomain = true;
            delete $httpProvider.defaults.headers.common['X-Requested-With'];

            nyanStackProvider
                .setup({
                    AppName: 'Nyan Angular Sample',
                    ScopePrefix: 'ng/scopes/'
                })
                .module('user', {
                    RootPrefix: "data",
                    collectionName: 'Users',
                    CollectionController: 'userCollectionCtrl'
                })
                .start();

        }
    ]).run(
        function (nyanStack, $state) {
            $state.go('module-users');
        }
    );

app.controller('topLevelController', function ($log) {
});