Feature: User API Testing

Scenario: Verify POST User API to get Token
    When User sends POST request
    Then Status should be OK

Scenario: Verify GET Product API
    When User sends GET request
    Then Status code should be 200

Scenario: Verify POST User API
    When User sends POST request
    Then Status should be OK

Scenario: Verify PUT User API
    When User sends PUT request
    Then Status code should be 200

Scenario: Verify PATCH User API
    When User sends PATCH request
    Then Status code should be 200

Scenario: Verify DELETE User API
    When User sends DELETE request
    Then Status code should be 204