using Chemistry;
using MathNet.Numerics.Statistics;
using MzLibUtil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EngineLayer;
using MetaDrawGUI;

namespace MassSpectrometry
{
    public class MzSpectrumBU
    {
        private const int numAveraginesToGenerate = 1500;
        private SinglePpmAroundZeroSearchMode _neuCodePpmTolerance = new SinglePpmAroundZeroSearchMode(10);
        private static readonly double[][] allMasses = new double[numAveraginesToGenerate][];
        private static readonly double[][] allIntensities = new double[numAveraginesToGenerate][];
        private static readonly double[] mostIntenseMasses = new double[numAveraginesToGenerate];
        private static readonly double[] diffToMonoisotopic = new double[numAveraginesToGenerate];

        public double[] XArray { get; private set; }
        public double[] YArray { get; private set; }

        public double[][] AllMasses { get { return allMasses; } }
        public double[][] AllIntensities { get { return allIntensities; } }

        static MzSpectrumBU()
        {
            //AVERAGINE
            const double averageC = 4.9384;
            const double averageH = 7.7583;
            const double averageO = 1.4773;
            const double averageN = 1.3577;
            const double averageS = 0.0417;

            //Glycopeptide Averagine
            //const double averageC = 10.93;
            //const double averageH = 15.75;
            //const double averageO = 6.48;
            //const double averageN = 1.66;
            //const double averageS = 0.02;

            const double fineRes = 0.125;
            const double minRes = 1e-8;

            for (int i = 0; i < numAveraginesToGenerate; i++)
            {
                double averagineMultiplier = (i + 1) / 2.0;
                //Console.Write("numAveragines = " + numAveragines);
                ChemicalFormula chemicalFormula = new ChemicalFormula();
                chemicalFormula.Add("C", Convert.ToInt32(averageC * averagineMultiplier));
                chemicalFormula.Add("H", Convert.ToInt32(averageH * averagineMultiplier));
                chemicalFormula.Add("O", Convert.ToInt32(averageO * averagineMultiplier));
                chemicalFormula.Add("N", Convert.ToInt32(averageN * averagineMultiplier));
                chemicalFormula.Add("S", Convert.ToInt32(averageS * averagineMultiplier));

                {
                    var chemicalFormulaReg = chemicalFormula;
                    IsotopicDistribution ye = IsotopicDistribution.GetDistribution(chemicalFormulaReg, fineRes, minRes);
                    var masses = ye.Masses.ToArray();
                    var intensities = ye.Intensities.ToArray();
                    Array.Sort(intensities, masses);
                    Array.Reverse(intensities);
                    Array.Reverse(masses);

                    mostIntenseMasses[i] = masses[0];
                    diffToMonoisotopic[i] = masses[0] - chemicalFormulaReg.MonoisotopicMass;
                    allMasses[i] = masses;
                    allIntensities[i] = intensities;
                }
            }
        }

        public MzSpectrumBU(double[] mz, double[] intensities, bool shouldCopy)
        {
            if (shouldCopy)
            {
                XArray = new double[mz.Length];
                YArray = new double[intensities.Length];
                Array.Copy(mz, XArray, mz.Length);
                Array.Copy(intensities, YArray, intensities.Length);
            }
            else
            {
                XArray = mz;
                YArray = intensities;
            }
        }

        public MzRange Range
        {
            get
            {
                if (Size == 0)
                {
                    return null;
                }
                return new MzRange(FirstX.Value, LastX.Value);
            }
        }

        public double? FirstX
        {
            get
            {
                if (Size == 0)
                {
                    return null;
                }
                return XArray[0];
            }
        }

        public double? LastX
        {
            get
            {
                if (Size == 0)
                {
                    return null;
                }
                return XArray[Size - 1];
            }
        }

        public int Size { get { return XArray.Length; } }

