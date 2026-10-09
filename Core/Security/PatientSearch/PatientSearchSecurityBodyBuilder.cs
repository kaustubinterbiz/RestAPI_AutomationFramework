using System.Text.Json;

namespace EnterpriseApiAutomationFramework.Core.Security.PatientSearch;

/// <summary>
/// APIM curl-aligned POST body: PatientDemographic.Filter only (no Insurance/Mode/OrderBy).
/// </summary>
public static class PatientSearchSecurityBodyBuilder
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = null,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
    };

    public static string Build(string firstName, string lastName, string dob)
    {
        var payload = new
        {
            PatientDemographic = new
            {
                Filter = new
                {
                    Ssn = new object?[] { null },
                    Dob = new[] { dob },
                    FirstName = new[] { firstName },
                    LastName = new[] { lastName },
                    Gender = new object?[] { null },
                    Zipcode = Array.Empty<string>(),
                    StreetAddress = Array.Empty<string>(),
                    City = Array.Empty<string>(),
                    Email = Array.Empty<string>(),
                    Phone = Array.Empty<string>(),
                    InsuranceId = Array.Empty<string>(),
                    InsuranceName = Array.Empty<string>()
                }
            }
        };

        return JsonSerializer.Serialize(payload, SerializerOptions);
    }
}
