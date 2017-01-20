AngularModule.controller('profileController', profileController);

profileController.$inject = ['$scope', '$http', 'ApiCall', '$routeParams', 'authService'];

function profileController($scope, $http, ApiCall, $routeParams, authService) {
    $scope.prop = {
        isUser: false,
        isReady: false
    };

    //$scope.user = {
    //    FullName: '',
    //    Followers: '',
    //    Messages:'',
    //    Birthday: '',
    //    Description: '',
    //    PhotoURL: '',
    //    PhoneNumber: '',
    //    Address: '',
    //    Website:""
    //};

    $scope.user={}

    $scope.Redirect = {}

    $scope.Redirect.Message = function (id) {
        window.location.href = "/message/" + id;
    };

    $scope.getRoles = function () {
        authService.getUserRolesNoAuthorizen().then(function (data) {
            if (data.includes("Admin")) $scope.isUser = true;
        })
    };

    $scope.getRoles();

    $scope.GetUserInfo = $http.get('api/Account/UserInfo').then(function (response) {
        $scope.prop.isReady = true;
        $scope.user = response.data;
        $scope.list = $scope.user.messages;
    });

    $scope.GetUserInfo;

    $scope.MessageModel = {
        Text: '',
        User:''
    }

    $scope.AddMessage = function () {
        $http({
            method: 'PUT',
            url: 'http://localhost:63417/api/Messages/AddMessage',
            data: $.param($scope.MessageModel),
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' }
        }).
           success(function (data) {
               window.location.href = "/profile";
           }).
           error(function (data) {
               //$scope.message = responseData.data;
           });
    };

    //$scope.Message = {}

    //$scope.Message.Model = {
    //    Text:''
    //};

    $scope.prop = {};
}