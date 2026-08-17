Feature: Business Unit API Testing

  @Api @BusinessUnit @[api/BusinessUnit/GetPACFByBusinessUnitID]
  Scenario:01.Verify GET PACF by BusinessUnitID returns business unit details
      When User sends POST request on "Auth" base url with "HospitalRole"
      Then Status should be OK
      And the access token is stored from the last login response
      When User sends GET request for feature "User API Testing" with cached id
      Then Status code should be 200
      And session info from the last response is stored in appsettings
      When User sends flexible "Get" request on "Api" base url for endpoint "getPACFByBusinessUnitID" with url placeholders "BusinessUnitId" target "PACFBusinessUnitID" headers "CacheId" query params "-" body "-"
      Then Status code should be 200
      And business unit info from the last response is stored in appsettings



