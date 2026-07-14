Feature: Add Member API - Role Group (Scenario Outline)
  Same add-member flow runs once per child role under AddMemberRole.
  Roles are defined in LoginRequest.xlsx -> RoleGroups sheet (AddMemberRole group).

  @Api @AddMemberRole @RoleOutline
  Scenario Outline: Verify add member register API for each role in AddMemberRole
    When User sends POST request on "Auth" base url with "<Role>"
    Then Status should be OK
    And the access token is stored from the last login response
    When User sends GET request for feature "User API Testing" with cached id
    Then Status code should be 200
    And session info from the last response is stored in appsettings
    When User sends flexible "Get" request on "Api" base url for endpoint "getCheckAvability" with url placeholders "ValidateCheckExistingEmail_Incorrect, BusinessUnitMemberId" target "ValidateCheckExistingEmail, ValidateBusinessUnitId" headers "CacheId" query params "-" body "-"
    Then validate the response for the existing user in the same organization "ValidateCheckExistingEmail_Incorrect" and "ValidateCheckExistingEmail"
    And Status should be NoContent
    When User sends flexible "Post" request on "Api" base url for endpoint "post_Register" with url placeholders "-" target "-" headers "CacheId" query params "-" body "register_Body"
    Then Status code should be <StatusCode>

    Examples: AddMemberRole
      | Role             | StatusCode |
      | SuperAdmin       | 200        |
      | HospitalRole     | 200        |
      | OrganizationRole | 200        |

  @Api @AddMemberRole @RoleOutline
  Scenario Outline: Verify add multiple member by excel for each role in AddMemberRole
    When User sends POST request on "Auth" base url with "<Role>"
    Then Status should be OK
    And the access token is stored from the last login response
    When User sends GET request for feature "User API Testing" with cached id
    Then Status code should be 200
    And session info from the last response is stored in appsettings
    When User sends flexible "Post" request on "Api" base url for endpoint "addMultipleMemberByExcel" with url placeholders "-" target "-" headers "CacheId" query params "-" body "-"
    Then Status code should be <StatusCode>
    And Store the AddMultipleMemberByExcel response in excel file "Sample_File_Member.xlsx" sheet "Member_ResponseSheet"
    Then Validate the Status should be "<ExpectedMessage>"

    Examples: AddMemberRole
      | Role             | StatusCode | ExpectedMessage                  |
      | SuperAdmin       | 200        | User exists in same organization |
      | HospitalRole     | 200        | User exists in same organization |
      | OrganizationRole | 200        | User exists in same organization |
