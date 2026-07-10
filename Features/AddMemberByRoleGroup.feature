Feature: Add Member API - Role Group (Excel Loop)
  One scenario per flow; framework reads AddMemberRole from Excel RoleGroups
  and runs login + API steps for each child role sequentially.

  @Api @AddMemberRole @RoleGroupLoop

  Scenario: Verify add member register API for all roles in AddMemberRole group
    When User executes add member register flow for role group "AddMemberRole"
    Then all role executions in the group should pass

  @Api @AddMemberRole @RoleGroupLoop
  Scenario: Verify add multiple member by excel for all roles in AddMemberRole group
    When User executes add multiple member by excel flow for role group "AddMemberRole"
    Then all role executions in the group should pass
