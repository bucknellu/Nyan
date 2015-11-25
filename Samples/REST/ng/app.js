var app = angular.module('NyanNG', ['ui.router', 'ngNyanStack']);

app
    .config([
        '$locationProvider', '$stateProvider', 'nyanStackProvider', '$httpProvider',
        function ($locationProvider, $stateProvider, nyanStackProvider, $httpProvider) {

            nyanStackProvider
                .setup({
                    AppName: 'Nyan Angular Sample',
                    ScopePrefix: 'ng/scopes/'
                })
                .module('user', {
                    RootPrefix: "data",
                    collectionName: 'User',
                    useLookupQuery: true,
                    useLocatorQuery: true,

                });

            $httpProvider.defaults.useXDomain = true;
            delete $httpProvider.defaults.headers.common['X-Requested-With'];
        }
    ]).run([
        'nyanStack', function (nyanStack) {
            nyanStack.start();
        }
    ]);
angular.module('ngNyanStack')
    .controller('SampleController', function ($scope, userDataService) {

        $scope.svc = userDataService;

        var localUpdate = function () {
            $scope.data = userDataService.data;
        };

        userDataService.register(localUpdate);

    });
