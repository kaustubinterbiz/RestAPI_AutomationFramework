Feature: GetFeatureBasedData API — Authentication Security

  POST api/v2/Patient/{patientId}/{dataType} JWT rejection checks.

  Uses Login.feature dynamic flexible steps and RequestBody.xlsx.



  @Api @Patient @HospitalRole @Authentication
  Scenario: FBD-01 GetFeatureBasedData rejects request when Authorization header is missing
    When User sends flexible "Post" request on "Apim" base url for endpoint "patientGetFeatureBasedData" with url placeholders "-" target "-" headers "CacheId" query params "-" body "patientGetFeatureBasedData_Body" url segments "patientId=A_OWNED_PATIENT_ID,dataType=dataType" token "none"
    Then Status code should be 401



  @Api @Patient @HospitalRole @Authentication
  Scenario: FBD-02 GetFeatureBasedData rejects request when Bearer token is empty
    When User sends flexible "Post" request on "Apim" base url for endpoint "patientGetFeatureBasedData" with url placeholders "-" target "-" headers "CacheId" query params "-" body "patientGetFeatureBasedData_Body" url segments "patientId=A_OWNED_PATIENT_ID,dataType=dataType" token "empty"
    Then Status code should be 401



  @Api @Patient @HospitalRole @Authentication
  Scenario: FBD-03 GetFeatureBasedData rejects request when access token is invalid garbage
    When User sends flexible "Post" request on "Apim" base url for endpoint "patientGetFeatureBasedData" with url placeholders "-" target "-" headers "CacheId" query params "-" body "patientGetFeatureBasedData_Body" url segments "patientId=A_OWNED_PATIENT_ID,dataType=dataType" token "garbage"
    Then Status code should be 401



  @Api @Patient @HospitalRole @Authentication
  Scenario: FBD-04 GetFeatureBasedData rejects request when access token is malformed
    When User sends flexible "Post" request on "Apim" base url for endpoint "patientGetFeatureBasedData" with url placeholders "-" target "-" headers "CacheId" query params "-" body "patientGetFeatureBasedData_Body" url segments "patientId=A_OWNED_PATIENT_ID,dataType=dataType" token "malformed"
    Then Status code should be 401



  @Api @HospitalRole @Authentication @[api/v2/Patient/{patientId}/{dataType}]
  Scenario: FBD-05 GetFeatureBasedData rejects expired access token
    When User sends POST request on "Auth" base url with "HospitalRole"
    Then Status should be OK
    And the access token is stored from the last login response
    When User sends GET request for feature "User API Testing" with cached id
    Then Status code should be 200
    And session info from the last response is stored in appsettings
    When User applies an expired access token
    When User sends flexible "Post" request on "Apim" base url for endpoint "patientGetFeatureBasedData" with url placeholders "-" target "-" headers "CacheId" query params "-" body "patientGetFeatureBasedData_Body" url segments "patientId=A_OWNED_PATIENT_ID,dataType=dataType" token "current"
    Then Status code should be 401



  @Api @HospitalRole @Authentication @[api/v2/Patient/{patientId}/{dataType}]
  Scenario: FBD-06 GetFeatureBasedData rejects tampered signature access token
    When User sends POST request on "Auth" base url with "HospitalRole"
    Then Status should be OK
    And the access token is stored from the last login response
    When User sends GET request for feature "User API Testing" with cached id
    Then Status code should be 200
    And session info from the last response is stored in appsettings
    When User applies a tampered access token
    When User sends flexible "Post" request on "Apim" base url for endpoint "patientGetFeatureBasedData" with url placeholders "-" target "-" headers "CacheId" query params "-" body "patientGetFeatureBasedData_Body" url segments "patientId=A_OWNED_PATIENT_ID,dataType=dataType" token "current"
    Then Status code should be 401



  @Api @HospitalRole @Authentication @[api/v2/Patient/{patientId}/{dataType}]
  Scenario: FBD-07 GetFeatureBasedData rejects wrong issuer or audience token
    When User sends POST request on "Auth" base url with "HospitalRole"
    Then Status should be OK
    And the access token is stored from the last login response
    When User sends GET request for feature "User API Testing" with cached id
    Then Status code should be 200
    And session info from the last response is stored in appsettings
    When User applies a wrong issuer audience access token
    When User sends flexible "Post" request on "Apim" base url for endpoint "patientGetFeatureBasedData" with url placeholders "-" target "-" headers "CacheId" query params "-" body "patientGetFeatureBasedData_Body" url segments "patientId=A_OWNED_PATIENT_ID,dataType=dataType" token "current"
    Then Status code should be 401



  @Api @HospitalRole @Authentication @[api/v2/Patient/{patientId}/{dataType}]
  Scenario: FBD-08 GetFeatureBasedData rejects token with missing required claim
    When User sends POST request on "Auth" base url with "HospitalRole"
    Then Status should be OK
    And the access token is stored from the last login response
    When User sends GET request for feature "User API Testing" with cached id
    Then Status code should be 200
    And session info from the last response is stored in appsettings
    When User applies a missing claim access token
    When User sends flexible "Post" request on "Apim" base url for endpoint "patientGetFeatureBasedData" with url placeholders "-" target "-" headers "CacheId" query params "-" body "patientGetFeatureBasedData_Body" url segments "patientId=A_OWNED_PATIENT_ID,dataType=dataType" token "current"
    Then Status code should be 401



  @Api @Patient @HospitalRole @Authorization
  Scenario: FBD-AUTH-0 Hospital can access GetFeatureBasedData for data type 0
    When User sends POST request on "Auth" base url with "HospitalRole"
    Then Status should be OK
    And the access token is stored from the last login response
    When User sends GET request for feature "User API Testing" with cached id
    Then Status code should be 200
    And session info from the last response is stored in appsettings
    When User sends GetFeatureBasedData request on "Api" for data type "0" with token "current"
    Then Status code should be 200



  @Api @Patient @HospitalRole @Authorization
  Scenario: FBD-AUTH-6 Hospital can access GetFeatureBasedData for data type 6
    When User sends POST request on "Auth" base url with "HospitalRole"
    Then Status should be OK
    And the access token is stored from the last login response
    When User sends GET request for feature "User API Testing" with cached id
    Then Status code should be 200
    And session info from the last response is stored in appsettings
    When User sends GetFeatureBasedData request on "Api" for data type "6" with token "current"
    Then Status code should be 200



  @Api @Patient @HospitalRole @Authorization
  Scenario: FBD-AUTH-1 Hospital can access GetFeatureBasedData for data type 1
    When User sends POST request on "Auth" base url with "HospitalRole"
    Then Status should be OK
    And the access token is stored from the last login response
    When User sends GET request for feature "User API Testing" with cached id
    Then Status code should be 200
    And session info from the last response is stored in appsettings
    When User sends GetFeatureBasedData request on "Api" for data type "1" with token "current"
    Then Status code should be 200



  @Api @Patient @Authorization @CrossOrg @BannerRole @[api/v2/Patient/{patientId}/{dataType}]
  Scenario: FBD-CROSS-0 Banner denied for Hospital patient on data type 0
    When User sends POST request on "Auth" base url with "HospitalRole"
    Then Status should be OK
    And the access token is stored from the last login response
    When User sends GET request for feature "User API Testing" with cached id
    Then Status code should be 200
    And session info from the last response is stored in appsettings
    When User sends GetFeatureBasedData request on "Api" for data type "0" with token "current"
    Then Status code should be 200
    When User sends POST request on "Auth" base url with "BannerRole"
    Then Status should be OK
    And the access token is stored from the last login response
    When User sends GET request for feature "User API Testing" with cached id
    Then Status code should be 200
    And session info from the last response is stored in appsettings
    When User sends GetFeatureBasedData request on "Api" for data type "0" with token "current"
    Then cross-org user must be denied GetFeatureBasedData access with status code 401



  @Api @Patient @Authorization @CrossOrg @BannerRole @[api/v2/Patient/{patientId}/{dataType}]
  Scenario: FBD-CROSS-6 Banner denied for Hospital patient on data type 6
    When User sends POST request on "Auth" base url with "HospitalRole"
    Then Status should be OK
    And the access token is stored from the last login response
    When User sends GET request for feature "User API Testing" with cached id
    Then Status code should be 200
    And session info from the last response is stored in appsettings
    When User sends GetFeatureBasedData request on "Api" for data type "6" with token "current"
    Then Status code should be 200
    When User sends POST request on "Auth" base url with "BannerRole"
    Then Status should be OK
    And the access token is stored from the last login response
    When User sends GET request for feature "User API Testing" with cached id
    Then Status code should be 200
    And session info from the last response is stored in appsettings
    When User sends GetFeatureBasedData request on "Api" for data type "6" with token "current"
    Then cross-org user must be denied GetFeatureBasedData access with status code 401



  @Api @Patient @Authorization @CrossOrg @BannerRole @[api/v2/Patient/{patientId}/{dataType}]
  Scenario: FBD-CROSS-1 Banner denied for Hospital patient on data type 1
    When User sends POST request on "Auth" base url with "HospitalRole"
    Then Status should be OK
    And the access token is stored from the last login response
    When User sends GET request for feature "User API Testing" with cached id
    Then Status code should be 200
    And session info from the last response is stored in appsettings
    When User sends GetFeatureBasedData request on "Api" for data type "1" with token "current"
    Then Status code should be 200
    When User sends POST request on "Auth" base url with "BannerRole"
    Then Status should be OK
    And the access token is stored from the last login response
    When User sends GET request for feature "User API Testing" with cached id
    Then Status code should be 200
    And session info from the last response is stored in appsettings
    When User sends GetFeatureBasedData request on "Api" for data type "1" with token "current"
    Then cross-org user must be denied GetFeatureBasedData access with status code 401
