using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MassSpectrometry;
using System.Collections.Concurrent;
using EngineLayer;
using System.IO;
using Chemistry;
using FlashLFQ;

namespace MetaDrawGUI
{
    public class MsDataFileDecon
    {

        #region Quantification based on selected precursor

        public void DeconQuantFile(List<MsDataScan> ms1DataScanList, string filePath, CommonParameters commonParameters, DeconvolutionParameter deconvolutionParameter)
        {
            SpectraFileInfo mzml = new SpectraFileInfo(filePath, "", 0, 0, 0);

            List<Identification>[] idts = new List<Identification>[ms1DataScanList.Count];
            List<IsoEnvelop>[] allIsotopicEnvelops = new List<IsoEnvelop>[ms1DataScanList.Count];

            List<double>[] aggragateIntensities = new List<double>[ms1DataScanList.Count];

            //for (int scanIndex = 0; scanIndex < ms1DataScanList.Count; scanIndex++)
            //{
            //    MzSpectrumBU mzSpectrumBU = new MzSpectrumBU(ms1DataScanList[scanIndex].MassSpectrum.XArray, ms1DataScanList[scanIndex].MassSpectrum.YArray, true);
            //    var isotopicEnvelopes = mzSpectrumBU.MsDeconv_Deconvolute(ms1DataScanList[scanIndex].ScanWindowRange, deconvolutionParameter).OrderBy(p => p.MonoisotopicMass).ToList();

            //    List<Identification> ids = new List<Identification>();
            //    int i = 0;
            //    foreach (var enve in isotopicEnvelopes)
            //    {
            //        isotopicEnvelopes[i].ScanNum = ms1DataScanList[scanIndex].OneBasedScanNumber;
            //        isotopicEnvelopes[i].RT = ms1DataScanList[scanIndex].RetentionTime;
            //        var id = GenerateIdentification(mzml, enve, ms1DataScanList[scanIndex].RetentionTime, scanIndex, i);
            //        i++;
            //        ids.Add(id);
            //    }
            //    if (ids.Count > 0)
            //    {
            //        idts[scanIndex] = ids;
            //        allIsotopicEnvelops[scanIndex] = isotopicEnvelopes;
            //    }
            //}


            Parallel.ForEach(Partitioner.Create(0, ms1DataScanList.Count), new ParallelOptions { MaxDegreeOfParallelism = commonParameters.MaxThreadsToUsePerFile }, (range, loopState) =>
            {
                for (int scanIndex = range.Item1; scanIndex < range.Item2; scanIndex++)
                {
                    MzSpectrumXY mzSpectrumBU = new MzSpectrumXY(ms1DataScanList[scanIndex].MassSpectrum.XArray, ms1DataScanList[scanIndex].MassSpectrum.YArray, true);
                    var isotopicEnvelopes = IsoDecon.MsDeconv_Deconvolute(mzSpectrumBU, ms1DataScanList[scanIndex].ScanWindowRange, deconvolutionParameter).OrderByDescending(p => p.TotalIntensity).ToList();

                    List<Identification> ids = new List<Identification>();
                    List<double> intensities = new List<double>();
                    double currIntensity = 0;
                    int i = 0;
                    foreach (var enve in isotopicEnvelopes)
                    {
                        isotopicEnvelopes[i].ScanNum = ms1DataScanList[scanIndex].OneBasedScanNumber;
                        isotopicEnvelopes[i].RT = ms1DataScanList[scanIndex].RetentionTime;
                        var id = GenerateIdentification(mzml, enve, ms1DataScanList[scanIndex].RetentionTime, scanIndex, i);
                        i++;
                        ids.Add(id);

                        currIntensity += enve.TotalIntensity / mzSpectrumBU.TotalIntensity;
                        intensities.Add(currIntensity);
                    }

                    idts[scanIndex] = ids;
                    allIsotopicEnvelops[scanIndex] = isotopicEnvelopes;

                    aggragateIntensities[scanIndex] = intensities;
                }

            });

            var idList = idts.SelectMany(p => p).ToList();
            FlashLfqEngine engine = new FlashLfqEngine(idList, integrate: false, ppmTolerance: 5, isotopeTolerancePpm: 3);
            var results = engine.Run();
            var peaks = results.Peaks.SelectMany(p => p.Value).ToList();
            WritePeakResults(Path.Combine(Path.GetDirectoryName(filePath), @"Peaks.tsv"), peaks);

            if (deconvolutionParameter.ToGetPartner)
            {
                var filteredPeaks = results.Peaks.First().Value.Where(p => p.Intensity > 0 && p.IsotopicEnvelopes.Count > 0).OrderBy(p => p.Identifications.First().MonoisotopicMass).ToList();
                List<NeucodeDoublet> neucodeDoublets = FindNeocodeDoublet(filteredPeaks, deconvolutionParameter);
                WriteResults(Path.Combine(Path.GetDirectoryName(filePath), @"NeucodesDoublets.tsv"), neucodeDoublets);
            }

            var envelops = allIsotopicEnvelops.SelectMany(p => p).ToList();
            WriteEnvelopResults(Path.Combine(Path.GetDirectoryName(filePath), @"Envelops.tsv"), envelops);

            var aggIntensities = aggragateIntensities.Where(p => p.Count >= 20).ToList();
            WriteAggIntensityResults(Path.Combine(Path.GetDirectoryName(filePath), @"AggIntensities.tsv"), aggIntensities);
        }

