app
.controller(
    'userCollectionCtrl',
        function ($controller, $scope, $log, $http, $state) {

            $controller('userCollectionBaseCtrl', { $scope: $scope }); //Inheriting base controller.            

            $scope.wipe = function () {

                $log.debug('Wiping all entries...');

                $http.get("data/users/wipe")
                   .then(function () {
                       $log.info('Entries wiped.');

                       $scope.selectedId = 0;
                       $scope.refresh();
                       $state.go('module-users', null, { reload: true });
                   });
            };

            $scope.make = function (number) {

                $log.debug('Creating ' + number + ' entries...');

                $http.get("data/users/make/" + number)
                   .then(function () {
                       $log.info(number + ' entries created.');

                       $scope.selectedId = 0;
                       $scope.refresh();
                       $state.go('module-users', null, { reload: true });
                   });
            };
        });
