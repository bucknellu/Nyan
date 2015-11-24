var app = angular.module('NyanNG', ['ngMaterial', 'ngResource', 'ngMessages', 'ui.router', 'ngNyanStack']);

app
.config(function ($mdThemingProvider) {
    // Configure a dark theme with primary foreground yellow
    $mdThemingProvider.theme('docs-dark', 'default')
    .primaryPalette('yellow')
    .dark();
})
.config(['$locationProvider', '$stateProvider', '$nyanStackProvider',
function ($locationProvider, $stateProvider, $nyanStackProvider) {


    //$locationProvider.html5Mode(true);
}
]).run(['$nyanStack', function ($nyanStack) {

    $nyanStack
        .setup({
            appName: 'Nyan Angular Sample',
            RootPrefix: 'my'
        })
        .prepare('user', {
            RootPrefix: "data",
            collectionName: 'User'
        })
        .registerAll()
    ;

}]);;