Feature: API Security Authentication
  JWT rejection checks on protected Api endpoints.
  Each scenario: apply missing/bad token → call Api → expect reject (ExpectedStatus from Excel).
  Data source: TestData/UploadFiles/ApiSecurityAuthMatrix.xlsx

  # =========================================================
  # Endpoint: api/v2/Session/GetSessionInfo/  (key = get)
  # =========================================================

  @Security @Authentication @P0 @Session @[api/v2/Session/GetSessionInfo/]
  Scenario: TC_01 Session API rejects request when Authorization header is missing
    When API Security runs authentication test "AUTH-GET-01"
    Then all authorization executions should pass

  @Security @Authentication @P0 @Session @[api/v2/Session/GetSessionInfo/]
  Scenario: TC_02 Session API rejects request when Bearer token is empty
    When API Security runs authentication test "AUTH-GET-02"
    Then all authorization executions should pass

  @Security @Authentication @P0 @Session @[api/v2/Session/GetSessionInfo/]
  Scenario: TC_03 Session API rejects request when access token is invalid garbage
    When API Security runs authentication test "AUTH-GET-03"
    Then all authorization executions should pass

  @Security @Authentication @P0 @Session @[api/v2/Session/GetSessionInfo/]
  Scenario: TC_04 Session API rejects request when access token is malformed
    When API Security runs authentication test "AUTH-GET-04"
    Then all authorization executions should pass

  @Security @Authentication @P0 @Session @[api/v2/Session/GetSessionInfo/]
  Scenario: TC_05 Session API rejects request when access token is expired
    When API Security runs authentication test "AUTH-GET-05"
    Then all authorization executions should pass

  @Security @Authentication @P0 @Session @[api/v2/Session/GetSessionInfo/]
  Scenario: TC_06 Session API rejects request after login when access token is tampered
    Given User has a valid access token on "Auth" base url
    When User applies a tampered access token
    When User sends GET request on "Api" base url with current token only
    Then the API status code should be 401

  @Security @Authentication @P0 @Session @[api/v2/Session/GetSessionInfo/]
  Scenario: TC_07 Session API rejects request when Bearer prefix is missing from token
    When API Security runs authentication test "AUTH-GET-07"
    Then all authorization executions should pass

      @Security @Authentication @P0 @Session @[api/v2/Session/GetSessionInfo/]
  Scenario: TC_08 Session API rejects request when Authorization header is missing
    When API Security runs authentication test "AUTH-GET-08"
    Then all authorization executions should pass

  @Security @Authentication @P0 @Session @[api/v2/Session/GetSessionInfo/]
  Scenario: TC_09 Session API rejects request when Bearer token is empty
    When API Security runs authentication test "AUTH-GET-09"
    Then all authorization executions should pass

  @Security @Authentication @P0 @Session @[api/v2/Session/GetSessionInfo/]
  Scenario: TC_10 Session API rejects request when access token is invalid garbage
    When API Security runs authentication test "AUTH-GET-10"
    Then all authorization executions should pass

  @Security @Authentication @P0 @Session @[api/v2/Session/GetSessionInfo/]
  Scenario: TC_11 Session API rejects request when access token is malformed
    When API Security runs authentication test "AUTH-GET-11"
    Then all authorization executions should pass

  @Security @Authentication @P0 @Session @[api/v2/Session/GetSessionInfo/]
  Scenario: TC_12 Session API rejects request when access token is expired
    When API Security runs authentication test "AUTH-GET-12"
    Then all authorization executions should pass

  @Security @Authentication @P0 @Session @[api/v2/Session/GetSessionInfo/]
  Scenario: TC_13 Session API rejects request after login when access token is tampered
    Given User has a valid access token on "Auth" base url
    When User applies a tampered access token
    When User sends GET request on "Api" base url with current token only
    Then the API status code should be 401

  @Security @Authentication @P0 @Session @[api/v2/Session/GetSessionInfo/]
  Scenario: TC_14 Session API rejects request when Bearer prefix is missing from token
    When API Security runs authentication test "AUTH-GET-14"
    Then all authorization executions should pass

  # =========================================================
  # Endpoint: api/Account/ExistingUser  (key = getExistingUser)
  # =========================================================

  @Security @Authentication @P0 @Account @[api/Account/ExistingUser]
  Scenario: ExistingUser API rejects request when Authorization header is missing
    When API Security runs authentication test "AUTH-GETEXISTINGUSER-01"
    Then all authorization executions should pass

  @Security @Authentication @P0 @Account @[api/Account/ExistingUser]
  Scenario: ExistingUser API rejects request when Bearer token is empty
    When API Security runs authentication test "AUTH-GETEXISTINGUSER-02"
    Then all authorization executions should pass

  @Security @Authentication @P0 @Account @[api/Account/ExistingUser]
  Scenario: ExistingUser API rejects request when access token is invalid garbage
    When API Security runs authentication test "AUTH-GETEXISTINGUSER-03"
    Then all authorization executions should pass

  @Security @Authentication @P0 @Account @[api/Account/ExistingUser]
  Scenario: ExistingUser API rejects request when access token is malformed
    When API Security runs authentication test "AUTH-GETEXISTINGUSER-04"
    Then all authorization executions should pass

  @Security @Authentication @P0 @Account @[api/Account/ExistingUser]
  Scenario: ExistingUser API rejects request when access token is expired
    When API Security runs authentication test "AUTH-GETEXISTINGUSER-05"
    Then all authorization executions should pass

  @Security @Authentication @P0 @Account @[api/Account/ExistingUser]
  Scenario: ExistingUser API rejects request after login when access token payload claim is tampered
    When API Security runs authentication test "AUTH-GETEXISTINGUSER-06"
    Then all authorization executions should pass

  @Security @Authentication @P0 @Account @[api/Account/ExistingUser]
  Scenario: ExistingUser API rejects request when Bearer prefix is missing from token
    When API Security runs authentication test "AUTH-GETEXISTINGUSER-07"
    Then all authorization executions should pass

  # =========================================================
  # Endpoint: api/Member/CheckEmailAvailibility  (key = getCheckAvability)
  # =========================================================

  @Security @Authentication @P0 @Member @[api/Member/CheckEmailAvailibility]
  Scenario: CheckEmailAvailibility API rejects request when Authorization header is missing
    When API Security runs authentication test "AUTH-GETCHECKAVABILITY-01"
    Then all authorization executions should pass

  @Security @Authentication @P0 @Member @[api/Member/CheckEmailAvailibility]
  Scenario: CheckEmailAvailibility API rejects request when Bearer token is empty
    When API Security runs authentication test "AUTH-GETCHECKAVABILITY-02"
    Then all authorization executions should pass

  @Security @Authentication @P0 @Member @[api/Member/CheckEmailAvailibility]
  Scenario: CheckEmailAvailibility API rejects request when access token is invalid garbage
    When API Security runs authentication test "AUTH-GETCHECKAVABILITY-03"
    Then all authorization executions should pass

  @Security @Authentication @P0 @Member @[api/Member/CheckEmailAvailibility]
  Scenario: CheckEmailAvailibility API rejects request when access token is malformed
    When API Security runs authentication test "AUTH-GETCHECKAVABILITY-04"
    Then all authorization executions should pass

  @Security @Authentication @P0 @Member @[api/Member/CheckEmailAvailibility]
  Scenario: CheckEmailAvailibility API rejects request when access token is expired
    When API Security runs authentication test "AUTH-GETCHECKAVABILITY-05"
    Then all authorization executions should pass

  @Security @Authentication @P0 @Member @[api/Member/CheckEmailAvailibility]
  Scenario: CheckEmailAvailibility API rejects request after login when access token payload claim is tampered
    When API Security runs authentication test "AUTH-GETCHECKAVABILITY-06"
    Then all authorization executions should pass

  @Security @Authentication @P0 @Member @[api/Member/CheckEmailAvailibility]
  Scenario: CheckEmailAvailibility API rejects request when Bearer prefix is missing from token
    When API Security runs authentication test "AUTH-GETCHECKAVABILITY-07"
    Then all authorization executions should pass

  # =========================================================
  # Batch run (all Excel Enabled rows in one scenario)
  # =========================================================

  @Security @Authentication @P0 @ExcelLoop
  Scenario: Execute all authentication security tests from Excel in one run
    When API Security executes all authentication tests from Excel
    Then all authorization executions should pass
