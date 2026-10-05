Feature: Patient List API — Authentication Security

  POST api/v2/Patient/{businessunitId}/Patients JWT rejection checks.

  Uses Login.feature dynamic flexible steps and RequestBody.xlsx.



  @Api @Patient @HospitalRole @Authentication

  Scenario: PAT-01 Patient List rejects request when Authorization header is missing
    When User sends flexible "Post" request on "Apim" base url for endpoint "patientList" with url placeholders "-" target "-" headers "CacheId" query params "-" body "patientList_Body" url segments "businessunitId=BusinessUnitId" token "none"
    Then Status code should be 401



  @Api @Patient @HospitalRole @Authentication
  Scenario: PAT-02 Patient List rejects request when Bearer token is empty
    When User sends flexible "Post" request on "Apim" base url for endpoint "patientList" with url placeholders "-" target "-" headers "CacheId" query params "-" body "patientList_Body" url segments "businessunitId=BusinessUnitId" token "empty"
    Then Status code should be 401



  @Api @Patient @HospitalRole @Authentication
  Scenario: PAT-03 Patient List rejects request when access token is invalid garbage
    When User sends flexible "Post" request on "Apim" base url for endpoint "patientList" with url placeholders "-" target "-" headers "CacheId" query params "-" body "patientList_Body" url segments "businessunitId=BusinessUnitId" token "garbage"
    Then Status code should be 401



  @Api @Patient @HospitalRole @Authentication
  Scenario: PAT-04 Patient List rejects request when access token is malformed
    When User sends flexible "Post" request on "Apim" base url for endpoint "patientList" with url placeholders "-" target "-" headers "CacheId" query params "-" body "patientList_Body" url segments "businessunitId=BusinessUnitId" token "malformed"
    Then Status code should be 401



  @Api @HospitalRole @Authentication @[api/v2/Patient/{businessunitId}/Patients]
  Scenario: PAT-05 Patient List rejects expired access token
    When User sends POST request on "Auth" base url with "HospitalRole"
    Then Status should be OK
    And the access token is stored from the last login response
    When User sends GET request for feature "User API Testing" with cached id
    Then Status code should be 200
    And session info from the last response is stored in appsettings
    When User applies an expired access token
    When User sends flexible "Post" request on "Apim" base url for endpoint "patientList" with url placeholders "-" target "-" headers "CacheId" query params "-" body "patientList_Body" url segments "businessunitId=BusinessUnitId" token "current"
    Then Status code should be 401



  @Api @HospitalRole @Authentication @[api/v2/Patient/{businessunitId}/Patients]
  Scenario: PAT-06 Patient List rejects tampered signature access token
    When User sends POST request on "Auth" base url with "HospitalRole"
    Then Status should be OK
   And the access token is stored from the last login response
    When User sends GET request for feature "User API Testing" with cached id
    Then Status code should be 200
    And session info from the last response is stored in appsettings
    When User applies a tampered access token
    When User sends flexible "Post" request on "Apim" base url for endpoint "patientList" with url placeholders "-" target "-" headers "CacheId" query params "-" body "patientList_Body" url segments "businessunitId=BusinessUnitId" token "current"
    Then Status code should be 401



  @Api @HospitalRole @Authentication @[api/v2/Patient/{businessunitId}/Patients]
  Scenario: PAT-07 Patient List rejects wrong issuer or audience token
    When User sends POST request on "Auth" base url with "HospitalRole"
    Then Status should be OK
    And the access token is stored from the last login response
    When User sends GET request for feature "User API Testing" with cached id
    Then Status code should be 200
    And session info from the last response is stored in appsettings
    When User applies a wrong issuer audience access token
    When User sends flexible "Post" request on "Apim" base url for endpoint "patientList" with url placeholders "-" target "-" headers "CacheId" query params "-" body "patientList_Body" url segments "businessunitId=BusinessUnitId" token "current"
    Then Status code should be 401



  @Api @HospitalRole @Authentication @[api/v2/Patient/{businessunitId}/Patients]
  Scenario: PAT-08 Patient List rejects token with missing required claim
    When User sends POST request on "Auth" base url with "HospitalRole"
    Then Status should be OK
    And the access token is stored from the last login response
    When User sends GET request for feature "User API Testing" with cached id
    Then Status code should be 200
    And session info from the last response is stored in appsettings
    When User applies a missing claim access token
    When User sends flexible "Post" request on "Apim" base url for endpoint "patientList" with url placeholders "-" target "-" headers "CacheId" query params "-" body "patientList_Body" url segments "businessunitId=BusinessUnitId" token "current"
    Then Status code should be 401

