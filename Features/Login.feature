Feature: API Testing
  Pass base URL type or feature name in steps (Auth = B2C, Api = application).

@Auth @SuperAdmin @LoginByAdmin
Scenario:1.Verify POST User API generates token successfully for SuperAdmin role through valid login
    When User sends POST request on "Auth" base url with "SuperAdmin"
    Then Status should be OK

@Auth @Env @LoginByEnv
Scenario:2.Verify POST User API generates token successfully for role through valid login
    When User sends POST request on "Auth" base url
    Then Status should be OK

@Api @SuperAdmin @LoginByAdmin
Scenario:3.Verify GET API retrieves Cached_ID successfully using SuperAdmin role
    When User sends POST request on "Auth" base url with "SuperAdmin"
    Then Status should be OK
    And the access token is stored from the last login response
    When User sends GET request for feature "User API Testing" with cached id
    Then Status code should be 200
    And session info from the last response is stored in appsettings

@Api @Env @LoginByEnv
Scenario:4.Verify GET API retrieves Cached_ID successfully using role
    When User sends POST request on "Auth" base url
    Then Status should be OK
    And the access token is stored from the last login response
    When User sends GET request for feature "User API Testing" with cached id
    Then Status code should be 200
    And session info from the last response is stored in appsettings

@Api @SuperAdmin @LoginByAdmin
Scenario: 5.Verify GET API retrieves existing user successfully by Admin role
    When User sends POST request on "Auth" base url with "SuperAdmin"
    Then Status should be OK
    And the access token is stored from the last login response
    When User sends GET request for feature "User API Testing" with cached id
    Then Status code should be 200
    And session info from the last response is stored in appsettings
    And Confirm the existing logged_in user is exist "Api"
    Then Status code should be 200

@Api @HospitalRole 
Scenario: 6.Verify GET API retrieves existing user successfully by HospitalCredential
    When User sends POST request on "Auth" base url with "HospitalRole"
    Then Status should be OK
    And the access token is stored from the last login response
    When User sends GET request for feature "User API Testing" with cached id
    Then Status code should be 200
    And session info from the last response is stored in appsettings
    And Confirm the existing logged_in user is exist "Api"
    Then Status code should be 200

@Api @HospitalRole
Scenario: 7.Verify GET API retrieves user not exist by HospitalCredential
    When User sends POST request on "Auth" base url with "HospitalRole"
    Then Status should be OK
    And the access token is stored from the last login response
    When User sends GET request for feature "User API Testing" with cached id
    Then Status code should be 200
    And session info from the last response is stored in appsettings
    And Confirm the Email logged_in user is exist "Api" 
	And Status should be NoContent

@Api
Scenario:8.Verify PUT User API
    When User sends PUT request on "Api" base url
    Then Status code should be 200

@Api
Scenario:9.Verify PATCH User API
    When User sends PATCH request on "Api" base url
    Then Status code should be 200

@Api
Scenario:9.Verify DELETE User API
    When User sends DELETE request on "Api" base url
    Then Status code should be 204

@Api @SuperAdmin @FlexibleRequest
Scenario: 10.Verify flexible GET request retrieves existing user with dynamic headers
    When User sends POST request on "Auth" base url with "SuperAdmin"
    Then Status should be OK
    And the access token is stored from the last login response
    When User sends GET request for feature "User API Testing" with cached id
    Then Status code should be 200
    And session info from the last response is stored in appsettings
    When User sends flexible GET request on "Api" base url for endpoint "getExistingUser" with headers "CacheId"
    Then Status code should be 200

@Api @Parameterized
Scenario Outline: Verify GET API retrieves existing user successfully by Role
  When User sends POST request on "<BaseUrlType>" base url with "<Role>"
  Then Status should be OK
  And the access token is stored from the last login response
  When User sends GET request for feature "<API>" with cached id
  Then Status code should be <StatusCode>
  And session info from the last response is stored in appsettings
  And Confirm the existing logged_in user is exist "<API>"
  Then Status code should be <StatusCode>
  Examples:
      | BaseUrlType | Role         | API              | Status | StatusCode |
      | Api         | SuperAdmin   | User API Testing | OK     |        200 |
      | Api         | HospitalRole | User API Testing | OK     |        200 |