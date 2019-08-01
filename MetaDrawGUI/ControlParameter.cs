using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaDrawGUI
{
    public class ControlParameter
    {
        public Tuple<double, double> LCTimeRange { get; set; } = new Tuple<double, double>(0, 150);

        public int deconScanNum { get; set; } = new int();

        public int modelStartNum { get; set; } = new int();

    }
}