        private NeuCodeIsotopicEnvelop GetEnvelopForPeakAtChargeState(double candidateForMostIntensePeakMz, double candidateForMostIntensePeakIntensity, int massIndex, int chargeState, DeconvolutionParameter deconvolutionParameter)
        {
            var listOfPeaks = new List<(double, double)> { (candidateForMostIntensePeakMz, candidateForMostIntensePeakIntensity) };
            var listOfRatios = new List<double> { allIntensities[massIndex][0] / candidateForMostIntensePeakIntensity };

            // Assuming the test peak is most intense...
            // Try to find the rest of the isotopes!


            double differenceBetweenTheorAndActual = candidateForMostIntensePeakMz.ToMass(chargeState) - mostIntenseMasses[massIndex];
            double totalIntensity = candidateForMostIntensePeakIntensity;
            for (int indexToLookAt = 1; indexToLookAt < allIntensities[massIndex].Length; indexToLookAt++)
            {
                //Console.WriteLine("   indexToLookAt: " + indexToLookAt);
                double theorMassThatTryingToFind = allMasses[massIndex][indexToLookAt] + differenceBetweenTheorAndActual;
                //Console.WriteLine("   theorMassThatTryingToFind: " + theorMassThatTryingToFind);
                //Console.WriteLine("   theorMassThatTryingToFind.ToMz(chargeState): " + theorMassThatTryingToFind.ToMz(chargeState));
                var closestPeakToTheorMass = GetClosestPeakIndex(theorMassThatTryingToFind.ToMz(chargeState));
                var closestPeakmz = XArray[closestPeakToTheorMass.Value];
                //Console.WriteLine("   closestPeakmz: " + closestPeakmz);
                var closestPeakIntensity = YArray[closestPeakToTheorMass.Value];
                if (Math.Abs(closestPeakmz.ToMass(chargeState) - theorMassThatTryingToFind) / theorMassThatTryingToFind * 1e6 <= deconvolutionParameter.DeconvolutionMassTolerance
                    && Peak2satisfiesRatio(allIntensities[massIndex][0], allIntensities[massIndex][indexToLookAt], candidateForMostIntensePeakIntensity, closestPeakIntensity, deconvolutionParameter.DeconvolutionIntensityRatio)
                    && !listOfPeaks.Contains((closestPeakmz, closestPeakIntensity)))
                {
                    //Found a match to an isotope peak for this charge state!
                    //Console.WriteLine(" *   Found a match to an isotope peak for this charge state!");
                    //Console.WriteLine(" *   chargeState: " + chargeState);
                    //Console.WriteLine(" *   closestPeakmz: " + closestPeakmz);
                    listOfPeaks.Add((closestPeakmz, closestPeakIntensity));
                    totalIntensity += closestPeakIntensity;
                    listOfRatios.Add(allIntensities[massIndex][indexToLookAt] / closestPeakIntensity);
                }
                else
                {
                    break;
                }
            }

            var extrapolatedMonoisotopicMass = candidateForMostIntensePeakMz.ToMass(chargeState) - diffToMonoisotopic[massIndex]; // Optimized for proteoforms!!
            var lowestMass = listOfPeaks.Min(b => b.Item1).ToMass(chargeState); // But may actually observe this small peak
            var monoisotopicMass = Math.Abs(extrapolatedMonoisotopicMass - lowestMass) < 0.5 ? lowestMass : extrapolatedMonoisotopicMass;

            var TheoryMasses = allMasses[massIndex].Select(p => p = p + differenceBetweenTheorAndActual).ToArray();
            var TheoryIntensities = allIntensities[massIndex];
            Array.Sort(TheoryMasses, TheoryIntensities);

            NeuCodeIsotopicEnvelop test = new NeuCodeIsotopicEnvelop(listOfPeaks, monoisotopicMass, chargeState, totalIntensity, MathNet.Numerics.Statistics.Statistics.StandardDeviation(listOfRatios), massIndex);

            return test;
        }

        public NeuCodeIsotopicEnvelop DeconvolutePeak(int candidateForMostIntensePeak, DeconvolutionParameter deconvolutionParameter)
        {
            NeuCodeIsotopicEnvelop bestIsotopeEnvelopeForThisPeak = null;

            var candidateForMostIntensePeakMz = XArray[candidateForMostIntensePeak];

            //Console.WriteLine("candidateForMostIntensePeakMz: " + candidateForMostIntensePeakMz);
            var candidateForMostIntensePeakIntensity = YArray[candidateForMostIntensePeak];

            //TO DO: Find possible chargeState.
            List<int> allPossibleChargeState = new List<int>();
            for (int i = candidateForMostIntensePeak + 1; i < XArray.Length; i++)
            {
                if (XArray[i] - candidateForMostIntensePeakMz > 0.01 && XArray[i] - candidateForMostIntensePeakMz < 0.8)
                {
                    var chargeDouble = 1 / (XArray[i] - candidateForMostIntensePeakMz);
                    int charge = Convert.ToInt32(chargeDouble);
                    if (Math.Abs(chargeDouble - charge) <= 0.1 && charge >= deconvolutionParameter.DeconvolutionMinAssumedChargeState && charge <= deconvolutionParameter.DeconvolutionMaxAssumedChargeState)
                    {
                        allPossibleChargeState.Add(charge);
                    }
                }
                else
                {
                    break;
                }
            }

            foreach (var chargeState in allPossibleChargeState)
            {
                //Console.WriteLine(" chargeState: " + chargeState);
                var testMostIntenseMass = candidateForMostIntensePeakMz.ToMass(chargeState);

                var massIndex = Array.BinarySearch(mostIntenseMasses, testMostIntenseMass);
                if (massIndex < 0)
                    massIndex = ~massIndex;
                if (massIndex == mostIntenseMasses.Length)
                {
                    //Console.WriteLine("Breaking  because mass is too high: " + testMostIntenseMass);
                    break;
                }
                //Console.WriteLine("  massIndex: " + massIndex);

                var test = GetEnvelopForPeakAtChargeState(candidateForMostIntensePeakMz, candidateForMostIntensePeakIntensity, massIndex, chargeState, deconvolutionParameter);

                if (test.peaks.Count >= 2 && test.stDev < 0.00001 && ScoreIsotopeEnvelope(test) > ScoreIsotopeEnvelope(bestIsotopeEnvelopeForThisPeak))
                {
                    //Console.WriteLine("Better charge state is " + test.charge);
                    //Console.WriteLine("peaks: " + string.Join(",", listOfPeaks.Select(b => b.Item1)));
                    bestIsotopeEnvelopeForThisPeak = test;
                }
            }
            return bestIsotopeEnvelopeForThisPeak;
        }

