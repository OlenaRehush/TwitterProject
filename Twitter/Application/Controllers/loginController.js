(function () {

    AngularModule.controller('loginController', ['$scope', '$location', 'authService', function ($scope, $location, authService) {
        $scope.forceError = false;

        $scope.reload = function () {
            $scope.show_message = false;
            $scope.inProgress = false;
            $scope.forceError = false;
            $scope.message = "";
        }

        $scope.reload();

        $scope.loginData = {
            userName: "",
            password: ""
        };
        $scope.allowReconfrimEmail = false;

        $scope.login = function () {
            $scope.inProgress = true;

            authService.login($scope.loginData).then(function (response) {
                location.reload();
                $location.path('/home');

            },
             function (err) {
                 $scope.inProgress = false;
                 if (err.error == 'invalid_grant') {
                     $scope.forceError = true;
                 }
                 else {
                     $scope.show_message = true;
                     $scope.message = err.error_description;
                     if (err.error == 'notConfimed_email')
                         $scope.allowReconfrimEmail = true;
                 }
             });
        };
        $scope.recomfirmEmail = function () {
            $scope.inProgress = true;
            authService.reConfirmEmail($scope.loginData.userName).then(function (response) {
                location.reload();
                $location.path('/home');
            },
             function (err) {
                 $scope.inProgress = false;
                 $scope.show_message = true;
                 $scope.message = err.error_description;
             });
        };

    }]);

})();


