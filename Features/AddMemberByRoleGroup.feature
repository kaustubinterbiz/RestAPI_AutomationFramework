Feature: Add Member API - Role Group (Excel Loop)
  Reads AddMemberRole child roles from LoginRequest.xlsx -> RoleGroups sheet
  (SuperAdmin, HospitalRole, OrganizationRole) and runs each flow sequentially.

  @Api @AddMemberRole @RoleGroupLoop
  Scenario: Verify add member register API for all roles in AddMemberRole group
    When User executes add member register flow for role group "AddMemberRole"
    Then all role executions in the group should pass

  @Api @AddMemberRole @RoleGroupLoop
  Scenario: Verify add multiple member by excel for all roles in AddMemberRole group
    When User executes add multiple member by excel flow for role group "AddMemberRole"
    Then all role executions in the group should pass