        public IEnumerable<NeuCodeIsotopicEnvelop> Deconvolute(MzRange theRange, DeconvolutionParameter deconvolutionParameter)
        {
            if (Size == 0)
            {
                yield break;
            }

            var isolatedMassesAndCharges = new List<NeuCodeIsotopicEnvelop>();

            HashSet<double> seenPeaks = new HashSet<double>();

            ////Deconvolution by Intensity decending order
            //foreach (var candidateForMostIntensePeak in ExtractIndicesByY())
            ////Deconvolution by MZ increasing order
            foreach (var candidateForMostIntensePeak in ExtractIndices(theRange.Minimum, theRange.Maximum))
            {
                if (seenPeaks.Contains(XArray[candidateForMostIntensePeak]))
                {
                    continue;
                }

                NeuCodeIsotopicEnvelop bestIsotopeEnvelopeForThisPeak = DeconvolutePeak(candidateForMostIntensePeak, deconvolutionParameter);

                if (bestIsotopeEnvelopeForThisPeak != null && bestIsotopeEnvelopeForThisPeak.peaks.Count >= 2)
                {
                    isolatedMassesAndCharges.Add(bestIsotopeEnvelopeForThisPeak);
                    foreach (var peak in bestIsotopeEnvelopeForThisPeak.peaks.Select(p => p.mz))
                    {
                        seenPeaks.Add(peak);
                    }
                }
            }

            HashSet<double> seen = new HashSet<double>(); //Do we still need this

            foreach (var ok in isolatedMassesAndCharges.OrderByDescending(b => ScoreIsotopeEnvelope(b)))
            {
                if (seen.Overlaps(ok.peaks.Select(b => b.mz)))
                {
                    continue;
                }
                foreach (var ah in ok.peaks.Select(b => b.mz))
                {
                    seen.Add(ah);
                }
                yield return ok;
            }
        }

        #region New deconvolution method optimized from MsDeconv (by Lei)
        
        //MsDeconv Score peak
        private double MsDeconvScore_peak((double, double) experiment, (double, double) theory, double mass_error_tolerance = 0.02)
        {
            double score = 0;

            double mass_error = Math.Abs(experiment.Item1 - theory.Item1);

            double mass_accuracy = 0;

            if (mass_error <= 0.02)
            {
                mass_accuracy = 1 - mass_error / mass_error_tolerance;
            }

            double abundance_diff = 0;

            if (experiment.Item2 < theory.Item2 && (theory.Item2 - experiment.Item2)/experiment.Item2 <= 1)
            {
                abundance_diff = 1 - (theory.Item2 - experiment.Item2) / experiment.Item2;
            }
            else if (experiment.Item2 >= theory.Item2 && (experiment.Item2 - theory.Item2) / experiment.Item2 <= 1)
            {
                abundance_diff = Math.Sqrt(1 - (experiment.Item2 - theory.Item2) / experiment.Item2);
            }

            score = Math.Sqrt(theory.Item2) * mass_accuracy * abundance_diff;

            return score;
        }

        //MsDeconv Envelop
        private double MsDeconvScore(IsoEnvelop isoEnvelop)
        {
            double score = 0;

            if (isoEnvelop ==null)
            {
                return score;
            }

            for (int i = 0; i < isoEnvelop.TheoIsoEnvelop.Length; i++)
            {
                score += MsDeconvScore_peak(isoEnvelop.ExperimentIsoEnvelop[i], isoEnvelop.TheoIsoEnvelop[i]);
            }

            return score;
        }

        //Scale Theoretical Envelope
        private (double, double)[] ScaleTheoEnvelop((double, double)[] experiment, (double, double)[] theory, string method = "sum")
        {
            var scale_Theory = new (double, double)[theory.Length];
            switch (method)
            {
                case "sum":
                    var total_abundance = experiment.Sum(p => p.Item2);
                    scale_Theory = theory.Select(p => (p.Item1, p.Item2 * total_abundance)).ToArray();
                    break;
                default:
                    break;
            }
            return scale_Theory;
        }

