(function () {

    AngularModule.controller('indexController', ['$scope', '$location', 'authService', function ($scope, $location, authService) {
        {

            $scope.prop = {
                IsUploadHeaderCount: false,
            }

 
            $scope.logOut = function () {
                authService.logOut();
                $location.path('/home');
            }

            $scope.authentication = authService.authentication;
        }
    }]);
})();