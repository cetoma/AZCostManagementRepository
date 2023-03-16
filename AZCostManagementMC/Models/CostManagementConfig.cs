
namespace AZCostManagementMC.Models
{
    public class CostManagementConfig
    {
        public int Period { get; set; }
        public string Customer { get; set; }
        public string BillID { get; set; }
        public AzureAppReg AppReg { get; set; }
    }
}
