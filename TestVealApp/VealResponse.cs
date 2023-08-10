using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestVealApp
{
    public class VealResponse
    {
        public DateTime DateOfRequest { get; set; } = DateTime.Now;
        public VealRequest RequestItem { get; set; }
    }
}
