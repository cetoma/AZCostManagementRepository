using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AZCostManagementMC.Models
{
    public class CustomResponse
    {
        public string Message { get; set; }
        public bool IsError { get; set; } = false;
    }
}
