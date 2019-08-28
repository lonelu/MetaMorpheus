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

        public IsoEnvelop((double, double)[] exp, (double, double)[] theo, double mass, int charge)
        {
            ExperimentIsoEnvelop = exp;
            TheoIsoEnvelop = theo;
            MonoisotopicMass = mass;
            Charge = charge;
        }

        public (double, double)[] ExperimentIsoEnvelop { get; set; }

        public (double, double)[] TheoIsoEnvelop { get; set; }

        public double MonoisotopicMass { get; set; }

        public double TotalIntensity
        {
            get
            {
                return ExperimentIsoEnvelop.Sum(p => p.Item2);
            }
        }

        public int Charge { get; set; }

        public int ScanNum { get; set; }
        public double RT { get; set; }

        //For NeuCode Feature
        public bool IsNeuCode { get; set; } = false;

    }
}
