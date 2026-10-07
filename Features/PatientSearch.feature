Feature: Patient Search API — Authorization

  POST api/v2/Patient/Search/1 authorized access check.
  Demographic fields FirstName, LastName, Dob loaded from RequestBody.xlsx.
  Insurance, Sort (Mode), and By (OrderBy) passed from Scenario Outline.



  @Api @Patient @HospitalRole @Authorization @[api/v2/Patient/Search/1]
  Scenario: PS-AUTH-1 Hospital can search patients by demographic filter
    When User sends POST request on "Auth" base url with "HospitalRole"
    Then Status should be OK
    And the access token is stored from the last login response
    When User sends GET request for feature "User API Testing" with cached id
    Then Status code should be 200
    And session info from the last response is stored in appsettings
    When User sends flexible "Post" request on "Api" base url for endpoint "patientSearch" with url placeholders "-" target "-" headers "CacheId" query params "-" body "patientSearch_Body" url segments "-" token "current"
    Then Status code should be 200



  @Api @Patient @HospitalRole @Authorization @Filters @[api/v2/Patient/Search/1]
  Scenario Outline: PS-FILTER Patient search respects Insurance Sort By filters
    When User sends POST request on "Auth" base url with "HospitalRole"
    Then Status should be OK
    And the access token is stored from the last login response
    When User sends GET request for feature "User API Testing" with cached id
    Then Status code should be 200
    And session info from the last response is stored in appsettings
    When User sends Patient Search request on "Api" with insurance "<Insurance>" sort "<Sort>" by "<By>" token "current"
    Then Status code should be <Status>

    Examples:
      | Insurance   | Sort      | By     | Status |
      | All         | Recent    | -      | 200    |
      | Aetna Group | Recent    | -      | 200    |
      | All         | Name      | A-Z    | 200    |
      | Aetna Group | Name      | A-Z    | 200    |
      | All         | Name      | Z-A    | 200    |
      | Aetna Group | Name      | Z-A    | 200    |
      | All         | Insurance | A-Z    | 200    |
      | Aetna Group | Insurance | A-Z    | 200    |
      | All         | Insurance | Z-A    | 200    |
      | Aetna Group | Insurance | Z-A    | 200    |
      | All         | Admitted  | Latest | 200    |
      | Aetna Group | Admitted  | Latest | 200    |
      | All         | Admitted  | Oldest | 200    |
      | Aetna Group | Admitted  | Oldest | 200    |
      | All         | Discharge | Latest | 200    |
      | Aetna Group | Discharge | Latest | 200    |
      | All         | Discharge | Oldest | 200    |
      | Aetna Group | Discharge | Oldest | 200    |
