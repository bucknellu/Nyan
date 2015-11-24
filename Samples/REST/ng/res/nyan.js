(function (window, angular, undefined) {

    'use strict';

    var ngNyanStackReference = angular.module('ngNyanStack', ['ng']);

    ngNyanStackReference.provider(
            "$nyanStackNg",
            function ProvideNyan() {
                var Setup = {
                    RootPrefix: "framework",
                    pagePathPrefix: "ng/scopes/{ScopeDescriptor}"
                };
                return ({
                    getGreeting: getGreeting,
                    setGreeting: setGreeting,
                    $get: instantiateGreeter
                });

                function getGreeting() {
                    return (greeting);
                }
                function setGreeting(newGreeting) {
                    greeting = newGreeting;
                }

                function instantiateGreeter() {
                    return ({
                        greet: greet
                    });
                    function greet(name) {
                        return (
                            greeting.replace(
                                /%s/gi,
                                function interpolateName($0) {
                                    return (($0 === "%s") ? name : ucase(name));
                                }
                            )
                        );
                    }
                    function ucase(name) {
                        return ((name || "").toUpperCase());
                    }
                }
            }
        );



















    ngNyanStackReference.provider('$nyanStack', function ($stateProvider, $controllerProvider, $provider) {

        var provider = this;

        this.valueOrDefault = function (pValue, pDefault) {
            return (typeof pValue === 'undefined' ? pDefault : pValue);
        };

        this.registerAll = function () {
            var queue = ngNyanStackReference._invokeQueue;
            for (var i = 0; i < queue.length; i++) {
                var call = queue[i];

                console.log(call);

                if (call[1] == "register") {

                    var controllerName = call[2][0];

                    if (call[0] == "$controllerProvider")
                        $controllerProvider.register(controllerName, call[2][1]);
                }
            }
        }

        provider.setup = function (parms) {
            parms = parms || {};
            for (var i in parms) {
                provider.Parms[i] = parms[i];
            }

            return this;
        };

        provider.Parms = {
            RootPrefix: "framework",
            pagePathPrefix: "ng/scopes/{ScopeDescriptor}"
        };


        this.prepareCachedService = function (scopeDescriptor, initOptions) {

            initOptions.serviceName = initOptions.serviceName || scopeDescriptor + 'Service';
            initOptions.restEndpoint = initOptions.restEndpoint || "";

            initOptions.isItemQuery = valueOrDefault(initOptions.itemQuery, false);
            initOptions.isGlobalService = valueOrDefault(initOptions.isGlobalService, false);

            initOptions.useLookupQuery = valueOrDefault(initOptions.useLookupQuery, true);
            initOptions.useLocatorQuery = valueOrDefault(initOptions.useLocatorQuery, true);

            initOptions.LookupQueryFactory = initOptions.LookupQueryFactory || scopeDescriptor + "QueryFactory";
            initOptions.LocatorQueryFactory = initOptions.LocatorQueryFactory || scopeDescriptor + "LocatorFactory";

            initOptions.LookupQueryUrl = initOptions.LookupQueryUrl || initOptions.restEndpoint + '/slookup/:term';
            initOptions.LocatorQueryUrl = initOptions.LocatorQueryUrl || initOptions.restEndpoint + '/bylocator/:id';

            initOptions.globalFactory = initOptions.globalFactory || scopeDescriptor + "GlobalFactory";
            initOptions.globalService = initOptions.globalService || scopeDescriptor + "GlobalService";

            initOptions.isApplicationScope = valueOrDefault(initOptions.isApplicationScope, true);
            initOptions.refreshCycleSeconds = initOptions.refreshCycleSeconds || 0;

            if (initOptions.isItemQuery) {

                if (initOptions.useLookupQuery) {
                    $provider.factory(initOptions.LookupQueryFactory, function ($resource) {
                        return $resource(initOptions.LookupQueryUrl, {}, {
                            query: { method: 'GET', isArray: false }
                        });
                    });
                }

                if (initOptions.useLocatorQuery) {
                    $provider.factory(initOptions.LocatorQueryFactory, function ($resource) {
                        return $resource(initOptions.LocatorQueryUrl, {}, {
                            query: { method: 'GET', isArray: false }
                        });
                    });
                }

                ngNyanStackReference.service(initOptions.serviceName,
                    [
                        initOptions.LocatorQueryFactory,
                        '$cacheFactory',
                        '$q',
                        function (locatorFactory, $cacheFactory, $q) {
                            var cache = $cacheFactory('cache.' + initOptions.serviceName);
                            var keys = [];
                            var that = this;

                            this.state = {
                                isLoading: false,
                                hasFailed: false,
                                hasOldData: false
                            };

                            this.getData = function (key) {
                                that.state.isLoading = true;
                                //console.log('fetching ' + key);
                                var probe = cache.get(key);

                                if (probe) {
                                    clearState();
                                    return probe;
                                }
                                var deferred = $q.defer();

                                locatorFactory.fetch({ id: key },
                                    function (data) {
                                        cache.put(key, data);
                                        deferred.resolve(data);
                                        clearState();
                                    },
                                    function (error) {
                                        that.state.isLoading = false;
                                        that.state.hasFailed = true;
                                        frameworkDiagnosticsService.log.add(error);
                                        deferred.reject(error.statusText);
                                    });

                                //console.log(initOptions.serviceName + ': returning promise for ' + key);
                                return deferred.promise;
                            };

                            function clearState() {
                                that.state.isLoading =
                                    that.state.hasFailed = false;
                            }
                        }
                    ])
                    .run(function (appService) {
                        console.log('appService: Start');
                    });
            }

            if (initOptions.isGlobalService) {

                console.log('Cached Service[' + scopeDescriptor + ']: Factory [' + initOptions.globalFactory + ']');
                console.log('Cached Service[' + scopeDescriptor + ']: Service [' + initOptions.globalService + ']');
                $provider.factory(initOptions.globalFactory, function ($resource) {
                    return $resource(initOptions.restEndpoint, {}, {
                        fetch: { method: 'GET', isArray: initOptions.globalFactoryArrayOutput, withCredentials: true }
                    });
                })
                    .service(initOptions.globalService,
                    [
                        initOptions.globalFactory,
                        '$log',
                        '$q',
                        '$rootScope',
                        '$timeout',
                        'frameworkConfigService',
                        'frameworkDiagnosticsService',
                        function (globalFactory, $log, $q, $rootScope, $timeout, frameworkConfigService, frameworkDiagnosticsService) {

                            $log.info('Cached Service[' + scopeDescriptor + ']: [' + initOptions.globalService + '] Start');

                            this.serviceName = initOptions.globalService;

                            var that = this;
                            this.data = [];
                            this.AnnouncementTicker = 'global-update:' + initOptions.globalFactory;
                            this.state = {
                                isLoading: false,
                                hasFailed: false
                            };

                            this.setData = function (parm) {
                                this.data = parm;
                                $rootScope.$broadcast(this.AnnouncementTicker);
                            }

                            var config = function () {

                                var storageDescriptor = scopeDescriptor;
                                var appCode = frameworkConfigService.appRootUrl;

                                if (initOptions.isApplicationScope)
                                    if (typeof appCode === 'undefined')
                                        return;
                                    else
                                        storageDescriptor = appCode + scopeDescriptor;

                                $log.info('Cached Service[' + scopeDescriptor + ']: [' + initOptions.globalService + '] Config');

                                localforage.getItem(storageDescriptor).then(function (value) {
                                    if (value) {
                                        $log.info('Cached Service[' + scopeDescriptor + ']: LocalStorage => Data');
                                        that.setData(value);
                                    }
                                });

                                that.fetchData = function () {
                                    //Wait for a valid Session state before start hammering the server with requests.
                                    if (!frameworkConfigService.isOperational)
                                        return;

                                    if (that.state.isLoading)
                                        return;

                                    that.state.isLoading = true;
                                    var deferred = $q.defer();
                                    globalFactory.fetch(
                                        function (data) {
                                            data = JSON.parse(JSON.stringify(data));

                                            if (initOptions.postProcessing) {
                                                $log.info('Cached Service[' + scopeDescriptor + ']: PostProcessing');
                                                data = initOptions.postProcessing(data);
                                            }

                                            $log.info('Cached Service[' + scopeDescriptor + ']: Data => LocalStorage');
                                            localforage.setItem(storageDescriptor, data);

                                            $log.info('Cached Service[' + scopeDescriptor + ']: [Broadcast]');
                                            that.setData(data);
                                            clearState();
                                            deferred.resolve(data);
                                        },
                                        function (error) {
                                            that.state.isLoading = false;
                                            that.state.hasFailed = true;

                                            //only if there was an error loading the new data do we want to expose this information to the ui
                                            if (that.data) {
                                                that.state.hasOldData = true;
                                            }

                                            $log.info(error);
                                            frameworkDiagnosticsService.log.add(error);
                                            deferred.reject(error.statusText);
                                        }
                                    );

                                    if (initOptions.refreshCycleSeconds) {
                                        $log.info('Cached Service[' + scopeDescriptor + ']: Scheduling next load (' + initOptions.refreshCycleSeconds + ' seconds)');

                                        $timeout(function () {
                                            $log.info('Cached Service[' + scopeDescriptor + ']: Starting scheduled load @ ' + Date().format("dd/M/yy h:mm tt"));
                                            that.fetchData();
                                        }, initOptions.refreshCycleSeconds * 1000);
                                    }

                                    return deferred.promise;
                                };

                                $timeout(function () {
                                    that.fetchData();
                                }, 1);
                            }

                            config();

                            function clearState() {
                                that.state.isLoading =
                                    that.state.hasFailed =
                                    that.state.hasOldData = false;
                            }

                            if (initOptions.isApplicationScope) {
                                $rootScope.$on('global-update:configuration', function () {
                                    config();
                                });
                            }
                        }
                    ])
                    .run([
                        initOptions.globalService, function (service) {
                            console.log('Cached Service[' + scopeDescriptor + ']: Booting up');
                        }
                    ]);
            }

            return this;
        }

        this.prepareSimpleState = function (scopeDescriptor, initOptions) {

            initOptions.useResource = provider.valueOrDefault(initOptions.useResource, false);
            initOptions.useController = provider.valueOrDefault(initOptions.useController, false);

            initOptions.useItemController = provider.valueOrDefault(initOptions.useItemController, false);
            initOptions.useItemRESTService = provider.valueOrDefault(initOptions.useItemRESTService, false);

            initOptions.useListController = provider.valueOrDefault(initOptions.useListController, false);
            initOptions.useListRESTService = provider.valueOrDefault(initOptions.useListRESTService, false);

            initOptions.useListRoute = provider.valueOrDefault(initOptions.useListRoute, false);
            initOptions.useState = provider.valueOrDefault(initOptions.useState, false);

            return this.prepare(scopeDescriptor, initOptions);
        };

        this.prepare = function (scopeDescriptor, initOptions) {

            console.log('provider: Initializing [' + scopeDescriptor + ']');

            var init = function () {
                //Handling defaults

                initOptions = initOptions || {};

                initOptions.collectionName = initOptions.collectionName || scopeDescriptor;
                initOptions.pluralDescriptor = initOptions.pluralDescriptor || scopeDescriptor + "s";

                initOptions.baseServiceUrl = initOptions.baseServiceUrl || './';

                initOptions.useState = provider.valueOrDefault(initOptions.useState, true);
                initOptions.useResource = provider.valueOrDefault(initOptions.useResource, true);
                initOptions.useController = provider.valueOrDefault(initOptions.useController, true);

                initOptions.RootPrefix = initOptions.RootPrefix || provider.Parms.RootPrefix;

                initOptions.ItemFactoryName = initOptions.ItemFactoryName || "{ScopeDescriptor}ItemFactory";
                initOptions.ReferenceFactoryName = initOptions.ReferenceFactoryName || "{ScopeDescriptor}RefFactory";
                initOptions.CollectionFactoryName = initOptions.CollectionFactoryName || "{ScopeDescriptor}CollectionFactory";

                initOptions.listRESTservice = initOptions.listRESTservice || '{RootPrefix}/' + initOptions.pluralDescriptor;
                initOptions.itemRESTservice = initOptions.itemRESTservice || '{RootPrefix}/' + initOptions.pluralDescriptor;
                initOptions.referenceRESTservice = initOptions.referenceRESTservice || '{RootPrefix}.' + initOptions.pluralDescriptor;

                initOptions.useListRESTService = provider.valueOrDefault(initOptions.useListRESTService, true);
                initOptions.useItemRESTService = provider.valueOrDefault(initOptions.useItemRESTService, true);
                initOptions.useReferenceRESTService = provider.valueOrDefault(initOptions.useReferenceRESTService, false);

                initOptions.state = initOptions.state || '{RootPrefix}-' + initOptions.pluralDescriptor;
                initOptions.stateRoute = initOptions.stateRoute || '/' + initOptions.state;

                initOptions.useListRoute = provider.valueOrDefault(initOptions.useListRoute, true);
                initOptions.useItemRoute = provider.valueOrDefault(initOptions.useItemRoute, true);

                initOptions.pagePath = initOptions.pagePath || provider.Parms.pagePathPrefix;
                initOptions.listPage = (initOptions.listPage || '/{ScopeDescriptor}-collection.html');
                initOptions.itemPage = (initOptions.itemPage || '/{ScopeDescriptor}-item.html');

                initOptions.baseListController = initOptions.baseListController || '{ScopeDescriptor}CollectionBaseCtrl';
                initOptions.baseItemController = initOptions.baseItemController || '{ScopeDescriptor}ItemBaseCtrl';

                initOptions.useListController = provider.valueOrDefault(initOptions.useListController, true);
                initOptions.useItemController = provider.valueOrDefault(initOptions.useItemController, true);

                initOptions.customCodeFiles = initOptions.customCodeFiles || [];

                initOptions.PaginationMode = initOptions.PaginationMode || 'Auto';

                //Now, replace all mask values for the proper scope descriptor.
                for (var key in initOptions) {
                    var probe = initOptions[key];

                    if (typeof probe == 'string') {
                        if (probe.indexOf('{') != -1) {
                            probe = probe.split("{RootPrefix}").join(initOptions.RootPrefix);
                            probe = probe.split("{ScopeDescriptor}").join(scopeDescriptor);
                            initOptions[key] = probe;
                        }
                    }
                };

                //Pre-compile some statements, based on user choice

                initOptions.preCompItemController = initOptions.itemController || initOptions.baseItemController;
                initOptions.preCompListController = initOptions.listController || initOptions.baseListController;

                initOptions.preCompListRESTServiceHandler = initOptions.baseServiceUrl + initOptions.listRESTservice;
                initOptions.preCompItemRESTServiceHandler = initOptions.baseServiceUrl + initOptions.itemRESTservice;
                initOptions.preCompReferenceRESTServiceHandler = initOptions.baseServiceUrl + initOptions.referenceRESTservice;

                initOptions.preCompListUrl = initOptions.pagePath + initOptions.listPage;
                initOptions.preCompItemUrl = initOptions.pagePath + initOptions.itemPage;

                //if table will be sorted, consumers need to specify a sort column
                initOptions.tableOptions = {
                    sortTable: provider.Parms.sortTable,
                    defaultSortColumn: provider.Parms.defaultSortColumn,
                    defaultSortDirection: provider.Parms.defaultSortDirection
                };

                //Now let's compile!

                scopeDependenciesInit();

                if (initOptions.useState)
                    scopeStateInit();
                if (initOptions.useResource)
                    scopeResourceInit();
                if (initOptions.useController)
                    scopeControllerInit();
            }

            var scopeDependenciesInit = function () {
                if (initOptions.customCodeFiles.length != 0) {

                    for (var i = 0; i < initOptions.customCodeFiles.length; i++) {
                        var o = initOptions.pagePath + '/' + initOptions.customCodeFiles[i] + '?$$FWVER$$';
                        console.log('Loading custom resource file: ' + o);

                        $.holdReady(true);
                        $.getScript(o, function () {
                            console.log('Custom resource [' + o + '] loaded.');
                            $.holdReady(false);
                        });
                    }
                }
            };

            var scopeResourceInit = function () {
                //Services
                console.log('  Initializing mappings for \'' + initOptions.preCompListRESTServiceHandler + '\' (RESTful endpoint)');

                if (initOptions.useListRESTService) {

                    $provider.factory(initOptions.CollectionFactoryName, function ($resource) {
                        return $resource(initOptions.preCompListRESTServiceHandler, {}, {
                            fetch: { method: 'GET', isArray: true },
                            create: { method: 'POST' }
                        });
                    });

                    console.log('    \'' + initOptions.CollectionFactoryName + '\' (Collection Factory) mapped to \'' + initOptions.preCompListRESTServiceHandler + '\'');
                }

                if (initOptions.useItemRESTService) {

                    $provider.factory(initOptions.ItemFactoryName, function ($resource) {
                        return $resource(initOptions.preCompItemRESTServiceHandler + '/:id', {}, {
                            fetch: { method: 'GET' },
                            update: { method: 'POST', params: { id: '@id' } },
                            delete: { method: 'DELETE', params: { id: '@id' } }
                        });
                    });

                    console.log('    \'' + initOptions.ItemFactoryName + '\' (Item Factory) mapped to \'' + initOptions.preCompItemRESTServiceHandler + '\'');

                }

                if (initOptions.useReferenceRESTService) {

                    $provider.factory(initOptions.ReferenceFactoryName, function ($resource) {
                        return $resource(initOptions.preCompReferenceRESTServiceHandler + '/:id/:collection', {}, {
                            fetch: { method: 'GET', params: { id: '@id', collection: '@collection' }, isArray: true }
                        });
                    });

                    console.log('    \'' + initOptions.ReferenceFactoryName + '\' (Reference Factory) mapped to \'' + initOptions.preCompReferenceRESTServiceHandler + '\'');
                }
            }
            var scopeStateInit = function () {

                console.log('  Initializing Routes for \'' + initOptions.state + '\'');

                //Resource Routes



                if (initOptions.useListRoute) {

                    $stateProvider
                        .state(initOptions.state, {
                            url: initOptions.stateRoute,
                            templateUrl: initOptions.preCompListUrl,
                            controller: initOptions.preCompListController
                        });

                    console.log('    List route for \'' + initOptions.state + '\' set (' + initOptions.stateRoute + ', URL: ' + initOptions.preCompListUrl + ')');
                }

                if (initOptions.useItemRoute) {

                    $stateProvider
                        .state(initOptions.state + '.detail', {
                            url: '/:id',
                            templateUrl: initOptions.preCompItemUrl,
                            controller: initOptions.customItemController || initOptions.preCompItemController
                        });

                    $stateProvider
                        .state(initOptions.state + '.new', {
                            url: '/new',
                            templateUrl: initOptions.preCompItemUrl,
                            controller: initOptions.preCompItemController
                        });

                    console.log('    List route for \'' + initOptions.state + '/*\' set (' + initOptions.stateRoute + '/1, URL: ' + initOptions.preCompItemUrl + ')');
                }

            }
            var scopeControllerInit = function () {

                //Controllers
                console.log('  Initializing Controllers for \'' + initOptions.collectionName + '\'');

                //List Control

                if (initOptions.useListController) {

                    console.log('  List: \'' + initOptions.baseListController + '\'');

                    ngNyanStackReference.controller(
                        initOptions.baseListController, [
                            '$scope',
                            '$state',
                            '$stateParams',
                            '$filter',
                            '$window',
                            initOptions.CollectionFactoryName,
                            initOptions.ItemFactoryName,
                            'toaster',
                            'ngTableParams',
                            '$q',
                            function ($scope, $state, $stateParams, $filter, $window, collectionFactory, itemFactory, toaster, ngTableParams, $q) {
                                $scope.dataLoaded = false;
                                $scope.angular = angular;
                                $scope.Items = [];
                                $scope.DisplayItems = [];
                                $scope.providerData = {};
                                $scope.providerData.collectionName = initOptions.collectionName;
                                $scope.rowsPerPage = provider.Parms.RowsPerPage;
                                $scope.tablePageNum = 1;
                                $scope.isPaginating = $scope.DisplayItems.length > $scope.rowsPerPage;
                                $scope.isTableSorted = initOptions.tableOptions.sortTable;
                                $scope.defaultSortColumn = initOptions.tableOptions.defaultSortColumn;
                                $scope.defaultSortDirection = initOptions.tableOptions.defaultSortDirection;

                                if (initOptions.PaginationMode.toString().toLowerCase() !== 'off') {
                                    //if a number, override RowsPerPage
                                    if (!isNaN(initOptions.PaginationMode) && isFinite(initOptions.PaginationMode)) {
                                        $scope.rowsPerPage = initOptions.PaginationMode;
                                    }
                                } else {
                                    $scope.rowsPerPage = Infinity;
                                }

                                $scope.$on('provider-viewport-resize', function (event, data) {
                                    $scope.$apply(function () {

                                        if (initOptions.PaginationMode.toString().toLowerCase() === 'auto') {
                                            $scope.rowsPerPage = data;
                                            $scope.tableParams.count(data);
                                            $scope.tableParams.reload();
                                        }
                                    });
                                });

                                //console.log('    Controller \'' + initOptions.baseListController + '\' invoked');

                                $scope.setSelectedItemId = function (id) {
                                    $scope.selectedItemId = id;
                                    console.log('selectedItemId: ' + $scope.selectedItemId);
                                };

                                $scope.setSelectedItemId(0);

                                if ($state.params)
                                    $scope.setSelectedItemId($state.params.id);

                                $scope.ControllerQuery = '';

                                $scope.selectItem = function (id) {
                                    $scope.setSelectedItemId(id);
                                    console.log('routing to ' + initOptions.state + '.detail');
                                    $state.go(initOptions.state + '.detail', { id: id });
                                };

                                $scope.createNew = function () {
                                    $scope.setSelectedItemId(0);
                                    $state.go(initOptions.state + '.new');
                                };

                                $scope.doQuery = function () {
                                    var term = $scope.ControllerQuery.toLowerCase();

                                    if (!term) {
                                        $scope.DisplayItems = $scope.Items;
                                        return;
                                    }

                                    var preData = $scope.Items.filter(function (item) {
                                        return ((item.Name + ' ' + item.Code).toLowerCase().indexOf(term) > -1);
                                    });

                                    $scope.DisplayItems = preData;
                                    $scope.reloadNgTable();
                                };

                                $scope.$watch("Items", function () {
                                    $scope.DisplayItems = $scope.Items;
                                    $scope.reloadNgTable();
                                });

                                $scope.$watch("DisplayItems", function () {
                                    $scope.reloadNgTable();
                                });

                                if (!$scope.listLoadData) {
                                    $scope.listLoadData = function () {

                                        if ($scope.onBeforeDataLoad)
                                            $scope.onBeforeDataLoad();

                                        collectionFactory.fetch(function (data) {
                                            $scope.Items = data;
                                            $scope.dataLoaded = true;
                                            if ($scope.onAfterDataLoad)
                                                $scope.onAfterDataLoad();

                                        });
                                    };
                                }

                                //ngTable Functionality
                                $scope.tableParams = new ngTableParams({
                                    page: 1,
                                    count: $scope.rowsPerPage
                                }, {
                                    total: $scope.DisplayItems.length, // length of data
                                    getData: function ($defer, params) {
                                        var orderedData = null,
                                            keys = Object.getOwnPropertyNames(params.sorting());

                                        //currently only support sorting by one column at a time
                                        //value sortable html attribute used by ngTable must match JSON property
                                        if (keys[0] && (keys[0].toLowerCase().indexOf("date") >= 0 || keys[0].toLowerCase().indexOf("time") >= 0)) {
                                            var sortDir = params.sorting()[keys[0]];

                                            if (sortDir === "asc") {
                                                $scope.DisplayItems.sort(function (a, b) {
                                                    var dateA = new Date(a[keys[0]]), dateB = new Date(b[keys[0]]);
                                                    return dateA - dateB; //sort by date descending
                                                });

                                            } else if (sortDir === "desc") {

                                                $scope.DisplayItems.sort(function (a, b) {
                                                    var dateA = new Date(a[keys[0]]), dateB = new Date(b[keys[0]]);
                                                    return dateB - dateA; //sort by date descending
                                                });

                                            }
                                            orderedData = $scope.DisplayItems;
                                        } else {
                                            // use build-in angular filter                             
                                            orderedData = params.sorting() ?
                                                              $filter('orderBy')($scope.DisplayItems, params.orderBy()) :
                                                              $scope.DisplayItems;
                                        }

                                        $scope.tablePageNum = params.page();

                                        $scope.getTableSortTooltipMessage = function () {
                                            return $scope.isSorted($scope.tableParams) ?
                                                       "Sorted by " + keys[0] + ", click to remove ordering" :
                                                       "Click on a column in the table to sort data";
                                        };

                                        $scope.isPaginating = $scope.DisplayItems.length > $scope.rowsPerPage;
                                        $defer.resolve(orderedData.slice((params.page() - 1) * params.count(), params.page() * params.count()));

                                    }
                                });

                                $scope.tableParams.$params.sorting[$scope.defaultSortColumn] = $scope.defaultSortDirection;

                                $scope.reloadNgTable = function () {
                                    $scope.tableParams.reload();
                                };

                                $scope.changePage = function (pageNum) {
                                    var maxPageNum = parseInt($scope.DisplayItems.length / $scope.rowsPerPage, 10);

                                    //true if increment up/down buttons pressed, else user has entered value
                                    if (pageNum) {
                                        $scope.tablePageNum = pageNum;
                                    }

                                    $scope.tablePageNum = parseInt($scope.tablePageNum, 10);
                                    if ($scope.tablePageNum < 1) {
                                        $scope.tablePageNum = 1;
                                    } else if ($scope.tablePageNum >= maxPageNum) {
                                        $scope.tablePageNum = maxPageNum == 0 ? 1 : maxPageNum;
                                    }

                                    $scope.tableParams.page($scope.tablePageNum);
                                    $scope.reloadNgTable();
                                };

                                $scope.unSortTable = function () {
                                    $scope.tableParams.sorting({});
                                };

                                //function binds to ui elements letting the user know when table is sorted
                                $scope.isSorted = function (tableParams) {
                                    return !angular.equals({}, tableParams.$params.sorting);
                                };

                                $scope.listLoadData();

                                $scope.DisplayItems = $scope.Items;

                                console.log('    Controller \'' + initOptions.baseListController + '\' finished loading');
                            }
                        ]);
                }

                //Item Control

                if (initOptions.useItemController) {

                    console.log('  Item: \'' + initOptions.baseItemController + '\'');

                    ngNyanStackReference.controller(
                        initOptions.baseItemController, [
                            '$scope',
                            '$state',
                            '$stateParams',
                            initOptions.customItemFactoryName || initOptions.ItemFactoryName,
                            'toaster',
                            '$q',
                            function ($scope, $state, $stateParams, itemFactory, toaster, $q) {

                                console.log('    Controller \'' + initOptions.baseItemController + '\' invoked');

                                $scope.save = function () {
                                    if ($scope.itemForm.$valid) {

                                        var data = $scope.Item;

                                        if ($scope.onBeforeSave)
                                            $q.when($scope.onBeforeSave(data)).then(function () {
                                                $scope.onHandlePreSave(data);
                                            });
                                        else
                                            $scope.onHandlePreSave(data);
                                    }
                                };

                                $scope.onHandlePreSave = function (data) {

                                    itemFactory.update($scope.Item,
                                        function () {
                                            if ($scope.onAfterSave)
                                                $q.when($scope.onAfterSave(data)).then(function () {
                                                    $scope.onHandlePostSave(data);
                                                });
                                            else
                                                $scope.onHandlePostSave(data);
                                        },
                                        function (e) {
                                            toaster.pop('warning', initOptions.collectionName, "An exception occurred while saving this item: " + e.data.ExceptionMessage);
                                        }
                                    );

                                };

                                $scope.onHandlePostSave = function (data) {
                                    toaster.pop('success', initOptions.collectionName, "Item Saved.");

                                    $scope.setSelectedItemId(data.Id);
                                    $state.go(initOptions.state + '.detail', { id: data.Id }, { reload: true });
                                };

                                $scope.cancel = function () {
                                    $scope.setSelectedItemId(0);
                                    $state.go(initOptions.state);
                                };

                                $scope.delete = function (id) {

                                    if ($scope.onBeforeDelete)
                                        $scope.onBeforeDelete();

                                    itemFactory.delete({ id: id },
                                        function (data) {

                                            if ($scope.onAfterDelete)
                                                $scope.onAfterDelete();

                                            toaster.pop('success', initOptions.collectionName, "Item " + id + " successfully removed.");
                                            $scope.setSelectedItemId(0);
                                            $state.go(initOptions.state, null, { reload: true });
                                        },
                                        function (e) {
                                            toaster.pop('warning', initOptions.collectionName, "An exception occurred while removing this item: " + e.data.ExceptionMessage);
                                        }
                                    );
                                };

                                $scope.setMode = function (parm) {
                                    switch (parm) {
                                        case 'delete':
                                            $scope.controllerDisplayMode = "view";
                                            $scope.controllerOperationMode = "delete";
                                            break;
                                        case 'edit':
                                            $scope.controllerDisplayMode = "edit";
                                            $scope.controllerOperationMode = "edit";
                                            break;
                                        case 'view':
                                            $scope.controllerDisplayMode = "view";
                                            $scope.controllerOperationMode = "view";
                                            break;
                                        case 'new':
                                            $scope.controllerDisplayMode = "edit";
                                            $scope.controllerOperationMode = "new";
                                            break;
                                    }
                                };

                                //If LoadData isn't overloaded by inheritance, implement it.

                                var targetId = $stateParams.id || 'new';

                                if (!$scope.itemLoadData) {

                                    if ($scope.onBeforeLoadData)
                                        $scope.onBeforeLoadData();

                                    $scope.itemLoadData = function () {
                                        $scope.Item = itemFactory.fetch(
                                            { id: targetId },
                                            function () {
                                                if ($scope.onAfterLoadData)
                                                    $scope.onAfterLoadData();
                                            }
                                        );
                                        $scope.setMode(targetId == 'new' ? 'new' : 'view');
                                    }
                                }

                                $scope.itemLoadData();

                                console.log('    Controller \'' + initOptions.baseItemController + '\' finished loading');
                            }
                        ]);
                }
            }

            init();

            return this;
        }


        this.$get = [
            '$http', '$q', function ($http, $q) {

                function resourceFactory(url, paramDefaults, actions, options) {

                    function setup() {
                    };

                };

                return resourceFactory;
            }
        ];
    });
}
)(window, window.angular);