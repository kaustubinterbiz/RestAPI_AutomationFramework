using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnterpriseApiAutomationFramework.Models.Response
{
    public class AddMultipleMemberByExcel_ResponseModel
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
        public string Status { get; set; } = string.Empty;
        public string MemberId { get; set; } = string.Empty;
    }
}
