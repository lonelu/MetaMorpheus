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

        public IsoEnvelop((double mz, double intensity)[] exp, (double mz, double intensity)[] theo, double mass, int charge)
        {
            ExperimentIsoEnvelop = exp;
            TheoIsoEnvelop = theo;
            MonoisotopicMass = mass;
            Charge = charge;
        }

        public (double mz, double intensity)[] ExperimentIsoEnvelop { get; set; }

        public (double mz, double intensity)[] TheoIsoEnvelop { get; set; }

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
        public bool HasPartner { get; set; } = false;

        public IsoEnvelop Partner { get; set; }

    }
}