        public void DeconLabelQuantFile(List<MsDataScan> ms1DataScanList, string filePath, CommonParameters commonParameters, DeconvolutionParameter deconvolutionParameter)
        {
            SpectraFileInfo mzml = new SpectraFileInfo(filePath, "", 0, 0, 0);
            
            //List<Identification>[] idts = new List<Identification>[ms1DataScanList.Count];
            List<Identification>[] lightIdts = new List<Identification>[ms1DataScanList.Count];
            List<Identification>[] heavyIdts = new List<Identification>[ms1DataScanList.Count];
            List<Identification>[] unmatchedIdts = new List<Identification>[ms1DataScanList.Count];

            //List<IsoEnvelop>[] allIsotopicEnvelops = new List<IsoEnvelop>[ms1DataScanList.Count];
            //FlashLfqResults[] flashLfqResults = new FlashLfqResults[ms1DataScanList.Count];

            //for (int scanIndex = 0; scanIndex < ms1DataScanList.Count; scanIndex++)
            //{
            //    MzSpectrumBU mzSpectrumBU = new MzSpectrumBU(ms1DataScanList[scanIndex].MassSpectrum.XArray, ms1DataScanList[scanIndex].MassSpectrum.YArray, true);
            //    var isotopicEnvelopes = mzSpectrumBU.MsDeconv_Deconvolute(ms1DataScanList[scanIndex].ScanWindowRange, deconvolutionParameter).OrderBy(p => p.MonoisotopicMass).ToList();

            //    List<Identification> ids = new List<Identification>();
            //    int i = 0;
            //    foreach (var enve in isotopicEnvelopes)
            //    {
            //        isotopicEnvelopes[i].ScanNum = ms1DataScanList[scanIndex].OneBasedScanNumber;
            //        isotopicEnvelopes[i].RT = ms1DataScanList[scanIndex].RetentionTime;
            //        var id = GenerateIdentification(mzml, enve, ms1DataScanList[scanIndex].RetentionTime, scanIndex, i);
            //        i++;
            //        ids.Add(id);
            //    }
            //    if (ids.Count > 0)
            //    {
            //        //FlashLfqEngine engi = new FlashLfqEngine(ids, integrate: true, ppmTolerance: 7.5, isotopeTolerancePpm: 3);
            //        //flashLfqResults[scanIndex] = engi.Run();

            //        idts[scanIndex] = ids;
            //        //allIsotopicEnvelops[scanIndex] = isotopicEnvelopes;
            //    }
            //}


            Parallel.ForEach(Partitioner.Create(0, ms1DataScanList.Count), new ParallelOptions { MaxDegreeOfParallelism = commonParameters.MaxThreadsToUsePerFile }, (range, loopState) =>
            {
                for (int scanIndex = range.Item1; scanIndex < range.Item2; scanIndex++)
                {
                    MzSpectrumXY mzSpectrumBU = new MzSpectrumXY(ms1DataScanList[scanIndex].MassSpectrum.XArray, ms1DataScanList[scanIndex].MassSpectrum.YArray, true);
                    var isotopicEnvelopes = IsoDecon.MsDeconv_Deconvolute(mzSpectrumBU, ms1DataScanList[scanIndex].ScanWindowRange, deconvolutionParameter).OrderBy(p => p.MonoisotopicMass).ToList();

                    //List<Identification> ids = new List<Identification>();
                    List<Identification> LightIds = new List<Identification>();
                    List<Identification> HeavyIds = new List<Identification>();
                    List<Identification> UnmatchedIds = new List<Identification>();


                    //int i = 0;
                    //foreach (var enve in isotopicEnvelopes)
                    for (int i = 0; i < isotopicEnvelopes.Count; i++)
                    {
                        if (isotopicEnvelopes[i].HasPartner && !isotopicEnvelopes[i].IsLight)
                        {
                            continue;
                        }

                        isotopicEnvelopes[i].ScanNum = ms1DataScanList[scanIndex].OneBasedScanNumber;
                        isotopicEnvelopes[i].RT = ms1DataScanList[scanIndex].RetentionTime;
                        var id = GenerateIdentification(mzml, isotopicEnvelopes[i], ms1DataScanList[scanIndex].RetentionTime, scanIndex, i);

                        if (!isotopicEnvelopes[i].HasPartner)
                        {
                            UnmatchedIds.Add(id);
                        }

                        if (isotopicEnvelopes[i].HasPartner && isotopicEnvelopes[i].IsLight)
                        {
                            LightIds.Add(id);

                            var heavyEnvelop = isotopicEnvelopes[i].Partner;
                            heavyEnvelop.ScanNum = ms1DataScanList[scanIndex].OneBasedScanNumber;
                            heavyEnvelop.RT = ms1DataScanList[scanIndex].RetentionTime;
                            var heavyId = GenerateIdentification(mzml, heavyEnvelop, ms1DataScanList[scanIndex].RetentionTime, scanIndex, i);
                            HeavyIds.Add(heavyId);  
                        }

                        //ids.Add(id);
                        //i++;
                    }

                    //if (ids.Count > 0)
                    //{
                    //    FlashLfqEngine engi = new FlashLfqEngine(ids, integrate: true, ppmTolerance: 7.5, isotopeTolerancePpm: 3);
                    //    flashLfqResults[scanIndex] = engi.Run();
                    //}
                    lightIdts[scanIndex] = LightIds;
                    heavyIdts[scanIndex] = HeavyIds;
                    unmatchedIdts[scanIndex] = UnmatchedIds;
                    //idts[scanIndex] = ids;
                    //allIsotopicEnvelops[scanIndex] = isotopicEnvelopes;
                }

            });

            //var peaksPerScans = flashLfqResults.SelectMany(p => p.Peaks.First().Value).ToList();
            //WritePeakResults(Path.Combine(Path.GetDirectoryName(filePath), @"PeaksPerScans.tsv"), peaksPerScans);

            //var idList = idts.SelectMany(p => p).ToList();
            //FlashLfqEngine engine = new FlashLfqEngine(idList, integrate:true, ppmTolerance:5, isotopeTolerancePpm:3);
            //var results = engine.Run();
            //var peaks = results.Peaks.SelectMany(p => p.Value).ToList();
            //WritePeakResults(Path.Combine(Path.GetDirectoryName(filePath), @"Peaks.tsv"), peaks);

            var light_idList = lightIdts.SelectMany(p => p).ToList();
            FlashLfqEngine light_engine = new FlashLfqEngine(light_idList, integrate: true, ppmTolerance: 5, isotopeTolerancePpm: 3);
            var light_results = light_engine.Run();
            var light_peaks = light_results.Peaks.SelectMany(p => p.Value).ToList();
            WritePeakResults(Path.Combine(Path.GetDirectoryName(filePath), @"light_Peaks.tsv"), light_peaks);

            var heavy_idList = heavyIdts.SelectMany(p => p).ToList();
            FlashLfqEngine heavy_engine = new FlashLfqEngine(heavy_idList, integrate: true, ppmTolerance: 5, isotopeTolerancePpm: 3);
            var heavy_results = heavy_engine.Run();
            var heavy_peaks = heavy_results.Peaks.SelectMany(p => p.Value).ToList();
            WritePeakResults(Path.Combine(Path.GetDirectoryName(filePath), @"heavy_Peaks.tsv"), heavy_peaks);

            var unmatch_idList = unmatchedIdts.SelectMany(p => p).ToList();
            FlashLfqEngine unmatch_engine = new FlashLfqEngine(unmatch_idList, integrate: true, ppmTolerance: 5, isotopeTolerancePpm: 3);
            var unmatch_results = unmatch_engine.Run();
            var unmatch_peaks = unmatch_results.Peaks.SelectMany(p => p.Value).ToList();
            WritePeakResults(Path.Combine(Path.GetDirectoryName(filePath), @"unmatch_Peaks.tsv"), unmatch_peaks);


            //if (deconvolutionParameter.ToGetPartner)
            //{
            //    //var unmatchNeuCodePeaks = new List<FlashLFQ.ChromatographicPeak>();
            //    var filteredPeaks = results.Peaks.First().Value.Where(p => p.Intensity > 0 && p.IsotopicEnvelopes.Count > 0).OrderBy(p => p.Identifications.First().MonoisotopicMass).ToList();
            //    List<NeucodeDoublet> neucodeDoublets = FindNeocodeDoublet(filteredPeaks, deconvolutionParameter);
            //    WriteResults(Path.Combine(Path.GetDirectoryName(filePath), @"NeucodesDoublets.tsv"), neucodeDoublets);
            //}

            //var envelops = allIsotopicEnvelops.SelectMany(p => p).ToList();
            //WriteEnvelopResults(Path.Combine(Path.GetDirectoryName(filePath), @"Envelops.tsv"), envelops);
        }

