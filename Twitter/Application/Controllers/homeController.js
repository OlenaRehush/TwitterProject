AngularModule.controller('homeController', function ($scope, $location, $http, ApiCall, authService) {

    $scope.UserRole = false;
    $scope.users;
    $scope.user;
    $scope.tweetList;
    $scope.GetUserInfo;
    $scope.ifSubscrEmpty;
    $scope.ifSumMessageEmpty;
    $scope.nonAuthMessage;

    $scope.isAuth = false;
    
    var result = $http.get('api/users/allusers').success(function (data) {
        $scope.users = data;
        var userRoles = $http.get('/api/Account/UserRoles').then(function (roles) {
            if (roles.data.includes("User")) {
                $scope.isAuth = true;
            }
            else {
                $scope.isAuth = true;
            }
        }, function (error) {
            $scope.message = error.data.Message;
        });
    })
    .then(function () {
        if ($scope.isAuth === false) {
            $scope.GetUserInfo = $http.get('api/Account/UserInfo').then(function (response) {
                $scope.prop.isReady = true;
                $scope.user = response.data;
                $scope.tweetList = $scope.user.subscribersTweets;

                if ($scope.user.subscribtions.length === 0)
                    $scope.ifSubscrEmpty = "You don't have subscribers";

                if ($scope.tweetList.length === 0)
                    $scope.ifSumMessageEmpty = "Your subscribers don't have tweets";

            });

           
        } else {
            document.getElementById('tweet-list').style.display = "none";
            $scope.nonAuthMessage = "You are not logged in!";
        }

    });
});