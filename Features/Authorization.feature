Feature: Authorization API Testing
  Excel-driven RBAC, permission matrix, and token validation.
  Data source: TestData/UploadFiles/AuthorizationMatrix.xlsx

  @Authorization @Api @AuthMatrix
  Scenario Outline: Verify endpoint access by role from authorization matrix
    When Authorization test runs for endpoint "<EndpointKey>" method "<HttpMethod>" role "<Role>"
    Then Authorization status code should be <ExpectedStatus>

    Examples: SessionAccess
      | EndpointKey | HttpMethod | Role             | ExpectedStatus |
      | get         | GET        | SuperAdmin       | 200            |
      | get         | GET        | HospitalRole     | 200            |
      | get         | GET        | OrganizationRole | 200            |

    Examples: ExistingUserAccess
      | EndpointKey     | HttpMethod | Role             | ExpectedStatus |
      | getExistingUser | GET        | SuperAdmin       | 200            |
      | getExistingUser | GET        | HospitalRole     | 401            |
      | getExistingUser | GET        | OrganizationRole | 401            |

    Examples: GuestAccess
      | EndpointKey     | HttpMethod | Role  | ExpectedStatus |
      | getExistingUser | GET        | Guest | 401            |

  @Authorization @Api @AuthMatrix @ExcelLoop
  Scenario: Execute all endpoint access tests from Excel matrix
    When Authorization executes all endpoint access tests from Excel
    Then all authorization executions should pass

  @Authorization @Api @TokenMatrix
  Scenario Outline: Verify token validation scenario from Excel
    When Authorization executes token scenario "<ScenarioType>" for role "<Role>"
    Then Authorization status code should be <ExpectedStatus>

    Examples: TokenScenarios
      | ScenarioType  | Role       | ExpectedStatus |
      | ValidToken    | SuperAdmin | 200            |
      | InvalidToken  | SuperAdmin | 400            |
      | ExpiredToken  | SuperAdmin | 400            |
      | EmptyToken    | -          | 401            |
      | TamperedToken | SuperAdmin | 400            |
      | MissingBearer | SuperAdmin | 400            |

  @Authorization @Api @TokenMatrix @ExcelLoop
  Scenario: Execute all token validation scenarios from Excel
    When Authorization executes all token validation scenarios from Excel
    Then all authorization executions should pass

  @Authorization @Api @PermissionMatrix
  Scenario: Validate permission matrix from Excel with soft assertions
    When Authorization validates all permission rules from Excel
    Then all authorization executions should pass
