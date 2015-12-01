(function (window, angular) {

    'use strict';

    var nyanStackModule = angular.module('ngNyanStack', ['ngResource', 'ui.router']);

    nyanStackModule
    .provider(
    "nyanStack",
    function provideNyan($stateProvider, $controllerProvider, $compileProvider, $filterProvider, $provide) {

        console.log('nyanStack: Initializing');

        //Setup and primitives

        var settings = {
            RestPrefix: "data",
            ScopePartialsStorageUrl: "ng/scopes/{ScopeDescriptor}",
            StatePrefix: "module",

            CollectionPage: '{ScopeDescriptor}-collection.html',
            ItemPage: '{ScopeDescriptor}-item.html',

            CollectionBaseController: '{ScopeDescriptor}CollectionBaseCtrl',
            ItemBaseController: '{ScopeDescriptor}ItemBaseCtrl',

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

        var configuration = {
            appRootUrl: window.location.origin + window.location.pathname,
            isOperational: true
        };


        return ({
            setup: configSetup,
            service: configService,
            scheduledService: configScheduledService,
            module: configModule,
            start: configStart,
            $get: configFactory
        });

        //Setup services

        function setupStackServices() {

            $provide.decorator('$log', [
            "$delegate",
            "nyanLogPipelineService",
            function ($delegate, pipeline) {
                // Save the original $log.debug()

                var original = {
                    log: $delegate.log,
                    info: $delegate.info,
                    warn: $delegate.warn,
                    error: $delegate.error,
                    debug: $delegate.debug
                };

                $delegate.log = function () {
                    pipeline.log.apply(null, arguments);
                    original.log.apply(null, arguments);
                };

                $delegate.info = function () {
                    pipeline.info.apply(null, arguments);
                    original.info.apply(null, arguments);
                };

                $delegate.warn = function () {
                    pipeline.warn.apply(null, arguments);
                    original.warn.apply(null, arguments);
                };

                $delegate.error = function () {
                    pipeline.error.apply(null, arguments);
                    original.error.apply(null, arguments);
                };

                $delegate.debug = function () {
                    pipeline.debug.apply(null, arguments);
                    original.debug.apply(null, arguments);
                };

                return $delegate;
            }]);

            registry
            .service('nyanLogPipelineService',
            function () {

                var observerCallbacks = [];

                this.register = function (callback) {
                    observerCallbacks.push(callback);
                };

                this.log = function () {
                    var args = arguments;
                    angular.forEach(observerCallbacks, function (callback) {
                        callback.log.apply(null, args);
                    });
                };

                this.info = function () {
                    var args = arguments;
                    angular.forEach(observerCallbacks, function (callback) {
                        callback.info.apply(null, args);
                    });
                };

                this.warn = function () {
                    var args = arguments;
                    angular.forEach(observerCallbacks, function (callback) {
                        callback.warn.apply(null, args);
                    });
                };

                this.error = function () {
                    var args = arguments;
                    angular.forEach(observerCallbacks, function (callback) {
                        callback.error.apply(null, args);
                    });
                };

                this.debug = function () {
                    var args = arguments;
                    angular.forEach(observerCallbacks, function (callback) {
                        callback.debug.apply(null, args);
                    });
                }
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

        function configService(scopeDescriptor, initOptions) {

            initOptions = initOptions || {};

            initOptions.ScopeDescriptor = scopeDescriptor;
            initOptions = merge(initOptions, defaultOptions);

            initOptions.implementFactory = true;
            initOptions.implementService = true;

            requests.push(initOptions);

            return this;
        }

        function configScheduledService(scopeDescriptor, initOptions) {

            initOptions = initOptions || {};

            initOptions.autoRefreshCycleSeconds = 60;

            service(scopeDescriptor, initOptions);

            return this;
        }

        function configModule(scopeDescriptor, initOptions) {

            initOptions = initOptions || {};

            initOptions.ScopeDescriptor = scopeDescriptor;
            initOptions = merge(initOptions, defaultOptions);

            initOptions.implementFactory = true;
            initOptions.implementControllers = true;
            initOptions.implementRoutes = true;
            initOptions.implementService = true;
            initOptions.useSecondaryRestEndpoint = true;

            initOptions.ScopeDescriptor = scopeDescriptor;

            requests.push(initOptions);

            return this;
        }

        //Run-time --------------------------------------------------------------

        function configFactory() {
            return ({
                start: configStart,
                settings: settings
            });
        }

        function configStart() {

            console.log("Starting");

            setupStackServices();

            var instanceOptions = {
                baseServiceUrl: './',

                RestEndpoint: settings.RestEndpoint,
                Identifier: 'id',
                isRestReadOnly: false,

                CollectionBaseController: settings.CollectionBaseController,
                CollectionFactoryName: "{ScopeDescriptor}CollectionFactory",
                CollectionName: '{ScopeDescriptor}',
                MainRestEndpoint: settings.RestEndpoint,
                isArrayRestSource: true,
                CollectionPartialUrl: settings.ScopePartialsStorageUrl + '/' + settings.CollectionPage,

                ModuleServiceName: "{ScopeDescriptor}DataService",

                isApplicationScope: false,
                isGlobalService: false,
                isItemQuery: false,

                ItemBaseController: settings.ItemBaseController,
                ItemFactoryName: '{ScopeDescriptor}ItemFactory',
                SecondaryRestEndpoint: settings.RestEndpoint + '/:id',
                ItemPartialUrl: settings.ScopePartialsStorageUrl + '/' + settings.ItemPage,

                ScopePartialsStorageUrl: settings.ScopePartialsStorageUrl,

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
                useState: false,
                useLocalCache: true
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

                if (initOptions.script) {
                    loadScript3(initOptions.ScopePartialsStorageUrl + '/' + initOptions.script)
                }


            });

            return this;
        }

        function loadScript3(src) {

            var o = src;
            console.log('Loading custom resource file [' + o + ']');
            $.holdReady(true);
            $.getScript(o, function () {
                console.log('Custom resource [' + o + '] loaded.');
                $.holdReady(false);
            });
        };


        function loadScript(url, type, charset) {
            if (type === undefined) type = 'text/javascript';
            if (charset === undefined) charset = 'utf-8';
            if (url) {
                var script = document.querySelector("script[src*='" + url + "']");
                if (!script) {
                    var heads = document.getElementsByTagName("head");
                    if (heads && heads.length) {
                        var head = heads[0];
                        if (head) {
                            script = document.createElement('script');
                            script.setAttribute('src', url);
                            script.setAttribute('type', type);
                            if (charset) script.setAttribute('charset', charset);
                            head.appendChild(script);
                        }
                    }
                }
                return script;
            }
        };
        function loadScript2(url) {

            $http({
                method: 'GET',
                url: url
            }).then(function successCallback(response) {
                console.log(response);
            }, function errorCallback(response) {
                // called asynchronously if an error occurs
                // or server returns response with an error status.
            });
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


            // Collection Factory

            registry
            .factory(initOptions.CollectionFactoryName, function ($resource) {

                var oInterface = {
                    fetch: { method: 'GET', isArray: initOptions.isArrayRestSource, withCredentials: settings.Authenticate }
                };

                return $resource(initOptions.MainRestEndpoint, {}, oInterface);
            });


            // Item Factory


            if (initOptions.useSecondaryRestEndpoint) {
                console.log('Registering Factory ' + initOptions.ItemFactoryName);
                console.log('                 to ' + initOptions.SecondaryRestEndpoint);

                registry
                .factory(initOptions.ItemFactoryName, function ($resource) {

                    var oInterface = {
                        fetch: { method: 'GET', isArray: false, withCredentials: settings.Authenticate },
                        create: { method: 'PUT', withCredentials: settings.Authenticate },
                        update: { method: 'POST', withCredentials: settings.Authenticate },
                        delete: { method: 'DELETE', params: { id: '@id' }, withCredentials: settings.Authenticate }
                    };

                    return $resource(initOptions.SecondaryRestEndpoint, {}, oInterface);
                });
            }

            var dataItemServiceName = (initOptions.useSecondaryRestEndpoint ? initOptions.ItemFactoryName : 'NullItemFactory');

            console.log('Registering Service ' + initOptions.ModuleServiceName);
            console.log('         Collection ' + initOptions.CollectionFactoryName);
            console.log('               Item ' + dataItemServiceName);

            // Data Service

            registry
            .service(initOptions.ModuleServiceName, [
            initOptions.CollectionFactoryName,
            dataItemServiceName,
            '$log',
            '$timeout',
            function (collectionFactory, itemFactory, $log, $timeout) {

                var observerCallbacks = [];

                var that = this;
                var init = false;
                var _schedule = null;

                this.data = [];

                this.status = 'Initializing';

                this.storageDescriptor = configuration.appRootUrl + initOptions.ScopeDescriptor;

                this.register = function (callback) {
                    observerCallbacks.push(callback);

                    if (init)
                        callback(that.data);
                };

                var notifyObservers = function () {

                    //$timeout: avoid $scope.$apply pitfalls.
                    $timeout(function () {
                        angular.forEach(observerCallbacks, function (callback) {
                            callback(that.data);
                        });
                    });
                };

                this.remove = function (id, successCallback, failCallback) {

                    $log.log(initOptions.ModuleServiceName + ': DELETE ' + id);

                    this.status = 'Removing';

                    itemFactory.delete(
                    { id: id },
                    function (data) {
                        $log.log('itemFactory DELETE SUCCESS');

                        var _index = that.data.map(function (x) { return x[initOptions.Identifier]; }).indexOf(id);
                        that.data.splice(_index, 1);

                        that.status = false;

                        notifyObservers();
                        successCallback(data);
                    },
                    function (data) {
                        $log.log('itemFactory DELETE FAIL =(');

                        that.status = false;

                        failCallback(data);
                    }
                    );
                }

                this.save = function (oData, successCallback, failCallback) {
                    $log.log(initOptions.ModuleServiceName + ': SAVE ' + oData[initOptions.Identifier]);

                    that.status = 'Saving';

                    console.log(" SAVE ITEM");

                    itemFactory.update(
                    oData,
                    function (data) {
                        if (oData[initOptions.Identifier] != 0) {
                            var _index = that.data.map(function (x) { return x[initOptions.Identifier]; }).indexOf(data.id);
                            that.data[_index] = data;
                        } else {
                            that.data.push(data);
                            //factoryGet();
                        }

                        that.status = false;

                        notifyObservers();

                        successCallback(data);
                    },
                    function (data) {

                        that.status = false;

                        failCallback(data);
                    }
                    );

                };

                this.factory = {
                    fetch: itemFactory.fetch,
                    delete: itemFactory.delete,
                    update: itemFactory.update,
                    create: itemFactory.create
                }

                this.refresh = function () {
                    factoryGet();
                }

                function factoryScheduledGet() {
                    factoryGet(true);
                }

                function factoryGet(pScheduled) {

                    console.log(initOptions.ModuleServiceName + ': GET' + (pScheduled ? " (SCHED)" : ""));

                    that.status = 'Loading';

                    that.data = collectionFactory.fetch(function (data) {

                        data = angular.fromJson(angular.toJson(data));

                        if (initOptions.collectionPostProcessing) {
                            $log.log(initOptions.ModuleServiceName + ': Collection Post Processing');
                            data = initOptions.collectionPostProcessing(data);
                        }

                        that.status = false;

                        if (initOptions.useLocalCache) {
                            $log.log(initOptions.ModuleServiceName + ': LocalStorage <= Data');
                            localforage.setItem(that.storageDescriptor, data);
                        }

                        setData(data);
                    });
                }

                function setData(data) {

                    init = true;
                    that.data = data;
                    notifyObservers();
                }

                //Service bootstrap

                //Set schedule, if required. 

                if (initOptions.autoRefreshCycleSeconds) {
                    startSchedule(initOptions.autoRefreshCycleSeconds);
                }

                this.startSchedule = function (pSecs) {
                    _schedule = setInterval(function () { factoryGet(true); }, pSecs * 1000);
                    console.log(initOptions.ModuleServiceName + ': SCHED ' + pSecs + 's');
                }

                this.stopSchedule = function () {
                    _schedule.stop();
                    console.log(initOptions.ModuleServiceName + ': SCHED STOP');
                }

                //Initial load

                if (initOptions.useLocalCache) {

                    that.status = 'Loading';

                    localforage.getItem(that.storageDescriptor)
                        .then(function (value) {
                            if (value) {
                                $log.log(initOptions.ModuleServiceName + ': LocalStorage => Data');

                                setData(value);
                            }
                        });

                    that.status = false;


                    setTimeout(factoryScheduledGet, 1000);
                } else {
                    factoryGet();
                }

                return this;
            }]);

            // Look-up Factory

            if (initOptions.useLookupQuery) {

                console.log('Registering Factory ' + initOptions.LookupQueryFactory);
                console.log('                 to ' + initOptions.LookupQueryRestEndpoint);

                registry.factory(initOptions.LookupQueryFactory, function ($resource) {
                    return $resource(initOptions.LookupQueryRestEndpoint, {}, {
                        query: { method: 'GET', isArray: false }
                    });
                });
            }

            // Locator Factory

            if (initOptions.useLocatorQuery) {
                console.log('Registering Factory ' + initOptions.LocatorQueryFactory);
                console.log('                 to ' + initOptions.LocatorRestEndpoint);

                registry.factory(initOptions.LocatorQueryFactory, function ($resource) {
                    return $resource(initOptions.LocatorRestEndpoint, {}, {
                        query: { method: 'GET', isArray: false }
                    });
                });

                console.log('Registering Service ' + initOptions.LocatorServiceName);

                // Locator Service

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

            initOptions.CollectionController = initOptions.CollectionController ? initOptions.CollectionController : initOptions.CollectionBaseController;

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

            initOptions.ItemController = initOptions.ItemController ? initOptions.ItemController : initOptions.ItemBaseController;


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

            //Collection Control
            console.log('Creating controller ' + initOptions.CollectionBaseController);

            registry.controller(
            initOptions.CollectionBaseController, [
            '$scope',
            '$state',
            '$stateParams',
            '$filter',
            '$log',
            initOptions.ModuleServiceName,
            function ($scope, $state, $stateParams, $filter, $log, dataService) {

                $log.log('Starting ' + initOptions.CollectionBaseController + ' instance');

                $scope.Stack = {
                    Service: dataService,
                    Options: initOptions
                }

                $scope.data = [];

                $scope.selectedId = 0;

                if ($state.params)
                    $scope.selectedId = $state.params.id;

                $scope.select = function (item) {
                    $scope.selectedId = item[initOptions.Identifier];
                    $state.go(initOptions.StateBase + '.detail', { id: $scope.selectedId });
                };

                $scope.new = function () {
                    $scope.selectedId = 0;
                    $state.go(initOptions.StateBase + '.new');
                };

                dataService.register(function (data) {
                    $scope.data = data;
                });

                $scope.refresh = function () {
                    dataService.refresh();
                };
            }
            ]);


            //Item Control
            console.log('Creating controller ' + initOptions.ItemBaseController);

            registry.controller(
            initOptions.ItemBaseController, [
            '$scope',
            '$state',
            '$stateParams',
            '$log',
            '$q',
            initOptions.ModuleServiceName,
            function ($scope, $state, $stateParams, $log, $q, dataService) {

                $log.log('Starting ' + initOptions.ItemBaseController + ' instance');

                $scope.State = {};

                $scope.item = {};

                //console.log('    Controller \'' + initOptions.baseItemBaseController + '\' invoked');


                // SAVE
                // 1) Handle OnBeforeSave (if present)
                $scope.save = function () {

                    var data = $scope.item;

                    if ($scope.onBeforeSave)
                        $q.when($scope.onBeforeSave(data)).then(function () { $scope.onHandlePreSave(data); });
                    else
                        $scope.onHandlePreSave(data);
                };

                // 1) Handle events just before saving
                $scope.onHandlePreSave = function (data) {

                    dataService.save(data,
                    function (procData) {
                        if ($scope.onAfterSave)
                            $q.when($scope.onAfterSave(procData)).then(function () { $scope.onHandlePostSave(procData); });
                        else
                            $scope.onHandlePostSave(procData);
                    },
                    function (e) {
                        $log.warn($scope.Stack.Options.collectionName, e.data.ExceptionMessage);
                    }
                    );
                };

                // 1) Handle events just after saving
                $scope.onHandlePostSave = function (data) {

                    $log.info($scope.Stack.Options.collectionName, "Item saved.");
                    $scope.selectedId = data[initOptions.Identifier];
                    $state.go(initOptions.StateBase + '.detail', { id: $scope.selectedId }, { reload: true });
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

                        $log.info($scope.Stack.Options.collectionName, "Item " + id + " successfully removed.");

                        $scope.selectedId = 0;
                        $state.go(initOptions.StateBase, null, { reload: true });
                    },
                    function (e) {
                        $log.warn($scope.Stack.Options.collectionName, "An exception occurred while removing this item: " + e.data.ExceptionMessage);
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


                        dataService.factory.fetch(
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

                //console.log('    Controller \'' + initOptions.baseItemBaseController + '\' finished loading');
            }
            ]);
        }
    }
    );
}
)(window, window.angular);