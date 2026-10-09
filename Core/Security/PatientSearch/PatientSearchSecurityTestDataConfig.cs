using EnterpriseApiAutomationFramework.Core.Configurations;

namespace EnterpriseApiAutomationFramework.Core.Security.PatientSearch;

public static class PatientSearchSecurityTestDataConfig
{
    private const string Section = "PatientSearchSecurityTestData";

    public static string DefaultFirstName =>
        FirstNonEmpty(Get($"{Section}:DefaultFirstName"), "fn");

    public static string DefaultLastName =>
        FirstNonEmpty(Get($"{Section}:DefaultLastName"), "ln");

    public static string DefaultDob =>
        FirstNonEmpty(Get($"{Section}:DefaultDob"), "01-01-1960");

    public static string NoMatchFirstName =>
        FirstNonEmpty(Get($"{Section}:NoMatchFirstName"), "ZZZ_NoMatch_SecurityAuto");

    public static string NoMatchLastName =>
        FirstNonEmpty(Get($"{Section}:NoMatchLastName"), "PatientSearch_NoHit");

    public static string NoMatchDob =>
        FirstNonEmpty(Get($"{Section}:NoMatchDob"), "01-01-1900");

    public static string ExpectedPatientId =>
        FirstNonEmpty(Get($"{Section}:ExpectedPatientId"), string.Empty);

    public static (string FirstName, string LastName, string Dob) ResolveDemographics(
        string firstName,
        string lastName,
        string dob)
    {
        if (string.Equals(firstName, "default", StringComparison.OrdinalIgnoreCase))
            firstName = DefaultFirstName;
        if (string.Equals(lastName, "default", StringComparison.OrdinalIgnoreCase))
            lastName = DefaultLastName;
        if (string.Equals(dob, "default", StringComparison.OrdinalIgnoreCase))
            dob = DefaultDob;

        return (firstName.Trim(), lastName.Trim(), dob.Trim());
    }

    private static string? Get(string key)
    {
        ConfigReaderNew.LoadConfig("appsettings.json");
        var value = ConfigReaderNew.GetValue(key);
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? string.Empty;
}