        //Change the workflow for different score method.
        private IsoEnvelop GetETEnvelopForPeakAtChargeState(double candidateForMostIntensePeakMz, int chargeState, DeconvolutionParameter deconvolutionParameter, double noiseLevel)
        {
            var testMostIntenseMass = candidateForMostIntensePeakMz.ToMass(chargeState);

            var massIndex = Array.BinarySearch(mostIntenseMasses, testMostIntenseMass);
            if (massIndex < 0)
                massIndex = ~massIndex;
            if (massIndex == mostIntenseMasses.Length)
            {
                return null;
            }

            // Assuming the test peak is most intense...
            // Try to find the rest of the isotopes!

            double differenceBetweenTheorAndActual = candidateForMostIntensePeakMz.ToMass(chargeState) - mostIntenseMasses[massIndex];

            double theoryIntensityCut = 0;
            int theoryIsoEnvelopLength = 0;
            for (int i = 0; i < allIntensities[massIndex].Length; i++)
            {
                theoryIsoEnvelopLength++;
                theoryIntensityCut += allIntensities[massIndex][i];
                if (theoryIntensityCut >= 0.85 && i >= 2)
                {
                    break;
                }             
            }

            var arrayOfPeaks = new (double, double)[theoryIsoEnvelopLength];
            var arrayOfTheoPeaks = new (double, double)[theoryIsoEnvelopLength];

            for (int indexToLookAt = 0; indexToLookAt < theoryIsoEnvelopLength; indexToLookAt++)
            {

                double theorMassThatTryingToFind = allMasses[massIndex][indexToLookAt] + differenceBetweenTheorAndActual;
                arrayOfTheoPeaks[indexToLookAt] = (theorMassThatTryingToFind.ToMz(chargeState), allIntensities[massIndex][indexToLookAt]);

                var closestPeakToTheorMass = GetClosestPeakIndex(theorMassThatTryingToFind.ToMz(chargeState));
                var closestPeakmz = XArray[closestPeakToTheorMass.Value];

                var closestPeakIntensity = YArray[closestPeakToTheorMass.Value];

                if (!deconvolutionParameter.DeconvolutionAcceptor.Within(theorMassThatTryingToFind, closestPeakmz.ToMass(chargeState)) || closestPeakIntensity < noiseLevel)
                {
                    closestPeakmz = theorMassThatTryingToFind;
                    closestPeakIntensity = 0;
                }

                arrayOfPeaks[indexToLookAt] = (closestPeakmz, closestPeakIntensity);
            }

            if (FilterEEnvelop(arrayOfPeaks))
            {
                var scaleArrayOfTheoPeaks = ScaleTheoEnvelop(arrayOfPeaks, arrayOfTheoPeaks);

                //The following 3 lines are for calculating monoisotopicMass, origin from Stephan, I don't understand it, and may optimize it in the future. (Lei)
                var extrapolatedMonoisotopicMass = candidateForMostIntensePeakMz.ToMass(chargeState) - diffToMonoisotopic[massIndex]; // Optimized for proteoforms!!
                var lowestMass = arrayOfPeaks.Min(b => b.Item1).ToMass(chargeState); // But may actually observe this small peak
                var monoisotopicMass = Math.Abs(extrapolatedMonoisotopicMass - lowestMass) < 0.5 ? lowestMass : extrapolatedMonoisotopicMass;

                IsoEnvelop isoEnvelop = new IsoEnvelop(arrayOfPeaks, scaleArrayOfTheoPeaks, monoisotopicMass, chargeState);
                return isoEnvelop;
            }

            return null;
        }

        private int GetConsecutiveLength((double, double)[] experiment)
        {
            List<int> inds = new List<int>();
            for (int i = 0; i < experiment.Length; i++)
            {
                if (experiment[i].Item2==0)
                {
                    inds.Add(i);
                }
            }

            if (inds.Count == 1)
            {
                return inds.First();
            }
            else if(inds.Count >1)
            {
                List<int> lens = new List<int>();

                lens.Add(inds[0]);

                for (int i = 0; i < inds.Count() - 1; i++)
                {
                    lens.Add(inds[i + 1] - inds[i]);
                }

                return lens.Max();
            }

            return experiment.Length;
        }

        private bool FilterEEnvelop((double, double)[] experiment)
        {
            int consecutiveLength = GetConsecutiveLength(experiment);
            if (consecutiveLength < 3 || consecutiveLength < (experiment.Length - 3))
            {
                return false;
            }

            return true;
        }

        public IsoEnvelop DeconvoluteExperimentPeak(int candidateForMostIntensePeak, DeconvolutionParameter deconvolutionParameter, double noiseLevel)
        {
            IsoEnvelop bestIsotopeEnvelopeForThisPeak = null;

            var candidateForMostIntensePeakMz = XArray[candidateForMostIntensePeak];

            //Find possible chargeStates.
            List<int> allPossibleChargeState = new List<int>();
            for (int i = candidateForMostIntensePeak + 1; i < XArray.Length; i++)
            {
                if (XArray[i] - candidateForMostIntensePeakMz < 1.1) //In case charge is +1
                {
                    var chargeDouble = 1.00289 / (XArray[i] - candidateForMostIntensePeakMz);
                    int charge = Convert.ToInt32(chargeDouble);
                    if (deconvolutionParameter.DeconvolutionAcceptor.Within(candidateForMostIntensePeakMz + 1.00289/ chargeDouble, XArray[i]) 
                        && charge >= deconvolutionParameter.DeconvolutionMinAssumedChargeState 
                        && charge <= deconvolutionParameter.DeconvolutionMaxAssumedChargeState
                        && !allPossibleChargeState.Contains(charge))
                    {
                        allPossibleChargeState.Add(charge);
                    }
                }
                else
                {
                    break;
                }
            }

            foreach (var chargeState in allPossibleChargeState)
            {
                var isoEnvelop = GetETEnvelopForPeakAtChargeState(candidateForMostIntensePeakMz,  chargeState, deconvolutionParameter, noiseLevel);

                if (MsDeconvScore(isoEnvelop) > MsDeconvScore(bestIsotopeEnvelopeForThisPeak))
                {
                    bestIsotopeEnvelopeForThisPeak = isoEnvelop;
                }
            }
            return bestIsotopeEnvelopeForThisPeak;
        }

