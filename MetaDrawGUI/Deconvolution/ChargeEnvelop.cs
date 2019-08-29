using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MassSpectrometry;

namespace MetaDrawGUI
{
    public class ChargeEnvelop
    {
        public List<(int charge, MzPeak peak, IsoEnvelop isoEnvelop)> distributions { get; set; } = new List<(int charge, MzPeak peak, IsoEnvelop isoEnvelop)>();

        public int FirstIndex;

        public double FirstMz;
    }
}
