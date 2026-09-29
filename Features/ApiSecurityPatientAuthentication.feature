Feature: Patient Controller API Security — Authentication (P0)
  JWT rejection checks on 9 Patient API groups from API_Security_Test_Matrix_P0.xlsx.
  Data source: TestData/UploadFiles/API_Security_Test_Matrix_P0.xlsx (sheet 02_Authentication)

  @Security @Authentication @Patient @P0 @ExcelLoop
  Scenario: Patient P0 — run all Authentication tests from Excel matrix
    When Patient Security runs all authentication tests from Excel
    Then all authorization executions should pass

  @Security @Authentication @Patient @P0
  Scenario: AUTH-003-02 GetFhirData rejects missing Authorization header
    When Patient Security runs test "AUTH-003-02"
    Then all authorization executions should pass

  @Security @Authentication @Patient @P0
  Scenario: AUTH-005-02 GetPatientData rejects empty bearer token
    When Patient Security runs test "AUTH-005-02"
    Then all authorization executions should pass

  @Security @Authentication @Patient @P0
  Scenario: AUTH-010-06 Patient Delete rejects tampered signature
    When Patient Security runs test "AUTH-010-06"
    Then all authorization executions should pass