        private double CalNoiseLevel()
        {
            return 0;
        }

        public IEnumerable<IsoEnvelop> MsDeconv_Deconvolute(MzRange theRange, DeconvolutionParameter deconvolutionParameter)
        {
            if (Size == 0)
            {
                yield break;
            }

            var isolatedMassesAndCharges = new List<IsoEnvelop>();

            //HashSet<double> seenPeaks = new HashSet<double>();

            ////Deconvolution by Intensity decending order
            //foreach (var candidateForMostIntensePeak in ExtractIndicesByY())
            ////Deconvolution by MZ increasing order
            foreach (var candidateForMostIntensePeak in ExtractIndices(theRange.Minimum, theRange.Maximum))
            {
                //if (seenPeaks.Contains(XArray[candidateForMostIntensePeak]))
                //{
                //    continue;
                //}

                double noiseLevel = CalNoiseLevel();

                IsoEnvelop bestIsotopeEnvelopeForThisPeak = DeconvoluteExperimentPeak(candidateForMostIntensePeak, deconvolutionParameter, noiseLevel);

                if (bestIsotopeEnvelopeForThisPeak != null)
                {
                    isolatedMassesAndCharges.Add(bestIsotopeEnvelopeForThisPeak);
                    //foreach (var peak in bestIsotopeEnvelopeForThisPeak.ExperimentIsoEnvelop.Select(p => p.Item1))
                    //{
                    //    seenPeaks.Add(peak);
                    //}
                }
            }

            HashSet<double> seen = new HashSet<double>(); //Do we still need this

            List<IsoEnvelop> isoEnvelops = new List<IsoEnvelop>();

            foreach (var ok in isolatedMassesAndCharges.OrderByDescending(b => MsDeconvScore(b)))
            {
                if (seen.Overlaps(ok.ExperimentIsoEnvelop.Select(b => b.Item1)))
                {
                    continue;
                }
                foreach (var ah in ok.ExperimentIsoEnvelop.Select(b => b.Item1))
                {
                    seen.Add(ah);
                }
                //yield return ok;
                isoEnvelops.Add(ok);
            }

            var orderedIsoEnvelops = isoEnvelops.OrderBy(p => p.MonoisotopicMass).ToList();
            FindLabelPair(orderedIsoEnvelops, deconvolutionParameter);
            foreach (var iso in orderedIsoEnvelops)
            {
                yield return iso;
            }
        }

        #endregion

        //DeconvolutePeak_NeuCode
        public NeuCodeIsotopicEnvelop DeconvolutePeak_NeuCode(int candidateForMostIntensePeak, DeconvolutionParameter deconvolutionParameter)
        {
            NeuCodeIsotopicEnvelop bestIsotopeEnvelopeForThisPeak = null;

            var candidateForMostIntensePeakMz = XArray[candidateForMostIntensePeak];

            //Console.WriteLine("candidateForMostIntensePeakMz: " + candidateForMostIntensePeakMz);
            var candidateForMostIntensePeakIntensity = YArray[candidateForMostIntensePeak];

            //TO DO: Find possible chargeState.
            List<int> allPossibleChargeState = new List<int>();
            for (int i = candidateForMostIntensePeak + 1; i < XArray.Length; i++)
            {
                if (XArray[i] - candidateForMostIntensePeakMz > 0.01 && XArray[i] - candidateForMostIntensePeakMz < 0.8)
                {
                    var chargeDouble = 1 / (XArray[i] - candidateForMostIntensePeakMz);
                    int charge = Convert.ToInt32(chargeDouble);
                    if (Math.Abs(chargeDouble - charge) <= 0.1 && charge >= deconvolutionParameter.DeconvolutionMinAssumedChargeState && charge <= deconvolutionParameter.DeconvolutionMaxAssumedChargeState)
                    {
                        allPossibleChargeState.Add(charge);
                    }
                }
                else
                {
                    break;
                }
            }

            foreach (var chargeState in allPossibleChargeState)
            {
                //Console.WriteLine(" chargeState: " + chargeState);
                var testMostIntenseMass = candidateForMostIntensePeakMz.ToMass(chargeState);

                var massIndex = Array.BinarySearch(mostIntenseMasses, testMostIntenseMass);
                if (massIndex < 0)
                    massIndex = ~massIndex;
                if (massIndex == mostIntenseMasses.Length)
                {
                    //Console.WriteLine("Breaking  because mass is too high: " + testMostIntenseMass);
                    break;
                }
                //Console.WriteLine("  massIndex: " + massIndex);

                var test = GetEnvelopForPeakAtChargeState(candidateForMostIntensePeakMz, candidateForMostIntensePeakIntensity, massIndex, chargeState, deconvolutionParameter);

                if (test.peaks.Count >= 2 && test.stDev < 0.00001 && ScoreIsotopeEnvelope(test) > ScoreIsotopeEnvelope(bestIsotopeEnvelopeForThisPeak))
                {
                    //Console.WriteLine("Better charge state is " + test.charge);
                    //Console.WriteLine("peaks: " + string.Join(",", listOfPeaks.Select(b => b.Item1)));
                    bestIsotopeEnvelopeForThisPeak = test;
                }
            }

            if (bestIsotopeEnvelopeForThisPeak != null)
            {
                var pairEnvelop = GetNeucodeEnvelopForThisEnvelop(bestIsotopeEnvelopeForThisPeak, deconvolutionParameter);
                if (bestIsotopeEnvelopeForThisPeak.IsNeuCode)
                {
                    bestIsotopeEnvelopeForThisPeak.Partner = pairEnvelop;
                }
            }

            return bestIsotopeEnvelopeForThisPeak;
        }

