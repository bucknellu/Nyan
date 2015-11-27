(function (window, angular) {

    'use strict';

    var nyanStackModule = angular.module('ngNyanLogToaster', ['ngNyanStack', 'toaster']);

    nyanStackModule
    .service(
    "nyanLogToasterService",
    function (nyanLogPipelineService, toaster) {


        this.log = function () {
            //Sit on this one.
        }

        this.info = function () {
            toaster.pop({
                type: 'success',
                title: arguments[0],
                body: arguments[1]
            });
        }

        this.warn = function () {
            toaster.pop({
                type: 'warning',
                title: arguments[0],
                body: arguments[1]
            });
        }

        this.error = function () {
            toaster.pop({
                type: 'error',
                title: arguments[0],
                body: arguments[1]
            });
        }

        this.debug = function () {
            toaster.pop({
                type: 'info',
                title: arguments[0],
                body: arguments[1]
            });
        }

        nyanLogPipelineService.register(this);

    }).run(function (nyanLogToasterService) {
        console.log('nyanLogToasterService: Start');
    })
}
)(window, window.angular);