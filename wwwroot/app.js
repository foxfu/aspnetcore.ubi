var app = angular.module('app', []);

app.controller('BodyController', BodyController);

// Setup the filter
app.filter('userFilter', function () {
    // Create the return function and set the required parameter as well as an optional paramater
    return function (inputUsers, query) {
        var out = [];
        angular.forEach(inputUsers, function (u) {
            if (query == null || query == "" || u.firstName.toUpperCase().indexOf(query.toUpperCase()) >= 0
                || u.lastName.toUpperCase().indexOf(query.toUpperCase()) >= 0) {
                out.push(u);
            }
        })
        return out;
    }
});

BodyController.$inject = ['$scope', '$rootScope', '$http'];

function BodyController($scope, $rootScope, $http) {
    console.log('Body controller...');
    //Init
    $scope.isShowProfile = false;
    $scope.isShowGames = false;
    $scope.isShowUserManagement = false;

    //Change show content
    $scope.showCurrentUserProfile = function () {
        $scope.user = this.currentUser();
        $scope.isFromManagement = false;
        document.getElementById("txtPwdConfirm").value = "";
        this.showUserProfile();
    };
    $scope.showUserProfile = function () {
        $scope.isShowProfile = true;
        $scope.isShowGames = false;
        $scope.isShowUserManagement = false;
    };
    $scope.showMyGames = function () {
        $scope.isShowProfile = false;
        $scope.isShowGames = true;
        $scope.isShowUserManagement = false;
        $scope.isFromManagement = false;
        $scope.isShowGrantList = false;
        //List my games
        this.getAllGames();
    };
    $scope.showUserGames = function (userAccountId) {
        $scope.isShowProfile = false;
        $scope.isShowGames = true;
        $scope.isShowUserManagement = false;
        $scope.isFromManagement = true;
        $scope.isShowGrantList = false;
        //List user games
        this.getAllGames(userAccountId);
        //$scope.selectedUserAccountId = userAccountId;
    };
    //Show all games to grant user
    $scope.showGamesToGrant = function (userAccountId) {
        $scope.isShowProfile = false;
        $scope.isShowGames = true;
        $scope.isShowUserManagement = false;
        $scope.isFromManagement = true;
        $scope.isShowGrantList = true;
        //List all games including pending granted
        this.getAllGames(userAccountId, true);
        //$scope.selectedUserAccountId = userAccountId;
    }

    $scope.showUserManagement = function (reload) {
        $scope.isShowProfile = false;
        $scope.isShowGames = false;
        $scope.isShowUserManagement = true;
        //Get user list
        if (reload) {
            this.getAllUsers();
        }
    };

    $scope.isAdmin = function () {
        if ($rootScope.currentUser) {
            return $rootScope.currentUser.isAdmin;
        }
        return false;
    };
    
    $scope.isLogin = function () {
        return $rootScope.isLogin;
    };
    $scope.token = function () {
        return $rootScope.token;
    };

    $scope.currentUser = function () {
        return $rootScope.currentUser;
    };

    //Edit user profile
    $scope.manageUserProfile = function (user) {
        $scope.user = user;
        $scope.isFromManagement = true;
        document.getElementById("txtPwdConfirm").value = "";
        this.showUserProfile();
    };

    //Get all games
    $scope.getAllGames = function (userAccountId, includingNotOwned) {
        $scope.games = [];//Clear
        var headers = { 'Content-Type': 'application/json', 'Authorization': 'Bearer ' + $rootScope.token };
        var targetUrl = '/api/games';//My owned games info
        if (userAccountId) {
            if (includingNotOwned) {
                targetUrl += "/all?userAccountId=" + userAccountId;//All games info with specific user, including not owned to grant
            } else {
                targetUrl += "/ownership?userAccountId=" + userAccountId;//User owned games details info, including revoked games
            }
        }
        $http({
            method: 'GET',
            url: targetUrl,
            headers: headers
        }).then(function (result) {
            $scope.games = result.data;
        }, function (error) {
            console.log('Error get games' + error);
            $scope.logout();
        });
    };

    //Revoke user game
    $scope.revokeUserGame = function (gameInfo) {
        if (confirm("Are you sure to revoke this game for this user?")) {
            var headers = { 'Content-Type': 'application/json', 'Authorization': 'Bearer ' + $rootScope.token };
            var message = {};
            message.ownershipId = gameInfo.ownershipId;
            $http.post('/api/games/revoke', message, { headers: headers }).then(function (response) {
                if (response.data.statusCode == 200) {
                    //Update the entry in scope.games
                    gameInfo.state = 1;
                } else if (response.data.statusCode == 404) {
                    alert("Ownership not found, please refresh the page.");
                } else if (response.data.statusCode == 304) {
                    alert("Revoked already, please refresh the page.");
                }
            }, function (err) {
                console.log('Error found - ' + error);
            });
        }
    };

    $scope.grantUserGame = function (gameInfo) {
        if (confirm("Are you sure to grant this game to this user?")) {
            var headers = { 'Content-Type': 'application/json', 'Authorization': 'Bearer ' + $rootScope.token };
            var message = {};
            message.ownershipId = gameInfo.ownershipId;
            message.userAccountId = gameInfo.userAccountId;
            message.gameId = gameInfo.gameId;
            $http.post('/api/games/grant', message, { headers: headers }).then(function (response) {
                if (response.data.statusCode == 200) {
                    //Update the entry in scope.games
                    gameInfo.state = response.data.grantInfo.state;
                } else {
                    alert("Grant failed");
                }
            }, function (err) {
                console.log('Error - ' + error);
            });
        }
    };

    //Get all users
    $scope.getAllUsers = function () {
        $scope.users = [];//Clear
        var headers = { 'Content-Type': 'application/json', 'Authorization': 'Bearer ' + $rootScope.token };
        $http({
            method: 'GET',
            url: '/api/users',
            headers: headers
        }).then(function (result) {
            $scope.users = result.data;
        }, function (error) {
            console.log('Error get users' + error);
        });
    };

    //Update profile
    $scope.updateProfile = function () {
        var confirmPwd = document.getElementById("txtPwdConfirm").value;
        if (confirmPwd != $scope.user.password) {
            alert("Confirm password not match");
            return;
        }

        if (confirmPwd == "" || $scope.user.firstName == "" || $scope.user.lastName == "" || $scope.user.emailAddress == "") {
            alert("All fields are required, empty value not allowed");
            return;
        }

        var headers = { 'Content-Type': 'application/json', 'Authorization': 'Bearer ' + $rootScope.token };
        $http.post('/api/users/update', $scope.user, { headers: headers }).then(function (response) {
            if (response.status == 200) {
                //$scope.updateProfileStatus = "Update success";
                alert("Update success");
            }
        }, function (err) {
            //$scope.updateProfileStatus = "Update failed - " + err;
            alert("Update failed - " + err);
        });
    };

    //Redeem a game with key
    $scope.redeemKey = function () {
        if ($scope.gameKey == '') {
            alert("Please input the game key to redeem.");
            return;
        }
        var headers = { 'Content-Type': 'application/json', 'Authorization': 'Bearer ' + $rootScope.token };
        var message = {};
        message.key = $scope.gameKey;
        $http.post('/api/games/redeem', message, { headers: headers }).then(function (response) {
            if (response.data.statusCode == 200) {
                alert("Redeem success");
                console.log(response.data.game);
                if (response.data.game) {
                    $scope.games.push(response.data.game);
                }
            } else if (response.data.statusCode == 404) {
                alert("Invalid key");
            } else if (response.data.statusCode == 304) {
                alert("Key already redeemed");
            }
            $scope.gameKey = "";
        }, function (err) {
            $scope.gameKey = "";
        });
    };

    //Login
    $scope.login = function () {
        var headers = { 'Content-Type': 'application/json' };
        var message = {};
        message.emailAddress = $scope.email;
        message.password = $scope.password;

        $http.post('/api/login', message, { headers: headers }).then(function (response) {
            console.log('success');
            $rootScope.token = response.data.token;
            $rootScope.currentUser = response.data.user;
            $rootScope.isLogin = true;
            $scope.loginStatus = "";
        }, function (err) {
            //alert('Login failed');
            $scope.loginStatus = "Login Failed";
        });
    };

    //Logout
    $scope.logout = function () {
        $rootScope.token = null;
        $rootScope.isLogin = false;
        $rootScope.currentUser = null;
        $scope.password = null;
        $scope.isShowProfile = false;
        $scope.isShowGames = false;
        $scope.isShowUserManagement = false;
        $scope.isShowGrantList = false;

        this.init();
    };
    $scope.init = function () {
        $scope.loginStatus = null;
        $scope.gameKey = '';
        $scope.games = [];
        $scope.users = [];

        //$scope.password = "correct_horse_battery_staple";//test
    };

    $scope.ConvertDateString = function (dt) {
        var date = new Date(dt);
        return date.getFullYear() + "-" + ("0" + (date.getMonth() + 1)).slice(-2) + "-" + ("0" + date.getDate()).slice(-2);
    };

    $scope.init();
}
