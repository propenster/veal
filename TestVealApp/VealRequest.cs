using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestVealApp
{
    public class VealRequest
    {
        public string name { get; set; }
        public string age { get; set; }
        public int level { get; set; }
        public double fee { get; set; }
        public List<Item> items { get; set; } = new List<Item>();
    }
    public class Item
    {
        public string type1 { get; set; }
        public int level { get; set; }
    }



}
