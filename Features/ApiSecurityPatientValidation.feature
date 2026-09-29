Feature: Patient Controller API Security — Input Validation (P0)
  Input validation and mass-assignment checks for Patient APIs.
  Data source: TestData/UploadFiles/API_Security_Test_Matrix_P0.xlsx (sheet 04_Input_Validation)

  @Security @Patient @InputValidation @P0 @ExcelLoop
  Scenario: Patient P0 — run all Input Validation tests from Excel matrix
    When Patient Security runs all input validation tests from Excel
    Then all authorization executions should pass

  @Security @Patient @InputValidation @P0
  Scenario: IV-005-01 GetPatientData invalid GUID rejected
    When Patient Security runs test "IV-005-01"
    Then all authorization executions should pass

  @Security @Patient @InputValidation @P0
  Scenario: IV-013-01 Bulk providers array abuse rejected
    When Patient Security runs test "IV-013-01"
    Then all authorization executions should pass
 