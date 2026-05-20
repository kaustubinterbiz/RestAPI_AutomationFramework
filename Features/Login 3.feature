Feature: User API Testing3

Scenario:1.Verify POST User API to get Token
    When User sends POST request
    Then Status should be OK

Scenario:2.Verify GET Product API
    When User sends GET request
    Then Status code should be 200

Scenario:3.Verify POST User API
    When User sends POST request to create
    Then Status should be OK

Scenario:4.Verify PUT User API
    When User sends PUT request
    Then Status code should be 200

Scenario:5.Verify PATCH User API
    When User sends PATCH request
    Then Status code should be 200

Scenario:6.Verify DELETE User API
    When User sends DELETE request
    Then Status code should be 204