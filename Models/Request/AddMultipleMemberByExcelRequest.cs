namespace EnterpriseApiAutomationFramework.Models.Request;

public class AddMultipleMemberByExcelRequest
{
    public List<MemberItem> MemberList { get; set; } = new();
    public string BusinessUnitID { get; set; } = string.Empty;
    public bool AddExistingUser { get; set; }
}

public class MemberItem
{
    public int SerialNumber { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string MiddleName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public long WorkNumber { get; set; }
    public long FaxNumber { get; set; }
    public long MobileNumber { get; set; }
    public string IsPrimaryMember { get; set; } = string.Empty;
    public string IsSecondaryMember { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Designation { get; set; } = string.Empty;
   
}
