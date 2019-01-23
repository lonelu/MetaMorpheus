using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MassSpectrometry
{
    public class NeuCodeIsotopicEnvelop : IsotopicEnvelope
    {
        public NeuCodeIsotopicEnvelop(List<(double mz, double intensity)> bestListOfPeaks, double bestMonoisotopicMass, int bestChargeState, double bestTotalIntensity, double bestStDev, int bestMassIndex) :
            base(bestListOfPeaks, bestMonoisotopicMass, bestChargeState, bestTotalIntensity, bestStDev, bestMassIndex)
        {

        }
        
        public bool IsNeuCode { get; set; }
    }
}
