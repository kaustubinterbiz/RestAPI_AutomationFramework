Feature: Patient Controller API Security — IDOR / Cross-Org (P0)
  IDOR and cross-organization access checks for Patient APIs.
  Data source: TestData/UploadFiles/API_Security_Test_Matrix_P0.xlsx (sheet 03_IDOR_CrossOrg)

  @Security @Patient @IDOR @P0 @ExcelLoop
  Scenario: Patient P0 — run all IDOR/Cross-Org tests from Excel matrix
    When Patient Security runs all IDOR tests from Excel
    Then all authorization executions should pass

  @Security @Patient @IDOR @P0
  Scenario: IDOR-005-01 GetPatientData cross-org patientId denied
    When Patient Security runs test "IDOR-005-01"
    Then all authorization executions should pass

  @Security @Patient @IDOR @P0
  Scenario: IDOR-011-01 PatientAttribute cross-org query denied
    When Patient Security runs test "IDOR-011-01"
    Then all authorization executions should pass
