(function (window, angular) {

    'use strict';

    var nyanStackModule = angular.module('ngNyanStack', ['ngResource', 'ui.router']);

    nyanStackModule
    .provider(
    "nyanStack",
    function provideNyan($stateProvider, $controllerProvider, $compileProvider, $filterProvider, $provide) {

        //Setup and primitives

        var settings = {
            RestPrefix: "data",
            ScopePartialsStorageUrl: "ng/scopes/{ScopeDescriptor}",
            StatePrefix: "module",

            CollectionPage: '{ScopeDescriptor}-collection.html',
            ItemPage: '{ScopeDescriptor}-item.html',

            CollectionController: '{ScopeDescriptor}CollectionCtrl',
            ItemController: '{ScopeDescriptor}ItemCtrl',

            RestEndpoint: '{RestPrefix}/{PluralDescriptor}',

            BaseServiceUrl: './',

            PluralDescriptor: '{ScopeDescriptor}s',

            AppName: '(none)',
            Authenticate: true
        };

        var requests = [];

        var isDefined = angular.isDefined,
        isFunction = angular.isFunction,
        isString = angular.isString,
        isObject = angular.isObject,
        isArray = angular.isArray,
        forEach = angular.forEach,
        extend = angular.extend,
        copy = angular.copy,
        injector = angular.injector();

        var registry =
        {
            controller: $controllerProvider.register,
            directive: $compileProvider.directive,
            filter: $filterProvider.register,
            factory: $provide.factory,
            service: $provide.service,
            stateProvider: $stateProvider
        };

        var defaultOptions = {
            implementFactory: false,
            implementControllers: false,
            implementRoutes: false,
            implementService: false
        };

        var provider = this;

        return ({
            setup: configSetup,
            service: configService,
            scheduledService: configScheduledService,
            module: configModule,
            start: runtimeStart,
            $get: configFactory
        });


        //Setup services

        function setupStackServices() {

            console.log('StackLog');

            registry
            .service('StackLog',
            function () {

                var observerCallbacks = [];

                var that = this;

                this.register = function (callback) {
                    observerCallbacks.push(callback);
                };

                var notifyObservers = function (pType, pMessage) {
                    angular.forEach(observerCallbacks, function (callback) {
                        callback(pType, pMessage);
                    });
                };

                this.log = function (msg) {
                    notifyObservers('l', msg);
                }

                this.info = function (msg) {
                    notifyObservers('i', msg);
                }

                this.warn = function (msg) {
                    notifyObservers('w', msg);
                }

                this.error = function (msg) {
                    notifyObservers('e', msg);
                }
                this.debug = function (msg) {
                    notifyObservers('d', msg);
                }
                return this;
            });

            registry.service('LogConsumer', function ($log, StackLog) {

                StackLog.register(function (t, m) {

                    if (t == 'l')
                        $log.log(m);

                    if (t == 'i')
                        $log.info(m);

                    if (t == 'w')
                        $log.warn(m);

                    if (t == 'e')
                        $log.error(m);

                    if (t == 'd')
                        $log.debug(m);
                });
            });
        }
        //Helpers

        function inherit(parent, extra) {
            return extend(new (extend(function () { }, { prototype: parent }))(), extra);
        }

        function merge(dst) {
            forEach(arguments, function (obj) {
                if (obj !== dst) {
                    forEach(obj, function (value, key) {
                        if (!dst.hasOwnProperty(key)) dst[key] = value;
                    });
                }
            });
            return dst;
        }

        function valueOrDefault(pValue, pDefault) {
            return (typeof pValue === 'undefined' ? pDefault : pValue);
        }

        //Interface members
        function configSetup(uSettings) {
            settings = merge(uSettings, settings);
            return this;
        }

        function configService(ScopeDescriptor, initOptions) {

            initOptions = initOptions || {};

            initOptions.ScopeDescriptor = ScopeDescriptor;
            initOptions = merge(initOptions, defaultOptions);

            initOptions.implementFactory = true;
            initOptions.implementService = true;

            requests.push(initOptions);

            return this;
        }

        function configScheduledService(ScopeDescriptor, initOptions) {

            initOptions = initOptions || {};

            initOptions.autoRefreshCycleSeconds = 60;

            service(ScopeDescriptor, initOptions);

            return this;
        }

        function configModule(ScopeDescriptor, initOptions) {

            initOptions = initOptions || {};

            initOptions.ScopeDescriptor = ScopeDescriptor;
            initOptions = merge(initOptions, defaultOptions);

            initOptions.implementFactory = true;
            initOptions.implementControllers = true;
            initOptions.implementRoutes = true;
            initOptions.implementService = true;
            initOptions.useSecondaryRestEndpoint = true;

            initOptions.ScopeDescriptor = ScopeDescriptor;

            requests.push(initOptions);

            return this;
        }

        //Run-time --------------------------------------------------------------

        function configFactory() {
            return ({
                start: runtimeStart
            });
        }

        function runtimeStart() {

            console.log("Starting");

            setupStackServices();

            var instanceOptions = {
                baseServiceUrl: './',

                RestEndpoint: settings.RestEndpoint,
                isRestReadOnly: false,

                CollectionController: settings.CollectionController,
                CollectionFactoryName: "{ScopeDescriptor}CollectionFactory",
                CollectionName: '{ScopeDescriptor}',
                MainRestEndpoint: settings.RestEndpoint,
                isArrayRestSource: true,
                CollectionPartialUrl: settings.ScopePartialsStorageUrl + '/' + settings.CollectionPage,

                ModuleServiceName: "{ScopeDescriptor}DataService",

                isApplicationScope: false,
                isGlobalService: false,
                isItemQuery: false,

                ItemController: settings.ItemController,
                ItemFactoryName: '{ScopeDescriptor}ItemFactory',
                SecondaryRestEndpoint: settings.RestEndpoint + '/:id',
                ItemPartialUrl: settings.ScopePartialsStorageUrl + '/' + settings.ItemPage,

                LocatorQueryFactory: '{ScopeDescriptor}LocatorFactory',
                LocatorRestEndpoint: '{RestEndpoint}/bylocator/:id',
                LocatorServiceName: '{ScopeDescriptor}LocatorService',

                LookupQueryFactory: "{ScopeDescriptor}QueryFactory",
                LookupQueryRestEndpoint: '{RestEndpoint}/slookup/:term',

                PaginationMode: 'Auto',
                PluralDescriptor: settings.PluralDescriptor,

                refreshCycleSeconds: 0,
                RootPrefix: settings.RootPrefix,
                serviceName: '{ScopeDescriptor}Service',
                StateBase: '{StatePrefix}-{PluralDescriptor}',
                StateDetail: '{StatePrefix}-{PluralDescriptor}.detail',
                StateNew: '{StatePrefix}-{PluralDescriptor}.new',
                StateRoute: '/{StatePrefix}/' + settings.PluralDescriptor,
                StatePrefix: settings.StatePrefix,
                useCollectionController: false,
                useMainRestEndpoint: false,
                useController: false,
                useItemController: false,
                useSecondaryRestEndpoint: false,
                useItemRoute: false,
                useListRoute: false,
                useLocatorQuery: false,
                useLookupQuery: false,
                useReferenceRestService: false,
                useResource: false,
                useState: false
            };

            ImplementNullElements();

            forEach(requests, function (initOptions) {

                //Parse defaults

                initOptions = merge(initOptions, instanceOptions);

                //Now, replace all mask values for the proper scope descriptor.
                for (var key in initOptions) {
                    var probe = initOptions[key];


                    if (typeof probe == 'string') {
                        if (probe.indexOf('{') != -1) {
                            probe = probe.split("{BaseServiceUrl}").join(settings.BaseServiceUrl);
                            probe = probe.split("{RestEndpoint}").join(settings.RestEndpoint);
                            probe = probe.split("{RestPrefix}").join(settings.RestPrefix);
                            probe = probe.split("{RootPrefix}").join(initOptions.RootPrefix);
                            probe = probe.split("{StatePrefix}").join(initOptions.StatePrefix);
                            probe = probe.split("{PluralDescriptor}").join(initOptions.PluralDescriptor);
                            probe = probe.split("{ScopeDescriptor}").join(initOptions.ScopeDescriptor);

                            initOptions[key] = probe;
                        }
                    }
                };

                console.log(initOptions);

                //Pre-compile some statements, based on user choice

                initOptions.implementFactory = true;

                if (initOptions.implementService) {
                    prepareFactories(initOptions);
                }

                if (initOptions.implementControllers) {
                    prepareControllers(initOptions);
                }

                if (initOptions.implementRoutes) {
                    prepareRoutes(initOptions);
                }

            });

            return this;
        }


        function ImplementNullElements() {

            registry
            .factory('NullItemFactory', function () {
                return {
                    fetch: function () { },
                    create: function () { },
                    update: function () { },
                    delete: function () { }
                };
            });

        };

        function prepareFactories(initOptions) {

            console.log('Registering Factory ' + initOptions.CollectionFactoryName);
            console.log('                 to ' + initOptions.MainRestEndpoint);

            registry
            .factory(initOptions.CollectionFactoryName, function ($resource) {

                var oInterface = {
                    fetch: { method: 'GET', isArray: initOptions.isArrayRestSource, withCredentials: settings.Authenticate },
                    update: { method: 'POST', withCredentials: settings.Authenticate },

                };

                if (!initOptions.isRestReadOnly) {
                    oInterface.create = { method: 'POST', withCredentials: settings.Authenticate }
                }

                return $resource(initOptions.MainRestEndpoint, {}, oInterface);
            });


            if (initOptions.useSecondaryRestEndpoint) {
                console.log('Registering Factory ' + initOptions.ItemFactoryName);
                console.log('                 to ' + initOptions.SecondaryRestEndpoint);

                registry
                .factory(initOptions.ItemFactoryName, function ($resource) {

                    var oInterface = {
                        fetch: { method: 'GET', isArray: false, withCredentials: settings.Authenticate },
                        create: { method: 'PUT', withCredentials: settings.Authenticate },
                        update: { method: 'POST', params: { id: '@id' }, withCredentials: settings.Authenticate },
                        delete: { method: 'DELETE', params: { id: '@id' }, withCredentials: settings.Authenticate }
                    };

                    return $resource(initOptions.SecondaryRestEndpoint, {}, oInterface);
                });
            }

            var dataItemServiceName = (initOptions.useSecondaryRestEndpoint ? initOptions.ItemFactoryName : 'NullItemFactory')

            console.log('Registering Service ' + initOptions.ModuleServiceName);
            console.log('         Collection ' + initOptions.CollectionFactoryName);
            console.log('               Item ' + dataItemServiceName);

            registry
            .service(initOptions.ModuleServiceName, [
            initOptions.CollectionFactoryName,
            dataItemServiceName,
            function (collectionFactory, itemFactory) {

                var observerCallbacks = [];

                var that = this;
                var _init = false;
                var _schedule = null;

                this.data = [];

                this.register = function (callback) {
                    observerCallbacks.push(callback);
                    callback(that.data);
                };

                var notifyObservers = function () {
                    angular.forEach(observerCallbacks, function (callback) {
                        callback(that.data);
                    });
                };

                this.remove = function (id) {

                    console.log(initOptions.ModuleServiceName + ': DELETE ' + id);

                    itemFactory.delete({ id: id },
                    function (data) {
                        console.log('itemFactory DELETE SUCCESS');
                        factoryGet();
                    },
                    function (data) {
                        console.log('itemFactory DELETE FAIL =(');
                    }
                    );
                }

                this.fetch = itemFactory.fetch;
                this.delete = itemFactory.delete;
                this.update = collectionFactory.update;
                this.create = itemFactory.create;

                function factoryGet(pScheduled) {

                    console.log(initOptions.ModuleServiceName + ': GET' + (pScheduled ? " (SCHED)" : ""));

                    _init = true;
                    that.data = collectionFactory.fetch(function (data) {
                        that.data = data;
                        notifyObservers(data);
                    });
                }

                //Service bootstrap

                //Set schedule, if required. 

                if (initOptions.autoRefreshCycleSeconds) {

                    _schedule = setInterval(function () { factoryGet(true); }, initOptions.autoRefreshCycleSeconds * 1000);
                    console.log(initOptions.ModuleServiceName + ': SCHED ' + initOptions.autoRefreshCycleSeconds + "s");
                }

                //Initial load

                factoryGet();

                return this;
            }]);

            if (initOptions.useLookupQuery) {

                console.log('Registering Factory ' + initOptions.LookupQueryFactory);
                console.log('                 to ' + initOptions.LookupQueryRestEndpoint);

                registry.factory(initOptions.LookupQueryFactory, function ($resource) {
                    return $resource(initOptions.LookupQueryRestEndpoint, {}, {
                        query: { method: 'GET', isArray: false }
                    });
                });
            }

            if (initOptions.useLocatorQuery) {
                console.log('Registering Factory ' + initOptions.LocatorQueryFactory);
                console.log('                 to ' + initOptions.LocatorRestEndpoint);

                registry.factory(initOptions.LocatorQueryFactory, function ($resource) {
                    return $resource(initOptions.LocatorRestEndpoint, {}, {
                        query: { method: 'GET', isArray: false }
                    });
                });

                console.log('Registering Service ' + initOptions.LocatorServiceName);

                registry.service(initOptions.LocatorServiceName,
                [
                initOptions.LocatorQueryFactory,
                '$cacheFactory',
                '$q',
                function (locatorFactory, $cacheFactory, $q) {

                    console.log("Initializing " + initOptions.serviceName + " for " + initOptions.LocatorQueryFactory);

                    var cache = $cacheFactory('cache.' + initOptions.serviceName);
                    var that = this;
                    var config = initOptions;

                    this.state = {
                        isLoading: false,
                        hasFailed: false,
                        hasOldData: false
                    };

                    this.getData = function (key) {

                        that.state.isLoading = true;
                        console.log('fetching ' + key);
                        var probe = cache.get(key);

                        if (probe) {
                            clearState();
                            return probe;
                        }
                        var deferred = $q.defer();

                        locatorFactory.query({ id: key },
                        function (data) {
                            cache.put(key, data);
                            deferred.resolve(data);
                            clearState();
                        },
                        function (error) {
                            that.state.isLoading = false;
                            that.state.hasFailed = true;
                            deferred.reject(error.statusText);
                        });

                        return deferred.promise;
                    };

                    function clearState() {
                        that.state.isLoading =
                        that.state.hasFailed = false;
                    }
                }
                ]);

            }

        }

        function prepareRoutes(initOptions) {

            console.log(' Preparing Routes ' + initOptions.StateRoute);

            console.log('            State ' + initOptions.StateBase);
            console.log('              URL ' + initOptions.CollectionPartialUrl);
            console.log('       Controller ' + initOptions.CollectionController);

            $stateProvider
            .state(initOptions.StateBase, {
                url: initOptions.StateRoute,
                templateUrl: initOptions.CollectionPartialUrl,
                controller: initOptions.CollectionController
            });

            console.log('            State ' + initOptions.StateDetail);
            console.log('              URL ' + initOptions.ItemPartialUrl);
            console.log('       Controller ' + initOptions.ItemController);

            $stateProvider
            .state(initOptions.StateDetail, {
                url: '/:id',
                templateUrl: initOptions.ItemPartialUrl,
                controller: initOptions.ItemController
            });

            console.log('            State ' + initOptions.StateNew);
            console.log('              URL ' + initOptions.ItemPartialUrl);
            console.log('       Controller ' + initOptions.ItemController);

            $stateProvider
            .state(initOptions.StateNew, {
                url: '/new',
                templateUrl: initOptions.ItemPartialUrl,
                controller: initOptions.ItemController
            });
        }

        function prepareControllers(initOptions) {
            registry.controller(
            initOptions.CollectionController, [
            '$scope',
            '$state',
            '$stateParams',
            '$filter',
            initOptions.ModuleServiceName,
            function ($scope, $state, $stateParams, $filter, dataService) {

                $scope.Stack = {
                    service: dataService,
                    Options: initOptions
                }

                $scope.data = [];

                $scope.selectedId = 0;

                if ($state.params)
                    $scope.selectedId = $state.params.id;

                $scope.select = function (item) {
                    $scope.selectedId = item.id;
                    $state.go(initOptions.StateBase + '.detail', { id: $scope.selectedId });
                };

                $scope.createNew = function () {
                    $scope.selectedId = 0;
                    $state.go(initOptions.StateBase + '.new');
                };

                dataService.register(function (data) {
                    $scope.data = data;
                });
            }
            ]);


            //Item Control
            registry.controller(
            initOptions.ItemController, [
            '$scope',
            '$state',
            '$stateParams',
            initOptions.ModuleServiceName,
            'StackLog',
            '$q',
            function ($scope, $state, $stateParams, dataService, Log, $q) {

                $scope.Stack = {
                    service: dataService,
                    Options: initOptions
                }

                $scope.State = {};

                $scope.item = {};

                //console.log('    Controller \'' + initOptions.baseItemController + '\' invoked');

                $scope.save = function () {

                    var data = $scope.item;

                    if ($scope.onBeforeSave)
                        $q.when($scope.onBeforeSave(data)).then(function () { $scope.onHandlePreSave(data); });
                    else
                        $scope.onHandlePreSave(data);
                };

                $scope.onHandlePreSave = function (data) {

                    dataService.update(data,
                    function (procData) {
                        if ($scope.onAfterSave)
                            $q.when($scope.onAfterSave(procData)).then(function () { $scope.onHandlePostSave(procData); });
                        else
                            $scope.onHandlePostSave(procData);
                    },
                    function (e) {
                        Log.warn({
                            title: $scope.Stack.Options.collectionName,
                            description: "An exception occurred while saving this item: " + e.data.ExceptionMessage
                        });
                    }
                    );
                };

                $scope.onHandlePostSave = function (data) {

                    Log.info({
                        title: $scope.Stack.Options.collectionName,
                        description: "Item saved."
                    });

                    $scope.selectedId = data.Id;
                    $state.go(initOptions.StateBase + '.detail', { id: data.Id }, { reload: true });
                };

                $scope.cancel = function () {
                    $scope.selectedId = 0;
                    $state.go(initOptions.StateBase);
                };

                $scope.delete = function (id) {

                    if ($scope.onBeforeDelete) $scope.onBeforeDelete();

                    dataService.remove(id,

                    function (data) {

                        if ($scope.onAfterDelete) $scope.onAfterDelete();

                        $scope.selectedId = 0;

                        $state.go(initOptions.StateBase, null, { reload: true });

                        Log.info({
                            title: $scope.Stack.Options.collectionName,
                            description: "Item " + id + " successfully removed."
                        });

                    },
                    function (e) {
                        Log.warn({
                            title: $scope.Stack.Options.collectionName,
                            description: "An exception occurred while removing this item: " + e.data.ExceptionMessage
                        });
                    }
                    );
                };

                $scope.setMode = function (parm) {
                    switch (parm) {
                        case 'delete':
                            $scope.State.Display = "view";
                            $scope.State.Operation = "delete";
                            break;
                        case 'edit':
                            $scope.State.Display = "edit";
                            $scope.controllerOperationMode = "edit";
                            break;
                        case 'view':
                            $scope.State.Display = "view";
                            $scope.controllerOperationMode = "view";
                            break;
                        case 'new':
                            $scope.State.Display = "edit";
                            $scope.State.Display = "new";
                            break;
                    }
                };

                //If LoadData isn't overloaded by inheritance, implement it.

                var targetId = $stateParams.id || 'new';

                if (!$scope.itemLoadData) {

                    if ($scope.onBeforeLoadData) $scope.onBeforeLoadData();

                    $scope.itemLoadData = function () {
                        dataService.fetch(
                        { id: targetId },
                        function (data) {
                            $scope.item = data;
                            if ($scope.onAfterLoadData) $scope.onAfterLoadData();
                        }
                        );
                        $scope.setMode(targetId == 'new' ? 'new' : 'view');
                    }
                }

                $scope.itemLoadData();

                //console.log('    Controller \'' + initOptions.baseItemController + '\' finished loading');
            }
            ]);


        }

        function configuration() {
            return ({
                appRootUrl: appRootUrl,
                isOperational: true
            });
            function appRootUrl() {
                return window.location.origin + window.location.pathname;
            }
        }

    }
    );
}
)(window, window.angular);