        private ChemicalFormula GenerateChemicalFormula(double monoIsotopicMass)
        {
            double massOfAveragine = 111.1254;
            double numberOfAveragines = monoIsotopicMass / massOfAveragine;

            double averageC = 4.9384 * numberOfAveragines;
            double averageH = 7.7583 * numberOfAveragines;
            double averageO = 1.4773 * numberOfAveragines;
            double averageN = 1.3577 * numberOfAveragines;
            double averageS = 0.0417 * numberOfAveragines;

            ChemicalFormula myFormula = ChemicalFormula.ParseFormula(
                "C" + (int)Math.Round(averageC) +
                "H" + (int)Math.Round(averageH) +
                "O" + (int)Math.Round(averageO) +
                "N" + (int)Math.Round(averageN) +
                "S" + (int)Math.Round(averageS));

            return myFormula;
        }

        private Identification GenerateIdentification(SpectraFileInfo mzml, IsoEnvelop Enve, double RT, int scanIndex, int i)
        {
            var myFormula = GenerateChemicalFormula(Enve.MonoisotopicMass);
            var pg = new FlashLFQ.ProteinGroup("", "", "");

            //string baseSeq = "";
            string baseSeq = changeInt2Seq(scanIndex, i);
            string modifiedSeq = Enve.MonoisotopicMass.ToString("F4") + "-" + RT.ToString("F2");     

            Identification id = new Identification(mzml, baseSeq, modifiedSeq, Enve.MonoisotopicMass, RT, Enve.Charge, new List<FlashLFQ.ProteinGroup> { pg }, myFormula);
            return id;
        }

