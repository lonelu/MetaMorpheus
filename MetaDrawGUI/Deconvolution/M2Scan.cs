using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Proteomics.Fragmentation;
using MzLibUtil;
using MassSpectrometry;
using Chemistry;

namespace MetaDrawGUI
{
    public class M2Scan
    {
        public static List<IsoEnvelop> MatchFragments(MzSpectrumXY mzSpectrumXY, List<Product> theoreticalProducts, DeconvolutionParameter deconParam, int PrecursorCharge)
        {
            var matchedIsos = new List<IsoEnvelop>();

            // if the spectrum has no peaks
            if (mzSpectrumXY.XArray.Length == 0)
            {
                return matchedIsos;
            }

            var isoEnvelops = GetNeutralExperimentalFragments(mzSpectrumXY, deconParam);
            var DeconvolutedMonoisotopicMasses = isoEnvelops.Select(p => p.MonoisotopicMass).ToArray();

            // search for ions in the spectrum
            foreach (Product product in theoreticalProducts)
            {
                // unknown fragment mass; this only happens rarely for sequences with unknown amino acids
                if (double.IsNaN(product.NeutralMass))
                {
                    continue;
                }

                var indexes = GetAllMassInRange(product.NeutralMass, DeconvolutedMonoisotopicMasses, deconParam.PartnerAcceptor);

                HashSet<int> seenCharge = new HashSet<int>();

                foreach (var ind in indexes)
                {
                    if (seenCharge.Contains(isoEnvelops[ind].Charge))
                    {
                        continue;
                    }
                    seenCharge.Add(isoEnvelops[ind].Charge);
                    isoEnvelops[ind].Product = product;
                    matchedIsos.Add(isoEnvelops[ind]);
                }
            }

            return matchedIsos;
        }

        private static List<int> GetAllMassInRange(double mass, double[] DeconvolutedMonoisotopicMasses, Tolerance tolerance)
        {
            List<int> indexes = new List<int>();
            if (DeconvolutedMonoisotopicMasses.Length == 0)
            {
                return indexes;
            }

            var minMass = tolerance.GetMinimumValue(mass);
            int index = Array.BinarySearch(DeconvolutedMonoisotopicMasses, minMass);

            if (index < 0)
            {
                index = ~index;
            }

            int ind = index;
            while (ind < DeconvolutedMonoisotopicMasses.Length && DeconvolutedMonoisotopicMasses[ind] < tolerance.GetMaximumValue(mass))
            {
                indexes.Add(ind);
                ind++;
            }

            return indexes;
        }

        private static int? GetClosestFragmentMass(double mass, double[] DeconvolutedMonoisotopicMasses)
        {
            if (DeconvolutedMonoisotopicMasses.Length == 0)
            {
                return null;
            }
            int index = Array.BinarySearch(DeconvolutedMonoisotopicMasses, mass);
            if (index >= 0)
            {
                return index;
            }

            index = ~index;

            if (index >= DeconvolutedMonoisotopicMasses.Length)
            {
                return index - 1;
            }
            if (index == 0)
            {
                return index;
            }

            if (mass - DeconvolutedMonoisotopicMasses[index - 1] > DeconvolutedMonoisotopicMasses[index] - mass)
            {
                return index;
            }
            return index - 1;
        }

        private static IsoEnvelop[] GetNeutralExperimentalFragments(MzSpectrumXY mzSpectrumXY, DeconvolutionParameter deconParam)
        {
            var neutralExperimentalFragmentMasses = IsoDecon.MsDeconv_Deconvolute(mzSpectrumXY, mzSpectrumXY.Range, deconParam).ToList();

            //HashSet<double> alreadyClaimedMzs = new HashSet<double>(neutralExperimentalFragmentMasses
            //    .SelectMany(p => p.ExperimentIsoEnvelop.Select(v => Chemistry.ClassExtensions.RoundedDouble(v.Mz).Value)));

            HashSet<int> alreadyClaimedMzIndexes = new HashSet<int>(neutralExperimentalFragmentMasses.SelectMany(p=>p.TheoPeakIndex));

            for (int i = 0; i < mzSpectrumXY.XArray.Length; i++)
            {
                double mz = mzSpectrumXY.XArray[i];
                double intensity = mzSpectrumXY.YArray[i];

                //if (!alreadyClaimedMzs.Contains(Chemistry.ClassExtensions.RoundedDouble(mz).Value))
                if (!alreadyClaimedMzIndexes.Contains(i))
                {
                    var isoEnvelop = new IsoEnvelop();
                    isoEnvelop.ExperimentIsoEnvelop = new MzPeak[] { new MzPeak(mz, intensity) };
                    isoEnvelop.MonoisotopicMass = mz.ToMass(1);
                    isoEnvelop.Charge = 1;
                    neutralExperimentalFragmentMasses.Add(isoEnvelop);
                }
            }


            return neutralExperimentalFragmentMasses.OrderBy(p => p.MonoisotopicMass).ToArray();
        }

    }
}
