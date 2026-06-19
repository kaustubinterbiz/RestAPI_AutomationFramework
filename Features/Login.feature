    Feature: API Testing
      Pass base URL type or feature name in steps (Auth = B2C, Api = application).

    @Auth @SuperAdmin @LoginByAdmin
    Scenario:01.Verify POST User API generates token successfully for SuperAdmin role through valid login
        When User sends POST request on "Auth" base url with "SuperAdmin"
        Then Status should be OK

    @Auth @Env @LoginByEnv
    Scenario:02.Verify POST User API generates token successfully for role through valid login
        When User sends POST request on "Auth" base url
        Then Status should be OK

    @Api @SuperAdmin @LoginByAdmin @[api/v2/Session/GetSessionInfo/]
    Scenario:03.Verify GET API retrieves Cached_ID successfully using SuperAdmin role 
        When User sends POST request on "Auth" base url with "SuperAdmin"
        Then Status should be OK
        And the access token is stored from the last login response
        When User sends GET request for feature "User API Testing" with cached id
        Then Status code should be 200
        And session info from the last response is stored in appsettings

    @Api @Env @LoginByEnv @[api/v2/Session/GetSessionInfo/]
    Scenario:04.Verify GET API retrieves Cached_ID successfully using role 
        When User sends POST request on "Auth" base url
        Then Status should be OK
        And the access token is stored from the last login response
        When User sends GET request for feature "User API Testing" with cached id
        Then Status code should be 200
        And session info from the last response is stored in appsettings

    @Api @SuperAdmin @LoginByAdmin @[api/Account/ExistingUser?emailId={EmailId}]
    Scenario:05.Verify GET API retrieves existing user successfully by Admin role
        When User sends POST request on "Auth" base url with "SuperAdmin"
        Then Status should be OK
        And the access token is stored from the last login response
        When User sends GET request for feature "User API Testing" with cached id
        Then Status code should be 200
        And session info from the last response is stored in appsettings
        And Confirm the existing logged_in user is exist "Api"
        Then Status code should be 200

    @Api @HospitalRole @[api/Account/ExistingUser?emailId={EmailId}]
    Scenario:06.Verify GET API retrieves existing user successfully by HospitalCredential
        When User sends POST request on "Auth" base url with "HospitalRole"
        Then Status should be OK
        And the access token is stored from the last login response
        When User sends GET request for feature "User API Testing" with cached id
        Then Status code should be 200
        And session info from the last response is stored in appsettings
        And Confirm the existing logged_in user is exist "Api"
        Then Status code should be 200

    @Api @HospitalRole @[api/Account/ExistingUser?emailId={EmailId}]
    Scenario:07.Verify GET API retrieves user not exist by HospitalCredential
        When User sends POST request on "Auth" base url with "HospitalRole"
        Then Status should be OK
        And the access token is stored from the last login response
        When User sends GET request for feature "User API Testing" with cached id
        Then Status code should be 200
        And session info from the last response is stored in appsettings
        And Confirm the Email logged_in user is exist "Api" 
	    And Status should be NoContent

    @Api @[api/Member/CheckEmailAvailibility?EmailID=&BusinessUnitID=]
    Scenario:08.Verify GET Request for checking existing user in the same Oranization by HospitalCredential
        When User sends POST request on "Auth" base url with "HospitalRole"
        Then Status should be OK
        And the access token is stored from the last login response
        When User sends GET request for feature "User API Testing" with cached id
        Then Status code should be 200
        And session info from the last response is stored in appsettings
        And Confirm the User exist in the Same Organization "ValidateCheckExistingEmail"
	    And validate the response for the existing user in the same organization "ValidateCheckExistingEmail" and "ValidateCheckExistingEmail"
        Then Status code should be 200

    @Api @[api/Member/CheckEmailAvailibility?EmailID=&BusinessUnitID=]
    Scenario:09.Verify GET Request for checking existing user not in the same Oranization by HospitalCredential
        When User sends POST request on "Auth" base url with "HospitalRole"
        Then Status should be OK
        And the access token is stored from the last login response
        When User sends GET request for feature "User API Testing" with cached id
        Then Status code should be 200
        And session info from the last response is stored in appsettings
        When User sends flexible "Get" request on "Api" base url for endpoint "getCheckAvability" with url placeholders "ValidateCheckExistingEmail_OtherBusinessUnitId, BusinessUnitMemberId" target "ValidateCheckExistingEmail, ValidateBusinessUnitId" headers "CacheId" query params "-" body "-"
	    Then validate the response for the existing user in the same organization "ValidateCheckExistingEmail_OtherBusinessUnitId" and "ValidateCheckExistingEmail"
        Then Status code should be 200

    @Api @[api/Member/CheckEmailAvailibility?EmailID=&BusinessUnitID=]
    Scenario:10.Verify GET Request for checking new user in the same Oranization by HospitalCredential
        When User sends POST request on "Auth" base url with "HospitalRole"
        Then Status should be OK
        And the access token is stored from the last login response
        When User sends GET request for feature "User API Testing" with cached id
        Then Status code should be 200
        And session info from the last response is stored in appsettings
        When User sends flexible "Get" request on "Api" base url for endpoint "getCheckAvability" with url placeholders "ValidateCheckExistingEmail_Incorrect, BusinessUnitMemberId" target "ValidateCheckExistingEmail, ValidateBusinessUnitId" headers "CacheId" query params "-" body "-"
	    Then validate the response for the existing user in the same organization "ValidateCheckExistingEmail_Incorrect" and "ValidateCheckExistingEmail"
        And Status should be NoContent

    @Api @[api/Member/CheckEmailAvailibility?EmailID=&BusinessUnitID=]
    Scenario:11.Verify existing user in the same Organization by HospitalCredential
        When User sends POST request on "Auth" base url with "HospitalRole"
        Then Status should be OK
        And the access token is stored from the last login response
        When User sends GET request for feature "User API Testing" with cached id
        Then Status code should be 200
        And session info from the last response is stored in appsettings
        And Confirm the User exist in the Same Organization "ValidateCheckExistingEmail"
        And existingUser info from the last response is stored in appsettings
        Then Status code should be 200

    @Api @SuperAdmin @FlexibleRequest @[api/Member/CheckEmailAvailibility?EmailID=&BusinessUnitID=]
    Scenario:12.Verify flexible GET request retrieves existing user with dynamic headers
        When User sends POST request on "Auth" base url with "SuperAdmin"
        Then Status should be OK
        And the access token is stored from the last login response
        When User sends GET request for feature "User API Testing" with cached id
        Then Status code should be 200
        And session info from the last response is stored in appsettings
        When User sends flexible GET request on "Api" base url for endpoint "getExistingUser" with headers "CacheId"
        Then Status code should be 200

    @Api @[api/Account/Register]
    Scenario:13.Verify POST Request for checking new user in the same Oranization by HospitalCredential
        When User sends POST request on "Auth" base url with "HospitalRole"
        Then Status should be OK
        And the access token is stored from the last login response
        When User sends GET request for feature "User API Testing" with cached id
        Then Status code should be 200
        And session info from the last response is stored in appsettings
        When User sends flexible "Get" request on "Api" base url for endpoint "getCheckAvability" with url placeholders "ValidateCheckExistingEmail_Incorrect, BusinessUnitMemberId" target "ValidateCheckExistingEmail, ValidateBusinessUnitId" headers "CacheId" query params "-" body "-"
	    Then validate the response for the existing user in the same organization "ValidateCheckExistingEmail_Incorrect" and "ValidateCheckExistingEmail"
        And Status should be NoContent
        When User sends flexible "Post" request on "Api" base url for endpoint "post_Register" with url placeholders "-" target "-" headers "CacheId" query params "-" body "register_Body"
        Then Status code should be 200

    @Api @[api/Account/AddMultipleMemberByExce]  
    Scenario:14.Verify POST Request for uploading multiple user by excel file in the same Oranization by HospitalCredential
        When User sends POST request on "Auth" base url with "HospitalRole"
        Then Status should be OK
        And the access token is stored from the last login response
        When User sends GET request for feature "User API Testing" with cached id
        Then Status code should be 200
        And session info from the last response is stored in appsettings
        When User sends flexible "Post" request on "Api" base url for endpoint "addMultipleMemberByExcel" with url placeholders "-" target "-" headers "CacheId" query params "-" body "-"
        Then Status code should be 200
        #And Store the info for AddMultipleMemberByExcel
	    And Store the AddMultipleMemberByExcel response in excel file "Sample_File_Member.xlsx" sheet "Member_Respons_UserAlreadyExist"
        Then Validate the Status should be "User exists in same organization"
        #Then Status for AddMultipleMemberByExcel should be "User exists in same organization"

    @Api @FlexibleRequest
    Scenario: Get request with multiple headers and query params
        When User sends POST request on "Auth" base url with "SuperAdmin"
        Then Status should be OK
        And the access token is stored from the last login response
        When User sends GET request for feature "User API Testing" with cached id
        Then Status code should be 200
        And session info from the last response is stored in appsettings

        # Multiple headers test
        When User sends flexible "Get" request on "Api" base url for endpoint "getExistingUser" with url placeholders "-" target "-" headers "CacheId" query params "EmailId" body "-"
        Then Status code should be 200

        # URL placeholder + header test
        When User sends flexible "Get" request on "Api" base url for endpoint "getExistingUser" with url placeholders "ValidateCheckExistingEmail" target "EmailId" headers "CacheId" query params "-" body "-"
        Then Status should be NoContent

    @Api @FlexibleRequest
    Scenario:  "GET"Request with multiple headers and query params
        When User sends POST request on "Auth" base url with "SuperAdmin"
        Then Status should be OK
        And the access token is stored from the last login response
        When User sends GET request for feature "User API Testing" with cached id
        Then Status code should be 200
        And session info from the last response is stored in appsettings
        When User sends flexible "Get" request on "Api" base url for endpoint "CheckExistingUserAvailabilityInfo" with url placeholders "-" target "-" headers "CacheId" query params "EmailId" body "-"
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