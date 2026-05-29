Feature: User API Testing
  Pass base URL type or feature name in steps (Auth = B2C, Api = application).

@Auth @SuperAdmin
Scenario:1.Verify POST User API to get Token
    When User sends POST request on "Auth" base url with "SuperAdmin"
    Then Status should be OK

@Auth
Scenario:2.Verify POST User API to get Token
    When User sends POST request on "Auth" base url
    Then Status should be OK

@Api
Scenario:3.Verify GET API to get CachedId
    When User sends POST request on "Auth" base url with "SuperAdmin"
    Then Status should be OK
     And the access token is stored from the last login response
    When User sends GET request for feature "User API Testing" with cached id
    Then Status code should be 200
    And session info from the last response is stored in appsettings

@Api
Scenario:4.Verify GET API to get CachedId
    When User sends POST request on "Auth" base url
    Then Status should be OK
     And the access token is stored from the last login response
    When User sends GET request for feature "User API Testing" with cached id
    Then Status code should be 200
    And session info from the last response is stored in appsettings

@Api
Scenario:5.Verify GET API to get Existing User
    When User sends POST request on "Auth" base url
    Then Status should be OK
     And the access token is stored from the last login response
    When User sends GET request for feature "User API Testing" with cached id
    Then Status code should be 200
    And session info from the last response is stored in appsettings
    

@Api
Scenario:6.Verify POST User API
    When User sends POST request to create on "Api" base url
    Then Status should be OK

@Api
Scenario:7.Verify PUT User API
    When User sends PUT request on "Api" base url
    Then Status code should be 200

@Api
Scenario:8.Verify PATCH User API
    When User sends PATCH request on "Api" base url
    Then Status code should be 200

@Api
Scenario:9.Verify DELETE User API
    When User sends DELETE request on "Api" base url
    Then Status code should be 204

@Api
Scenario Outline:10. Dynamic base URL from step parameter
    When User sends GET request on "<BaseUrlType>" base url
    Then Status code should be <StatusCode>

    Examples:
      | BaseUrlType | StatusCode |
      | Api         | 200        |
