using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnterpriseApiAutomationFramework.Models.Response
{
    public class CheckExistingUser_ResponseModel
    {
        public bool IsAvailableUserEmail { get; set; } = false;
        public string MemberId { get; set; } = string.Empty;
        public bool IsSameBusinessUnitMemebr { get; set; } = false;
    }
}
