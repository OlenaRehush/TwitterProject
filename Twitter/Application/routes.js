var AngularModule = angular.module('twitter_main', ['ngRoute', 'LocalStorageModule']);

AngularModule.config(function ($routeProvider, $locationProvider) {
    $routeProvider
    .when('/', {
        templateUrl: '../Application/Views/home.html'
    })
    .when('/login', {
        templateUrl: '../Application/Views/login.html',
        controller: 'loginController'
    })
    .when('/register', {
        templateUrl: '../Application/Views/register.html',
        controller: 'signupController'
    })
    .when('/home', {
        templateUrl: '../Application/Views/home.html',
        controller: 'homeController'
    })
    .when('/user/:userId', {
        templateUrl: '../Application/Views/userPage.html',
        controller: 'userPageController'
    })
    .when('/profile', {
        templateUrl: '../Application/Views/profile.html',
        controller: 'profileController'
    })
    .otherwise({
        templateUrl: '../Application/Views/error.html'
    });


    $locationProvider.html5Mode(true);

});

AngularModule.run(['authService', function (authService) {
    authService.fillAuthData();
}]);

AngularModule.config(function ($httpProvider) {
    $httpProvider.interceptors.push('authInterceptorService');
});

AngularModule.service('ApiCall', ['$http', function ($http) {

    var result;

    this.GetApiCall = function (controllerName, methodName) {
        result = $http.get(controllerName).success(function (data, status) {
            result = (data);
        });
        return result;
    };

    this.PostApiCall = function (controllerName, methodName, obj) {
        result = $http.post('api/' + controllerName + '/' + methodName, obj).success(function (data, status) {
            result = (data);
        });
        return result;
    };
}]);


/*
    Directive for opening link in new _blank
*/
AngularModule.directive('myTarget', function () {
    return {
        restrict: 'A',
        link: function (scope, element, attrs) {
            var href = element.href;
            if (true) {
                element.attr("target", "_blank");
            }
        }
    };
});

AngularModule.directive('head', ['$rootScope', '$compile',
    function ($rootScope, $compile) {
        return {
            restrict: 'E',
            link: function (scope, elem) {
                var html = '<link rel="stylesheet" ng-repeat="(routeCtrl, cssUrl) in routeStyles" ng-href="{{cssUrl}}" />';
                elem.append($compile(html)(scope));
                scope.routeStyles = {};
                $rootScope.$on('$routeChangeStart', function (e, next, current) {
                    if (current && current.$$route && current.$$route.css) {
                        if (!angular.isArray(current.$$route.css)) {
                            current.$$route.css = [current.$$route.css];
                        }
                        angular.forEach(current.$$route.css, function (sheet) {
                            delete scope.routeStyles[sheet];
                        });
                    }
                    if (next && next.$$route && next.$$route.css) {
                        if (!angular.isArray(next.$$route.css)) {
                            next.$$route.css = [next.$$route.css];
                        }
                        angular.forEach(next.$$route.css, function (sheet) {
                            scope.routeStyles[sheet] = sheet;
                        });
                    }
                });
            }
        };
    }
]);