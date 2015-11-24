(function (window, angular) {

    'use strict';

    var nyanStackModule = angular.module('ngNyanStack', ['ngResource', 'toaster', 'ngTable']);

    nyanStackModule
    .provider(
        "nyanStack",
            function provideNyan($stateProvider, $controllerProvider, $compileProvider, $filterProvider, $provide) {

                //Setup and primitives

                var settings = {
                    RESTPrefix: "data",
                    ScopePrefix: "ng/scopes/{ScopeDescriptor}",

                    CollectionPage: '{ScopeDescriptor}-collection.html',
                    ItemPage: '{ScopeDescriptor}-item.html',

                    CollectionController: '{ScopeDescriptor}CollectionCtrl',
                    ItemController: '{ScopeDescriptor}ItemCtrl',

                    RESTEndpoint: '{RESTPrefix}/{ScopeDescriptor}s',

                    BaseServiceUrl: './',

                    PluralDescriptor: '{ScopeDescriptor}s',

                    AppName: '(none)'
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
                            baseCollectionController: '{ScopeDescriptor}ListBaseCtrl',
                            baseItemController: '{ScopeDescriptor}ItemBaseCtrl',
                            baseServiceUrl: './',

                            RESTEndpoint: settings.RESTEndpoint,

                            CollectionController: settings.CollectionController,
                            CollectionFactoryName: "{ScopeDescriptor}CollectionFactory",
                            CollectionName: '{ScopeDescriptor}',
                            CollectionRESTEndpoint: settings.RESTEndpoint,
                            CollectionRESTHandler: '{BaseServiceUrl}' + settings.RESTEndpoint,
                            CollectionPartialUrl: settings.ScopePrefix + '{ScopeDescriptor}/' + settings.CollectionPage,

                            customCodeFiles: [],

                            globalFactory: "{ScopeDescriptor}GlobalFactory",
                            globalService: "{ScopeDescriptor}GlobalService",
                            globalFactoryArrayOutput: true,

                            isApplicationScope: true,
                            isGlobalService: false,
                            isItemQuery: false,

                            ItemController: settings.ItemController,
                            ItemFactoryName: "{ScopeDescriptor}ItemFactory",
                            ItemRESTEndpoint: settings.RESTEndpoint,
                            ItemRESTHandler: '{BaseServiceUrl}' + settings.RESTEndpoint,
                            ItemPartialUrl: settings.ScopePrefix + '{ScopeDescriptor}/' + settings.ItemPage,

                            LocatorQueryFactory: "{ScopeDescriptor}LocatorFactory",
                            LocatorRESTEndpoint: '{RESTEndpoint}/bylocator/:id',
                            LocatorServiceName: '{ScopeDescriptor}LocatorService',

                            LookupQueryFactory: "{ScopeDescriptor}QueryFactory",
                            LookupQueryRESTEndpoint: '{RESTEndpoint}/slookup/:term',

                            PaginationMode: 'Auto',

                            refreshCycleSeconds: 0,
                            RootPrefix: settings.RootPrefix,
                            serviceName: '{ScopeDescriptor}Service',
                            state: '{RootPrefix}/' + settings.PluralDescriptor,
                            stateRoute: '/' + '{RootPrefix}/' + settings.PluralDescriptor,
                            useCollectionController: true,
                            useCollectionRESTEndpoint: true,
                            useController: true,
                            useItemController: true,
                            useItemRESTEndpoint: true,
                            useItemRoute: true,
                            useListRoute: true,
                            useLocatorQuery: true,
                            useLookupQuery: true,
                            useReferenceRESTService: false,
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
                                        probe = probe.split("{RESTEndpoint}").join(settings.RESTEndpoint);
                                        probe = probe.split("{RESTPrefix}").join(settings.RESTPrefix);
                                        probe = probe.split("{RootPrefix}").join(initOptions.RootPrefix);
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

                    console.log('Registering Factory ' + initOptions.globalFactory);

                    registry
                        .factory(initOptions.globalFactory, function ($resource) {
                            return $resource(initOptions.RESTEndpoint, {}, {
                                fetch: { method: 'GET', isArray: initOptions.globalFactoryArrayOutput, withCredentials: true }
                            } );
                        });

                    if (initOptions.useLookupQuery) {

                        console.log('Registering Factory ' + initOptions.LookupQueryFactory);

                        registry.factory(initOptions.LookupQueryFactory, function ($resource) {
                            return $resource(initOptions.LookupQueryRESTEndpoint, {}, {
                                query: { method: 'GET', isArray: false }
                            });
                        });
                    }

                    if(initOptions.useLocatorQuery) {
                        console.log ( 'Registering Factory ' + initOptions.LocatorQueryFactory );

                        registry.factory ( initOptions.LocatorQueryFactory, function($resource) {
                            return $resource ( initOptions.LocatorRESTEndpoint, {}, {
                                query: { method: 'GET', isArray: false }
                            } );
                        } );

                        console.log('Registering Service ' + initOptions.LocatorServiceName);

                        registry.service(initOptions.LocatorServiceName,
                        [
                            initOptions.LocatorQueryFactory,
                            '$cacheFactory',
                            '$q',
                            function(locatorFactory, $cacheFactory, $q) {

                                console.log ( "Initializing " + initOptions.serviceName + " for " + initOptions.LocatorQueryFactory );

                                var cache = $cacheFactory ( 'cache.' + initOptions.serviceName );
                                var that = this;
                                var config = initOptions;

                                this.state = {
                                    isLoading: false,
                                    hasFailed: false,
                                    hasOldData: false
                                };

                                this.getData = function(key) {

                                    that.state.isLoading = true;
                                    console.log ( 'fetching ' + key );
                                    var probe = cache.get ( key );

                                    if(probe) {
                                        clearState();
                                        return probe;
                                    }
                                    var deferred = $q.defer();

                                    locatorFactory.query ( { id: key },
                                        function(data) {
                                            cache.put ( key, data );
                                            deferred.resolve ( data );
                                            clearState();
                                        },
                                        function(error) {
                                            that.state.isLoading = false;
                                            that.state.hasFailed = true;
                                            deferred.reject ( error.statusText );
                                        } );

                                    return deferred.promise;
                                };

                                function clearState() {
                                    that.state.isLoading =
                                        that.state.hasFailed = false;
                                }
                            }
                        ] );

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