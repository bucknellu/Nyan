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
                        service: $provide.service
                    };

                var defaultOptions = {
                    implementFactory: false,
                    implementControllers: false,
                    implementRoutes: false,
                    implementService: false
                };

                var provider = this;

                return ({
                    setup: setup,
                    service: service,
                    module: module,
                    $get: instantiateFactory
                });


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
                function setup(uSettings) {
                    settings = merge(uSettings, settings);
                    return this;
                }

                function service(ScopeDescriptor, initOptions) {

                    initOptions.ScopeDescriptor = ScopeDescriptor;
                    initOptions = merge(initOptions, defaultOptions);

                    initOptions.implementFactory = true;
                    initOptions.implementService = true;

                    requests.push(initOptions);

                    return this;
                }

                function module(ScopeDescriptor, initOptions) {

                    initOptions.ScopeDescriptor = ScopeDescriptor;
                    initOptions = merge(initOptions, defaultOptions);

                    initOptions.implementFactory = true;
                    initOptions.implementControllers = true;
                    initOptions.implementRoutes = true;
                    initOptions.implementService = true;

                    initOptions.ScopeDescriptor = ScopeDescriptor;

                    initOptions = initOptions || {};

                    requests.push(initOptions);

                    return this;
                }


                //Run-time --------------------------------------------------------------

                function instantiateFactory() {
                    return ({
                        start: start
                    });
                    function start() {

                        console.log("Starting!");

                        var instanceOptions = {
                            baseServiceUrl: './',

                            RestEndpoint: settings.RestEndpoint,
                            isRestReadOnly: false,

                            CollectionController: settings.CollectionController,
                            CollectionFactoryName: "{ScopeDescriptor}CollectionFactory",
                            CollectionName: '{ScopeDescriptor}',
                            MainRestEndpoint: settings.RestEndpoint,
                            CollectionPartialUrl: settings.ScopePartialsStorageUrl + '{ScopeDescriptor}/' + settings.CollectionPage,

                            ModuleServiceName: "{ScopeDescriptor}DataService",

                            customCodeFiles: [],

                            MainRestArrayOutput: true,

                            isApplicationScope: true,
                            isGlobalService: false,
                            isItemQuery: false,

                            ItemController: settings.ItemController,
                            ItemFactoryName: '{ScopeDescriptor}ItemFactory',
                            SecondaryRestEndpoint: settings.RestEndpoint + '/:id',
                            ItemPartialUrl: settings.ScopePartialsStorageUrl + '{ScopeDescriptor}/' + settings.ItemPage,

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
                            state: '{StatePrefix}/' + settings.PluralDescriptor,
                            stateRoute: '/' + '{StatePrefix}/' + settings.PluralDescriptor,
                            StatePrefix: settings.StatePrefix,
                            useCollectionController: true,
                            useMainRestEndpoint: true,
                            useController: true,
                            useItemController: true,
                            useSecondaryRestArrayOutput: true,
                            useItemRoute: true,
                            useListRoute: true,
                            useLocatorQuery: true,
                            useLookupQuery: true,
                            useReferenceRestService: false,
                            useResource: true,
                            useState: true
                        };

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
                        });

                        return this;
                    }
                }

                function prepareFactories(initOptions) {

                    console.log('Registering Factory ' + initOptions.CollectionFactoryName);
                    console.log('                 to ' + initOptions.MainRestEndpoint);

                    registry
                        .factory(initOptions.CollectionFactoryName, function ($resource) {

                            var oInterface = {
                                fetch: { method: 'GET', isArray: initOptions.MainRestArrayOutput, withCredentials: settings.Authenticate }
                            };

                            if (!initOptions.isRestReadOnly) {
                                oInterface.create = { method: 'POST', withCredentials: settings.Authenticate }
                            }

                            return $resource(initOptions.MainRestEndpoint, {}, oInterface);
                        });

                    console.log('Registering Factory ' + initOptions.ItemFactoryName);
                    console.log('                 to ' + initOptions.SecondaryRestEndpoint);

                    registry
                        .factory(initOptions.ItemFactoryName, function ($resource) {

                            var oInterface = {
                                fetch: { method: 'GET', isArray: initOptions.MainRestArrayOutput, withCredentials: settings.Authenticate }
                            };

                            if (!initOptions.isRestReadOnly) {
                                oInterface.create = { method: 'PUT', withCredentials: settings.Authenticate }
                                oInterface.update = { method: 'POST', params: { id: '@id' }, withCredentials: settings.Authenticate },
                                oInterface.delete = { method: 'DELETE', params: { id: '@id' }, withCredentials: settings.Authenticate }
                            }

                            return $resource(initOptions.SecondaryRestEndpoint, {}, oInterface);
                        });

                    console.log('Registering Service ' + initOptions.ModuleServiceName);

                    registry
                        .service(initOptions.ModuleServiceName, [
                            initOptions.CollectionFactoryName,
                            initOptions.ItemFactoryName,
                            function (collectionFactory, itemFactory) {

                                var observerCallbacks = [];

                                var that = this;
                                var _init = false;

                                this.data = [];

                                this.register = function (callback) {
                                    observerCallbacks.push(callback);
                                    callback();
                                };

                                var notifyObservers = function () {
                                    angular.forEach(observerCallbacks, function (callback) {
                                        callback();
                                    });
                                };

                                this.remove = function (id) {
                                    console.log('itemFactory DELETE');

                                    itemFactory.delete({ id: id },
                                        function (data) {
                                            console.log('itemFactory DELETE SUCCESS');
                                            factoryGet();
                                        },
                                        function (data) {
                                            console.log('itemFactory DELETE FAIL =(.');
                                        }
                                        );
                                }

                                function factoryGet() {
                                    console.log('collectionFactory GET');
                                    _init = true;
                                    that.data = collectionFactory.fetch();
                                    notifyObservers();
                                }

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