        // Mass tolerance must account for different isotope spacing!
        public IEnumerable<NeuCodeIsotopicEnvelop> DeconvoluteBU_NeuCode(MzRange theRange, DeconvolutionParameter deconvolutionParameter)
        {
            if (Size == 0)
            {
                yield break;
            }

            var isolatedMassesAndCharges = new List<NeuCodeIsotopicEnvelop>();

            HashSet<double> seenPeaks = new HashSet<double>();
            //int cut = 50;

            ////Deconvolution by Intensity decending order
            foreach (var candidateForMostIntensePeak in ExtractIndicesByY())
            ////Deconvolution by MZ increasing order
            //foreach (var candidateForMostIntensePeak in ExtractIndices(theRange.Minimum, theRange.Maximum))
            {

                if (seenPeaks.Contains(XArray[candidateForMostIntensePeak]))
                {
                    continue;
                }

                NeuCodeIsotopicEnvelop bestIsotopeEnvelopeForThisPeak = DeconvolutePeak(candidateForMostIntensePeak, deconvolutionParameter);

                if (bestIsotopeEnvelopeForThisPeak != null && bestIsotopeEnvelopeForThisPeak.peaks.Count >= 2)
                {

                    var pairEnvelop = GetNeucodeEnvelopForThisEnvelop(bestIsotopeEnvelopeForThisPeak, deconvolutionParameter);
                   
                    foreach (var peak in bestIsotopeEnvelopeForThisPeak.peaks.Select(p => p.mz))
                    {
                        seenPeaks.Add(peak);
                    }

                    if (pairEnvelop != null)
                    {
                        bestIsotopeEnvelopeForThisPeak.Partner = pairEnvelop;

                        foreach (var peak in pairEnvelop.peaks.Select(p => p.mz))
                        {
                            seenPeaks.Add(peak);
                        }
                    }                   

                    isolatedMassesAndCharges.Add(bestIsotopeEnvelopeForThisPeak);
                }

                //if (isolatedMassesAndCharges.Count > cut * 2)
                //{
                //    break;
                //}
            }

            HashSet<double> seen = new HashSet<double>();

            foreach (var ok in isolatedMassesAndCharges.OrderByDescending(b => ScoreIsotopeEnvelope(b)))
            {
                if (seen.Overlaps(ok.peaks.Select(b => b.mz)))
                {
                    continue;
                }
                foreach (var ah in ok.peaks.Select(b => b.mz))
                {
                    seen.Add(ah);
                }
                yield return ok;
            }
        }

        public IEnumerable<int> ExtractIndices(double minX, double maxX)
        {
            int ind = Array.BinarySearch(XArray, minX);
            if (ind < 0)
            {
                ind = ~ind;
            }
            while (ind < Size && XArray[ind] <= maxX)
            {
                yield return ind;
                ind++;
            }
        }

        //TO DO: speed up this function. 
        public IEnumerable<int> ExtractIndicesByY()
        {
            var YArrayCopy = new double[Size];
            Array.Copy(YArray, YArrayCopy, Size);
            var sorted = YArrayCopy.Select((x, i) => new KeyValuePair<double, int>(x, i)).OrderBy(x => x.Key).ToList();

            int z = Size - 1;
            while (z >= 0)
            {
                yield return sorted.Select(x => x.Value).ElementAt(z);
                z--;
            }
        }


        //TO DO: speed up this function. 
        public static IEnumerable<int> ExtractIndicesByY_old(double[] Y)
        {
            var YArrayIndex = Enumerable.Range(0, Y.Length).ToArray();
            var YArrayCopy = new double[Y.Length];
            Array.Copy(Y, YArrayCopy, Y.Length);
            Array.Sort(YArrayCopy, YArrayIndex);
            int z = Y.Length - 1;
            while (z >= 0)
            {
                yield return YArrayIndex[z];
                z--;
            }
        }

        public static IEnumerable<int> ExtractIndicesByY_new(double[] Y)
        {
            var YArrayCopy = new double[Y.Length];
            Array.Copy(Y, YArrayCopy, Y.Length);

            var sorted = YArrayCopy.Select((x, i) => new KeyValuePair<double, int>(x, i)).OrderBy(x => x.Key);

            int z = Y.Length - 1;
            while (z >= 0)
            {
                yield return sorted.Select(x => x.Value).ElementAt(z);
                z--;
            }
        }

