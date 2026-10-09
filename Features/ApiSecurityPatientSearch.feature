Feature: Patient Search API Security
  POST api/v2/Patient/Search/1 — curl-aligned PatientDemographic body on Api; auth negatives on Apim (same path via gateway).
  Login + session gives Bearer token and CacheId (never hardcoded in tests).

  # Outcome values: returns patients | returns no patients | includes expected patient

  @Security @PatientSearch @P0 @Response @[api/v2/Patient/Search/1]
  Scenario Outline: TC_01 Patient Search returns expected results for demographics
    When the user "<Role>" is logged in with session ready for Patient Search
    And the user searches patients on "Api" with first name "<FirstName>" last name "<LastName>" date of birth "<Dob>"
    Then the search result should be "<Outcome>"
    And all authorization executions should pass

    Examples:
      | Role         | FirstName                | LastName            | Dob        | Outcome                   |
      | HospitalRole | fn                       | ln                  | 01-01-1960 | returns patients          |
      | HospitalRole | First Fax                | Iverson             | 01-01-1960 | returns patients          |
      | HospitalRole | ZZZ_NoMatch_SecurityAuto | PatientSearch_NoHit | 01-01-1900 | returns no patients       |
      | HospitalRole | fn                       | ln                  | 01-01-1960 | includes expected patient |
	  | BannerRole   | fn                       | ln                  | 01-01-1960 | returns patients          |
      | HospitalRole |                          |                     |            |                           |
      | BannerRole   |                          |                     |            |                           |



  @Security @PatientSearch @P0 @Authentication @[api/v2/Patient/Search/1]
  Scenario Outline: TC_02Patient Search rejects bad authentication
    When the user "HospitalRole" is logged in with session ready for Patient Search
    And the user searches patients on "Apim" with "<AuthProblem>"
    Then the API should reject the request as unauthorized
    And all authorization executions should pass

    Examples:
      | AuthProblem                  |
      | missing authorization header |
      | empty bearer token           |
      | invalid token                |
      | malformed token              |
      | expired token                |
      | tampered token               |
      | bearer prefix missing        |

  @Security @PatientSearch @P0 @Authorization @[api/v2/Patient/Search/1]
  Scenario: TC_03Hospital user can search patients successfully
    When the user "HospitalRole" is logged in with session ready for Patient Search
    And the user searches patients on "Api" with first name "fn" last name "ln" date of birth "01-01-1960"
    Then the search should succeed for this user
    And all authorization executions should pass

  @Security @PatientSearch @P0 @Authorization @[api/v2/Patient/Search/1]
  Scenario: TC_04 Hospital user must not see other organization patient id in search response
    When the user "HospitalRole" is logged in with session ready for Patient Search
    And the user searches patients on "Api" with first name "fn" last name "ln" date of birth "01-01-1960"
    Then the other organization's patient id must not appear in the response
    And all authorization executions should pass

  @Security @PatientSearch @P0 @Authorization @[api/v2/Patient/Search/1]
  Scenario: TC_05 Banner user can search patients successfully
    When the user "BannerRole" is logged in with session ready for Patient Search
    And the user searches patients on "Api" with first name "fn" last name "ln" date of birth "01-01-1960"
    Then the search should succeed for this user
    And all authorization executions should pass

  @Security @PatientSearch @P0 @InputValidation @[api/v2/Patient/Search/1]
  Scenario Outline: TC_06 Patient Search handles invalid or abusive input safely
    When the user "HospitalRole" is logged in with session ready for Patient Search
    And the user searches patients on "Api" page "<PathSegment>" with first name "<FirstName>" last name "<LastName>" date of birth "<Dob>"
    Then the API should respond with status <ExpectedStatus> and not expose internal errors
    And all authorization executions should pass

    Examples:
      | PathSegment | FirstName | LastName | Dob        | ExpectedStatus |
      | 1           |           | ln       | 01-01-1960 | 400            |
      | abc         | fn        | ln       | 01-01-1960 | 400            |
      | 1           | fn        | ln       | 01-01-1960 | 200            |
      | 1           | ' OR 1=1  | ln       | 01-01-1960 | 400            |
