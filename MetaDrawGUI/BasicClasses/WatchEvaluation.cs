using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaDrawGUI
{
    public class WatchEvaluation
    {
        public WatchEvaluation(int theScanNumber, double theRT, double watchIsoDecon, double watchChaDecon)
        {
            TheScanNumber = theScanNumber;
            TheRT = theRT;
            WatchIsoDecon = watchIsoDecon;
            WatchChaDecon = watchChaDecon;
        }
        public int TheScanNumber { get; set; }
        public double TheRT { get; set; }
        public double WatchIsoDecon { get; set; }
        public double WatchChaDecon { get; set; }
    }
}
