namespace EnterpriseApiAutomationFramework.Models.Response;

public class GetPACFByBusinessUnitId_ResponseModel
{
    public string BusinessUnitId { get; set; } = string.Empty;
    public string? ParentBusinessUnitId { get; set; }
    public string CompanyType { get; set; } = string.Empty;
    public bool OpenToNonContractedProvider { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Status { get; set; }
    public string Description { get; set; } = string.Empty;
    public string LocationType { get; set; } = string.Empty;
    public bool? IsHeadOffice { get; set; }
    public string Latitude { get; set; } = string.Empty;
    public string Longitude { get; set; } = string.Empty;
    public string BusinessUnitType { get; set; } = string.Empty;
    public string? CreatedBy { get; set; }
    public string? CreatedOn { get; set; }
    public string LastUpdatedBy { get; set; } = string.Empty;
    public string LastUpdatedOn { get; set; } = string.Empty;
    public string WorkNumber { get; set; } = string.Empty;
    public string EmailId { get; set; } = string.Empty;
    public string Fax { get; set; } = string.Empty;
    public List<string> BusinessUnitCategoryType { get; set; } = new();
    public List<BusinessUnitAttribute> BusinessUnitAttributes { get; set; } = new();
}

public class BusinessUnitAttribute
{
    public string EntityName { get; set; } = string.Empty;
    public string EntityValue { get; set; } = string.Empty;
}
