using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnterpriseApiAutomationFramework.Models.Response
{
    public class GetSessionInfo
    {
        public string MemberId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string EmailId { get; set; } = string.Empty;
        public string BusinessUnitId {  get; set; } = string.Empty;
        public string CompanyBusinessUnitId { get; set; } = string.Empty;
        public string BusinessUnitMemberId { get; set; } = string.Empty;
        public string CacheId { get; set; } = string.Empty;
    }
}