        public int? GetClosestPeakIndex(double x)
        {
            if (Size == 0)
            {
                return null;
            }
            int index = Array.BinarySearch(XArray, x);
            if (index >= 0)
            {
                return index;
            }
            index = ~index;

            if (index >= Size)
            {
                return index - 1;
            }
            if (index == 0)
            {
                return index;
            }

            if (x - XArray[index - 1] > XArray[index] - x)
            {
                return index;
            }
            return index - 1;
        }

        private double ScoreIsotopeEnvelope(IsotopicEnvelope b)
        {
            if (b == null)
            {
                return 0;
            }
            return b.totalIntensity / Math.Pow(b.stDev, 0.13) * Math.Pow(b.peaks.Count, 0.4) / Math.Pow(b.charge, 0.06);
        }

        private bool Peak2satisfiesRatio(double peak1theorIntensity, double peak2theorIntensity, double peak1intensity, double peak2intensity, double intensityRatio)
        {
            var comparedShouldBe = peak1intensity / peak1theorIntensity * peak2theorIntensity;

            if (peak2intensity < comparedShouldBe / intensityRatio || peak2intensity > comparedShouldBe * intensityRatio)
            {
                return false;
            }
            return true;
        }

        //NeuCode
        private NeuCodeIsotopicEnvelop GetNeucodeEnvelopForThisEnvelop(NeuCodeIsotopicEnvelop BestIsotopicEnvelop, DeconvolutionParameter deconvolutionParameter)
        {
            NeuCodeIsotopicEnvelop neuCodeIsotopicEnvelop = null;

            var MostIntensePeakMz = BestIsotopicEnvelop.peaks.First().mz;

            List<int> range = new List<int>();


            for (int i = -deconvolutionParameter.MaxmiumNeuCodeNumber; i <= deconvolutionParameter.MaxmiumNeuCodeNumber; i++)
            {
                if (i!=0)
                {
                    range.Add(i);
                }              
            }

            foreach (var i in range)
            {
                var NeuCodeMostIntesePeakMz = MostIntensePeakMz + deconvolutionParameter.NeuCodeMassDefect * i / BestIsotopicEnvelop.charge / 1000;

                var closestPeakIndex = GetClosestPeakIndex(NeuCodeMostIntesePeakMz);
                var closestPeakmz = XArray[closestPeakIndex.Value];
                var closestPeakIntensity = YArray[closestPeakIndex.Value];
                if (closestPeakmz != MostIntensePeakMz && _neuCodePpmTolerance.Accepts(closestPeakmz.ToMass(BestIsotopicEnvelop.charge), NeuCodeMostIntesePeakMz.ToMass(BestIsotopicEnvelop.charge)) >= 0)
                {
                    var test = GetEnvelopForPeakAtChargeState(closestPeakmz, closestPeakIntensity, BestIsotopicEnvelop.massIndex, BestIsotopicEnvelop.charge, deconvolutionParameter);

                    if (ScoreIsotopeEnvelope(test) > ScoreIsotopeEnvelope(neuCodeIsotopicEnvelop))
                    {
                        neuCodeIsotopicEnvelop = test;
                    }
                }
            }

            if (neuCodeIsotopicEnvelop != null)
            {
                var pairRatio = BestIsotopicEnvelop.totalIntensity / neuCodeIsotopicEnvelop.totalIntensity;

                var rangeRatio = NeuCodeIsotopicEnvelop.EnvolopeToRangeRatio(XArray, YArray, BestIsotopicEnvelop.massIndex, BestIsotopicEnvelop.SelectedMz, BestIsotopicEnvelop.totalIntensity + neuCodeIsotopicEnvelop.totalIntensity);

                if (0.75 <= pairRatio && pairRatio <= 1.25 && rangeRatio > 0.5)
                {
                    BestIsotopicEnvelop.IsNeuCode = true;
                }
                else if (BestIsotopicEnvelop.peaks.Count >= 3 && neuCodeIsotopicEnvelop.peaks.Count >= 3 && rangeRatio > 0.5 && (pairRatio > 0.2 && pairRatio < 5))
                {
                    BestIsotopicEnvelop.IsNeuCode = true;
                }
                else if (BestIsotopicEnvelop.peaks.Count >= 4 && neuCodeIsotopicEnvelop.peaks.Count >= 4 && BestIsotopicEnvelop.peaks.Count == neuCodeIsotopicEnvelop.peaks.Count)
                {
                    BestIsotopicEnvelop.IsNeuCode = true;
                }
            }

            return neuCodeIsotopicEnvelop;
        }