        private string changeInt2Seq(int a, int b)
        {
            string seq = "";
            
            Dictionary<char, char> num2aa = new Dictionary<char, char>();
            num2aa.Add('0','Q');
            num2aa.Add('1', 'G');
            num2aa.Add('2', 'A');
            num2aa.Add('3', 'S');
            num2aa.Add('4', 'T');
            num2aa.Add('5', 'C');
            num2aa.Add('6', 'V');
            num2aa.Add('7', 'L');
            num2aa.Add('8', 'I');
            num2aa.Add('9', 'M');
            
            foreach (var ia in a.ToString().ToCharArray())
            {
                seq += num2aa[ia];
            }
            seq += 'F';
            foreach (var ib in b.ToString().ToCharArray())
            {
                seq += num2aa[ib];
            }
            return seq;
        }

        //chromatographicPeaks should be ordered.
        private List<NeucodeDoublet> FindNeocodeDoublet(List<FlashLFQ.ChromatographicPeak> chromatographicPeaks, DeconvolutionParameter deconvolutionParameter)
        {
            List<NeucodeDoublet> neucodeDoublets = new List<NeucodeDoublet>();

            for (int i = 0; i < chromatographicPeaks.Count-1; i++)
            {
                for (int j = i+1; j < chromatographicPeaks.Count; j++)
                {
                    if (chromatographicPeaks[j].Identifications.First().MonoisotopicMass - chromatographicPeaks[i].Identifications.First().MonoisotopicMass > deconvolutionParameter.PartnerMassDiff * (deconvolutionParameter.MaxmiumLabelNumber + 1))
                    {
                        break;
                    }
                    if (CheckNeuCode(chromatographicPeaks[i], chromatographicPeaks[j], deconvolutionParameter))
                    {
                        neucodeDoublets.Add(new NeucodeDoublet(chromatographicPeaks[i], chromatographicPeaks[j]));
                        break;
                    }
                }
            }

            return neucodeDoublets;
        }

