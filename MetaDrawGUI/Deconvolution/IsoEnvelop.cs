using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MassSpectrometry
{
    public class IsoEnvelop
    {
        public IsoEnvelop() { }

        public IsoEnvelop((double, double)[] exp, (double, double)[] theo, double mass)
        {
            ExperimentIsoEnvelop = exp;
            TheoIsoEnvelop = theo;
            MonoisotopicMass = mass;
        }

        public (double, double)[] ExperimentIsoEnvelop { get; set; }

        public (double, double)[] TheoIsoEnvelop { get; set; }

        public double MonoisotopicMass { get; set; }
    }
}
