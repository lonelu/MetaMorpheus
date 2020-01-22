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
            //        FlashLfqEngine engi = new FlashLfqEngine(ids, integrate: true, ppmTolerance: 7.5, isotopeTolerancePpm: 3);
            //        flashLfqResults[scanIndex] = engi.Run();

            //        idts[scanIndex] = ids;
            //        allIsotopicEnvelops[scanIndex] = isotopicEnvelopes;
            //    }
            //}


            Parallel.ForEach(Partitioner.Create(0, ms1DataScanList.Count), new ParallelOptions { MaxDegreeOfParallelism = commonParameters.MaxThreadsToUsePerFile }, (range, loopState) =>
            {
                for (int scanIndex = range.Item1; scanIndex < range.Item2; scanIndex++)
                {
                    MzSpectrumBU mzSpectrumBU = new MzSpectrumBU(ms1DataScanList[scanIndex].MassSpectrum.XArray, ms1DataScanList[scanIndex].MassSpectrum.YArray, true);
                    var isotopicEnvelopes = mzSpectrumBU.MsDeconv_Deconvolute(ms1DataScanList[scanIndex].ScanWindowRange, deconvolutionParameter).OrderBy(p => p.MonoisotopicMass).ToList();

                    List<Identification> ids = new List<Identification>();
                    int i = 0;
                    foreach (var enve in isotopicEnvelopes)
                    {
                        isotopicEnvelopes[i].ScanNum = ms1DataScanList[scanIndex].OneBasedScanNumber;
                        isotopicEnvelopes[i].RT = ms1DataScanList[scanIndex].RetentionTime;
                        var id = GenerateIdentification(mzml, enve, ms1DataScanList[scanIndex].RetentionTime, scanIndex, i);
                        i++;
                        ids.Add(id);
                    }
                    //if (ids.Count > 0)
                    //{
                    //    FlashLfqEngine engi = new FlashLfqEngine(ids, integrate: true, ppmTolerance: 7.5, isotopeTolerancePpm: 3);
                    //    flashLfqResults[scanIndex] = engi.Run();
                    //}
                    idts[scanIndex] = ids;
                    allIsotopicEnvelops[scanIndex] = isotopicEnvelopes;
                }

            });

            //var peaksPerScans = flashLfqResults.SelectMany(p => p.Peaks.First().Value).ToList();
            //WritePeakResults(Path.Combine(Path.GetDirectoryName(filePath), @"PeaksPerScans.tsv"), peaksPerScans);

            var idList = idts.SelectMany(p => p).ToList();
            FlashLfqEngine engine = new FlashLfqEngine(idList, integrate:true, ppmTolerance:5, isotopeTolerancePpm:3);
            var results = engine.Run();
            var peaks = results.Peaks.SelectMany(p => p.Value).ToList();
            WritePeakResults(Path.Combine(Path.GetDirectoryName(filePath), @"Peaks.tsv"), peaks);

            //var unmatchNeuCodePeaks = new List<FlashLFQ.ChromatographicPeak>();
            List<NeucodeDoublet> neucodeDoublets = CheckNeocodeDoublet(results.Peaks.First().Value.OrderBy(p => p.Identifications.First().MonoisotopicMass).ToList(), deconvolutionParameter);
            WriteResults(Path.Combine(Path.GetDirectoryName(filePath), @"NeucodesDoublets.tsv"), neucodeDoublets);

            var envelops = allIsotopicEnvelops.SelectMany(p => p).ToList();
            WriteEnvelopResults(Path.Combine(Path.GetDirectoryName(filePath), @"Envelops.tsv"), envelops);
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
        private List<NeucodeDoublet> CheckNeocodeDoublet(List<FlashLFQ.ChromatographicPeak> chromatographicPeaks, DeconvolutionParameter deconvolutionParameter)
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
            if (aPeak.IsotopicEnvelopes.Count == 0 || bPeak.IsotopicEnvelopes.Count == 0)
            {
                return false;
            }
            for (int i = 1; i < deconvolutionParameter.MaxmiumLabelNumber + 1; i++)
            {
                if (aPeak.IsotopicEnvelopes.Select(p => p.IndexedPeak.RetentionTime).Min() <= bPeak.IsotopicEnvelopes.Select(p => p.IndexedPeak.RetentionTime).Max()
                    && bPeak.IsotopicEnvelopes.Select(p => p.IndexedPeak.RetentionTime).Min() <= aPeak.IsotopicEnvelopes.Select(p => p.IndexedPeak.RetentionTime).Max())
                {
                    if (deconvolutionParameter.DeconvolutionAcceptor.Within(aPeak.Identifications.First().MonoisotopicMass,
                        bPeak.Identifications.First().MonoisotopicMass - deconvolutionParameter.PartnerMassDiff * i / 1000))
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

        public void WriteResults(string peaksOutputPath, List<NeucodeDoublet> neucodeDoublets)
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

        #endregion

    }
}
