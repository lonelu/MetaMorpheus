using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MassSpectrometry;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using MzLibUtil;
using Chemistry;

namespace MetaDrawGUI
{
    public class ChargeDecon
    {
        static Tolerance tolerance = new PpmTolerance(5);

        static DeconvolutionParameter deconvolutionParameter = new DeconvolutionParameter()
        {
            DeconvolutionMinAssumedChargeState = 6, 
            DeconvolutionMaxAssumedChargeState = 60
        };

        public static int GetCloestIndex(double x, double[] array)
        {
            if (array.Length == 0)
            {
                return 0;
            }
            int index = Array.BinarySearch(array, x);
            if (index >= 0)
            {
                return index;
            }
            index = ~index;

            if (index >= array.Length)
            {
                return index - 1;
            }
            if (index == 0)
            {
                return index;
            }

            if (x - array[index - 1] > array[index] - x)
            {
                return index;
            }
            return index - 1;
        }

        public static double[] GenerateRuler()
        {
            List<double> chargeDiff = new List<double>();
            for (int i = 1; i <= 60; i++)
            {
                chargeDiff.Add(Math.Log(i));
            }
            return chargeDiff.ToArray();
        }

        //Dictinary<charge, mz>
        private static Dictionary<int, double> GenerateMzs(double mz, int charge)
        {
            Dictionary<int, double> mz_z = new Dictionary<int, double>();

            var monomass = mz * charge - charge * 1.0072;

            for (int i = 1; i <= 60; i++)
            {
                mz_z.Add(i, (monomass + i * 1.0072) / i);

            }

            return mz_z;
        }

        private static double ScoreCurrentCharge(MzSpectrumBU mzSpectrumBU_log, List<int> matchedCharges)
        {
            List<int> continuousChangeLength = new List<int>();

            if (matchedCharges.Count <= 1)
            {
                return 1;
            }

            int i = 0;
            int theChargeLength = 1;
            while (i < matchedCharges.Count() - 1)
            {
                int diff = matchedCharges[i + 1] - matchedCharges[i] - 1;
                if (diff == 0)
                {
                    theChargeLength++;
                }
                else
                {
                    continuousChangeLength.Add(theChargeLength);
                    theChargeLength = 1;
                }
                i++;

                if (i == matchedCharges.Count() - 1)
                {
                    continuousChangeLength.Add(theChargeLength);
                }
            }

            return continuousChangeLength.Max();

        }

        public static Dictionary<int, MzPeak> FindChargesForPeak(MzSpectrumBU mzSpectrumBU, int index)
        {
            var mz = mzSpectrumBU.XArray[index];

            Dictionary<int, MzPeak> matched_mz_z = new Dictionary<int, MzPeak>();

            double score = 1;

            //each charge state
            for (int i = 6; i <= 60; i++)
            {
                var mz_z = GenerateMzs(mz, i);

                List<int> matchedIndexes = new List<int>();
                List<int> matchedCharges = new List<int>();

                foreach (var amz in mz_z)
                {
                    var ind = GetCloestIndex(amz.Value, mzSpectrumBU.XArray);

                    if (tolerance.Within(amz.Value, mzSpectrumBU.XArray[ind]))
                    {
                        matchedIndexes.Add(ind);
                        matchedCharges.Add(amz.Key);
                    }
                }

                double theScore = ScoreCurrentCharge(mzSpectrumBU, matchedCharges);

                if (theScore > score)
                {
                    matched_mz_z.Clear();
                    for (int j = 0; j < matchedIndexes.Count(); j++)
                    {
                        matched_mz_z.Add(matchedCharges[j], new MzPeak(mzSpectrumBU.XArray[matchedIndexes[j]], mzSpectrumBU.YArray[matchedIndexes[j]]));
                    }
                    score = theScore;
                }
            }

            return matched_mz_z;
        }

        //public static List<Dictionary<int, MzPeak>> FindChargesForScan(MzSpectrumBU mzSpectrumBU)
        //{
        //    List<Dictionary<int, MzPeak>> mz_zs_list = new List<Dictionary<int, MzPeak>>();
        //    HashSet<double> seenPeaks = new HashSet<double>();

        //    foreach (var peakIndex in mzSpectrumBU.ExtractIndicesByY())
        //    {
        //        if (seenPeaks.Contains(mzSpectrumBU.XArray[peakIndex]))
        //        {
        //            continue;
        //        }

        //        var mz_zs = FindChargesForPeak(mzSpectrumBU, peakIndex);
        //        if (mz_zs.Count >=5)
        //        {
        //            foreach (var mzz in mz_zs)
        //            {
        //                var ind = mzSpectrumBU.GetClosestPeakIndex(mzz.Value.Mz);

        //                var iso = mzSpectrumBU.DeconvolutePeak(ind.Value, deconvolutionParameter);

        //                seenPeaks.Add(mzz.Value.Mz);

        //                if (iso != null)
        //                {
        //                    foreach (var peak in iso.peaks.Select(p => p.mz))
        //                    {
        //                        seenPeaks.Add(peak);
        //                    }
        //                }
        //            }

        //            mz_zs_list.Add(mz_zs);
        //        }
        //    }

        //    return mz_zs_list;
        //}

    }
}