        //isoEnvelops should be already ordered by mono mass
        //TO DO: need to be improved
        private void FindLabelPair(List<IsoEnvelop> isoEnvelops, DeconvolutionParameter deconvolutionParameter)
        {
            double[] monoMasses = isoEnvelops.Select(p=>p.MonoisotopicMass).ToArray();

            List<int> range = new List<int>();

            for (int i = -deconvolutionParameter.MaxmiumNeuCodeNumber; i <= deconvolutionParameter.MaxmiumNeuCodeNumber; i++)
            {
                if (i != 0)
                {
                    range.Add(i);
                }
            }

            foreach (var iso in isoEnvelops)
            {
                if (iso.IsNeuCode)
                {
                    continue;
                }

                foreach (var i in range)
                {
                    var possiblePairMass = iso.MonoisotopicMass + deconvolutionParameter.NeuCodeMassDefect * i / 1000;

                    var closestIsoIndex = GetClosestIsoIndex(possiblePairMass, monoMasses);

                    if (_neuCodePpmTolerance.Accepts(monoMasses[closestIsoIndex.Value], possiblePairMass) >= 0)
                    {
                        var ratio = iso.TotalIntensity / isoEnvelops.ElementAt(closestIsoIndex.Value).TotalIntensity;
                        if (0.5 <= ratio && ratio <= 2)
                        {
                            iso.IsNeuCode = true;
                            isoEnvelops.ElementAt(closestIsoIndex.Value).IsNeuCode = true;
                        }
                    }
                }
            }
        }

        public static int? GetClosestIsoIndex(double x, double[] masses)
        {
            if (masses.Length == 0)
            {
                return null;
            }
            int index = Array.BinarySearch(masses, x);
            if (index >= 0)
            {
                return index;
            }
            index = ~index;

            if (index >= masses.Length)
            {
                return index - 1;
            }
            if (index == 0)
            {
                return index;
            }

            if (x - masses[index - 1] > masses[index] - x)
            {
                return index;
            }
            return index - 1;
        }

        //ChargeState
        public List<ChargeDeconEnvelope> ChargeDeconvolution(List<NeuCodeIsotopicEnvelop> isotopicEnvelopes)
        {
            List<ChargeDeconEnvelope> chargeDeconEnvelopes = new List<ChargeDeconEnvelope>();
            SingleAbsoluteAroundZeroSearchMode massAccept = new SingleAbsoluteAroundZeroSearchMode(2.2);
            SinglePpmAroundZeroSearchMode massAcceptForNotch = new SinglePpmAroundZeroSearchMode(10);
            int i = 0;
            bool conditioner = true;
            while (conditioner)
            {
                if (i < isotopicEnvelopes.Count)
                {
                    var chargeDecon = new List<IsotopicEnvelope>();
                    chargeDecon.Add(isotopicEnvelopes[i]);

                    //The j here need to be break in a better way
                    for (int j = 1; j < 20; j++)
                    {
                        //Decide envelopes are from same mass or not, need better algorithm
                        //if (i + j < isotopicEnvelopes.Count
                        //    && massAccept.Accepts(isotopicEnvelopes[j + i].monoisotopicMass, isotopicEnvelopes[i].monoisotopicMass) == 0
                        //    && !chargeDecon.Exists(p => p.charge == isotopicEnvelopes[j + i].charge))
                        if (i + j < isotopicEnvelopes.Count && NotchTolerance(isotopicEnvelopes[i].monoisotopicMass, isotopicEnvelopes[j + i].monoisotopicMass, massAcceptForNotch))
                        {
                            if (!chargeDecon.Exists(p => p.charge == isotopicEnvelopes[j + i].charge))
                            {
                                chargeDecon.Add(isotopicEnvelopes[i + j]);
                            }
                        }
                        else
                        {
                            i = i + j;
                            break;
                        }
                    }
                    //Decide the charge deconvolution distribution, need better algorithm
                    if (chargeDecon.Count >= 3)
                    {
                        chargeDeconEnvelopes.Add(new ChargeDeconEnvelope(chargeDecon));
                    }
                }
                else
                {
                    conditioner = false;
                }
            }
            return chargeDeconEnvelopes;
        }

        private bool NotchTolerance(double theMass1, double theMass2, SinglePpmAroundZeroSearchMode massAccept)
        {
            if (massAccept.Accepts(theMass1, theMass2) == 0 || massAccept.Accepts(theMass1 + 1, theMass2) == 0
                || massAccept.Accepts(theMass1 + 2, theMass2) == 0 || massAccept.Accepts(theMass1 + 3, theMass2) == 0
                || massAccept.Accepts(theMass1 + 4, theMass2) == 0)
            {
                return true;
            }
            return false;
        }

        private void BestMonoisotopicMassForThisEnvelop(NeuCodeIsotopicEnvelop BestIsotopicEnvelop, DeconvolutionParameter deconvolutionParameter)
        {
            //if this envelop have multiple peaks, which means that it is highly likely to have a confused most intense peak.
            if (BestIsotopicEnvelop.peaks.Count >= 3)
            {
                for (int i = 1; i < 3; i++)
                {
                    var test = GetEnvelopForPeakAtChargeState(BestIsotopicEnvelop.peaks[i].mz, BestIsotopicEnvelop.peaks[i].intensity, BestIsotopicEnvelop.massIndex, BestIsotopicEnvelop.charge, deconvolutionParameter);
                    if (ScoreIsotopeEnvelope(test) > ScoreIsotopeEnvelope(BestIsotopicEnvelop))
                    {
                        BestIsotopicEnvelop = test;
                    }

                }
            }

        }

    }
}