        private bool CheckNeuCode(FlashLFQ.ChromatographicPeak aPeak, FlashLFQ.ChromatographicPeak bPeak, DeconvolutionParameter deconvolutionParameter)
        {
            //if (aPeak.IsotopicEnvelopes.Count == 0 || bPeak.IsotopicEnvelopes.Count == 0 ||aPeak.Intensity == 0 || bPeak.Intensity == 0)
            //{
            //    return false;
            //}
            for (int i = 1; i < deconvolutionParameter.MaxmiumLabelNumber + 1; i++)
            {
                if (aPeak.IsotopicEnvelopes.Select(p => p.IndexedPeak.RetentionTime).Min() <= bPeak.IsotopicEnvelopes.Select(p => p.IndexedPeak.RetentionTime).Max()
                    && bPeak.IsotopicEnvelopes.Select(p => p.IndexedPeak.RetentionTime).Min() <= aPeak.IsotopicEnvelopes.Select(p => p.IndexedPeak.RetentionTime).Max())
                {
                    //Test if the 
                    if (deconvolutionParameter.DeconvolutionAcceptor.Within(aPeak.Identifications.First().MonoisotopicMass,
                        bPeak.Identifications.First().MonoisotopicMass - deconvolutionParameter.PartnerMassDiff * i) &&
                        aPeak.Apex.ChargeState == bPeak.Apex.ChargeState)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static List<NeucodeDoublet> FindNeocodeDoublet(List<MsFeature> msFeatures, DeconvolutionParameter deconvolutionParameter)
        {
            List<NeucodeDoublet> neucodeDoublets = new List<NeucodeDoublet>();

            for (int i = 0; i < msFeatures.Count - 1; i++)
            {
                for (int j = i + 1; j < msFeatures.Count; j++)
                {
                    if (msFeatures[j].MonoMass - msFeatures[i].MonoMass > deconvolutionParameter.PartnerMassDiff * (deconvolutionParameter.MaxmiumLabelNumber + 1))
                    {
                        break;
                    }
                    if (CheckNeuCode(msFeatures[i], msFeatures[j], deconvolutionParameter))
                    {
                        neucodeDoublets.Add(new NeucodeDoublet(msFeatures[i], msFeatures[j]));
                        break;
                    }
                }
            }

            return neucodeDoublets;
        }

        private static bool CheckNeuCode(MsFeature aPeak, MsFeature bPeak, DeconvolutionParameter deconvolutionParameter)
        {
            
            for (int i = 1; i < deconvolutionParameter.MaxmiumLabelNumber + 1; i++)
            {
                //For MaxQuant output, somehow, the retention time is weird.
                if (aPeak.MaxScanNum!=0 || bPeak.MaxScanNum!=0)
                {
                    if (aPeak.MinScanNum <= bPeak.MaxScanNum && bPeak.MinScanNum <= aPeak.MaxScanNum)
                    {
                        if (deconvolutionParameter.DeconvolutionAcceptor.Within(aPeak.MonoMass,
                            bPeak.MonoMass - deconvolutionParameter.PartnerMassDiff * i))
                        {
                            return true;
                        }
                    }
                }
                if (aPeak.StartRT <= bPeak.EndRT && bPeak.StartRT <= aPeak.EndRT)
                {
                    if (deconvolutionParameter.DeconvolutionAcceptor.Within(aPeak.MonoMass,
                        bPeak.MonoMass - deconvolutionParameter.PartnerMassDiff *i) &&
                        aPeak.Charge == bPeak.Charge)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public void WritePeakResults(string peaksOutputPath, List<FlashLFQ.ChromatographicPeak> chromatographicPeaks)
        {
            using (StreamWriter output = new StreamWriter(peaksOutputPath))
            {
                output.WriteLine(FlashLFQ.ChromatographicPeak.TabSeparatedHeader);

                foreach (var peak in chromatographicPeaks)
                {
                    output.WriteLine(peak.ToString());
                }
            }
        }

        public static void WriteResults(string peaksOutputPath, List<NeucodeDoublet> neucodeDoublets)
        {
            using (StreamWriter output = new StreamWriter(peaksOutputPath))
            {
                output.WriteLine(NeucodeDoublet.TabSeparatedHeader);

                foreach (var peak in neucodeDoublets)
                {
                    output.WriteLine(peak.ToString());
                }
            }
        }

        public void WriteEnvelopResults(string peaksOutputPath, List<IsoEnvelop> isotopicEnvelops)
        {
            using (StreamWriter output = new StreamWriter(peaksOutputPath))
            {
                output.WriteLine(IsoEnvelop.TabSeparatedHeader);

                foreach (var enve in isotopicEnvelops)
                {
                    output.WriteLine(enve.ToString());
                }
            }
        }

        public static void WriteAggIntensityResults(string peaksOutputPath, List<List<double>> aggIntensities)
        {
            using (StreamWriter output = new StreamWriter(peaksOutputPath))
            {
                foreach (var intensity in aggIntensities)
                {
                    output.WriteLine(string.Join("\t", intensity));
                }
            }
        }

        #endregion

    }
}
