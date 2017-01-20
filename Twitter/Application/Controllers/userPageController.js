AngularModule.controller('userPageController', userPageController);

userPageController.$inject = ['$scope', '$http', 'ApiCall', '$routeParams'];
function userPageController($scope, $http, ApiCall, $routeParams) {

    var result = ApiCall.GetApiCall('/api/users/getuser?id=' + $routeParams.userId).success(function (data) {
        $scope.user = data;
        $scope.list = $scope.user.messages;
        $scope.isSubsc = $scope.user.isSubscribed;
    })
    .then(function () {

    });

    $scope.Subscribe;

    $scope.Subscribe = function () {
        $http({
            method: 'POST',
            url: 'api/user/addSubscriber?subscriberId=' + $scope.user.id,
           // data: $.param($scope.user.id),
           // headers: { 'Content-Type': 'application/x-www-form-urlencoded' }
        }).
           success(function (data) {
               //  window.location.href = "/user/"+$scope.user.id;
               $scope.isSubsc = true;
           }).
           error(function (data) {
               //$scope.message = responseData.data;
               
           });
    }

    $scope.UnSubscribe = function () {
        $http({
            method: 'POST',
            url: 'api/user/deleteSubscriber?subscriberId=' + $scope.user.id,
            // data: $.param($scope.user.id),
            // headers: { 'Content-Type': 'application/x-www-form-urlencoded' }
        }).
           success(function (data) {
               //window.location.href = "/user/" + $scope.user.id;
               $scope.isSubsc = false;
           }).
           error(function (data) {
               //$scope.message = responseData.data;
           });
    }
}