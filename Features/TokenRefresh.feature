Feature: Access Token Refresh

  @token-refresh
  Scenario: Expired token returns error and refresh restores access
    Given User has a valid access token on "Auth" base url
    When User applies an expired access token
    When User sends GET request on "Api" base url with current token only
    Then the API status code should be 401
    And Response should indicate token error "expired"
    When User refreshes the access token on "Auth" base url
    When User sends GET request for feature "Access Token Refresh"
    Then the API status code should be 200
