﻿using System;
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
        public List<ChargeDeconPerMS1Scan> ChargeDeconvolutionFile(List<MsDataScan> msDataScanList, CommonParameters commonParameters, DeconvolutionParameter deconvolutionParameter)
        {
            var chargeDeconPerMS1Scans = new List<ChargeDeconPerMS1Scan>();
            Parallel.ForEach(Partitioner.Create(0, msDataScanList.Count), new ParallelOptions { MaxDegreeOfParallelism = commonParameters.MaxThreadsToUsePerFile }, range =>
            {
                for (int i = range.Item1; i < range.Item2; i++)
                {
                    var msDataScan = msDataScanList[i];
                    MzSpectrumBU mzSpectrumTD = new MzSpectrumBU(msDataScan.MassSpectrum.XArray, msDataScan.MassSpectrum.YArray, true);
                    if (msDataScan.MsnOrder == 1)
                    {
                        var isotopicEnvelopes = mzSpectrumTD.DeconvoluteBU(msDataScan.ScanWindowRange, deconvolutionParameter).OrderBy(p => p.monoisotopicMass).ToList();

                        var selectedMS2 = msDataScanList.Where(p => p.OneBasedPrecursorScanNumber == msDataScan.OneBasedScanNumber).Select(p => p.SelectedIonMZ).ToList();

                        var chargeDecon = mzSpectrumTD.ChargeDeconvolution(msDataScan.OneBasedScanNumber, msDataScan.RetentionTime, isotopicEnvelopes, selectedMS2);

                        lock (chargeDeconPerMS1Scans)
                        {
                            if (chargeDecon.Count != 0)
                            {
                                chargeDeconPerMS1Scans.Add(new ChargeDeconPerMS1Scan(chargeDecon));
                            }
                        }

                        //lock (chargeEnvelopesList)
                        //{
                        //    foreach (var item in chargeDecon)
                        //    {
                        //        chargeEnvelopesList.Add(item);
                        //    }
                        //}
                    }
                }
            });
            //chargeEnvelopesList = chargeEnvelopesList.OrderBy(p => p.OneBasedScanNumber).ToList();
            chargeDeconPerMS1Scans = chargeDeconPerMS1Scans.OrderBy(p => p.OneBasedScanNumber).ToList();
            return chargeDeconPerMS1Scans;
        }

        public List<ChargeParsi> ChargeParsimony(List<ChargeDeconPerMS1Scan> chargeDeconPerMS1ScanList, SingleAbsoluteAroundZeroSearchMode massAccept, SingleAbsoluteAroundZeroSearchMode rtAccept)
        {
            List<ChargeParsi> chargeParsis = new List<ChargeParsi>();
            for (int i = 0; i < chargeDeconPerMS1ScanList.Count; i++)
            {
                if (chargeDeconPerMS1ScanList[i].ChargeDeconEnvelopes.Count != 0)
                {
                    foreach (var chargeDeconEnvelope in chargeDeconPerMS1ScanList[i].ChargeDeconEnvelopes)
                    {
                        if (chargeDeconEnvelope != null)
                        {
                            ChargeParsi chargeParsi = new ChargeParsi();
                            chargeParsi.chargeDeconEnvelopes.Add(chargeDeconEnvelope);

                            bool decision = true;
                            //Base on disappear in 3 scans.
                            int limit = 1;
                            int j = 1;
                            while (decision)
                            {
                                if (limit <= 3 && i + j < chargeDeconPerMS1ScanList.Count)
                                {
                                    if (chargeDeconPerMS1ScanList[i + j].ChargeDeconEnvelopes.Count != 0)
                                    {
                                        for (int z = 0; z < chargeDeconPerMS1ScanList[i + j].ChargeDeconEnvelopes.Count; z++)
                                        {
                                            if (chargeDeconPerMS1ScanList[i + j].ChargeDeconEnvelopes[z] != null && massAccept.Accepts(chargeDeconPerMS1ScanList[i + j].ChargeDeconEnvelopes[z].isotopicMass, chargeDeconEnvelope.isotopicMass) >= 0)
                                            {
                                                chargeParsi.chargeDeconEnvelopes.Add(chargeDeconPerMS1ScanList[i + j].ChargeDeconEnvelopes[z]);
                                                chargeDeconPerMS1ScanList[i + j].ChargeDeconEnvelopes[z] = null;
                                                limit--;
                                                break;
                                            }
                                        }
                                        limit++;
                                    }
                                    j++;
                                }
                                else { decision = false; }
                            }

                            chargeParsis.Add(chargeParsi);
                        }
                    }
                }
            }
            return chargeParsis;
        }

        public void ChargeDeconWriteToTSV(List<ChargeDeconPerMS1Scan> chargeDeconPerMS1ScanList, string outputFolder, string fileName)
        {
            var writtenFile = Path.Combine(outputFolder, fileName + ".mytsv");
            using (StreamWriter output = new StreamWriter(writtenFile))
            {
                output.WriteLine("Isotopic Mass\tNumber of IsotopicEnvelops");
                foreach (var chargeDeconPerMS1Scan in chargeDeconPerMS1ScanList)
                {
                    if (chargeDeconPerMS1Scan.ChargeDeconEnvelopes.Count != 0)
                    {
                        output.WriteLine("Scan #" + chargeDeconPerMS1Scan.OneBasedScanNumber);
                        foreach (var ChargeDeconEnvelope in chargeDeconPerMS1Scan.ChargeDeconEnvelopes)
                        {
                            string Ms2ToString = "";
                            foreach (var Ms2 in ChargeDeconEnvelope.SelectedMs2s)
                            {
                                Ms2ToString += Ms2 + "-";
                            }
                            output.WriteLine(ChargeDeconEnvelope.isotopicMass.ToString("F1") + "\t" + ChargeDeconEnvelope.numOfEnvelopes + "\t" + Ms2ToString);
                        }

                    }
                }
            }
        }

        public void DeconQuantFile(List<MsDataScan> ms1DataScanList, string filePath, CommonParameters commonParameters, DeconvolutionParameter deconvolutionParameter)
        {
            SpectraFileInfo mzml = new SpectraFileInfo(filePath, "", 0, 0, 0);
            
            List<Identification>[] idts = new List<Identification>[ms1DataScanList.Count];

            //for (int i = 0; i < ms1DataScanList.Count; i++)
            Parallel.ForEach(Partitioner.Create(0, ms1DataScanList.Count), new ParallelOptions { MaxDegreeOfParallelism = commonParameters.MaxThreadsToUsePerFile }, (range, loopState) =>
             {
                 for (int scanIndex = range.Item1; scanIndex < range.Item2; scanIndex++)
                 {
                     MzSpectrumBU mzSpectrumBU = new MzSpectrumBU(ms1DataScanList[scanIndex].MassSpectrum.XArray, ms1DataScanList[scanIndex].MassSpectrum.YArray, true);
                     var isotopicEnvelopes = mzSpectrumBU.DeconvoluteBU(ms1DataScanList[scanIndex].ScanWindowRange, deconvolutionParameter).OrderBy(p => p.monoisotopicMass).ToList();
                     List<Identification> ids = new List<Identification>();
                     int i = 0;
                     foreach (var enve in isotopicEnvelopes)
                     {                       

                         var id = GenerateIdentification(mzml, enve, ms1DataScanList[scanIndex].RetentionTime, scanIndex, i);
                         i++;
                         ids.Add(id);
                     }

                     idts[scanIndex] = ids;
                 }

             });

            var idList = idts.SelectMany(p => p).ToList();
            FlashLfqEngine engine = new FlashLfqEngine(idList, normalize: true);

            var results = engine.Run();

            results.WriteResults(Path.Combine(Path.GetDirectoryName(filePath), @"1.tsv"), Path.Combine(Path.GetDirectoryName(filePath), @"2.tsv"), null);

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

        private Identification GenerateIdentification(SpectraFileInfo mzml, NeuCodeIsotopicEnvelop Enve, double RT, int scanIndex, int i)
        {
            var myFormula = GenerateChemicalFormula(Enve.monoisotopicMass);
            var pg = new FlashLFQ.ProteinGroup("", "", "");

            //string baseSeq = "";
            string baseSeq = changeInt2Seq(scanIndex, i);
            string modifiedSeq = Enve.monoisotopicMass.ToString("F4") + "-" + RT.ToString("F2");     

            Identification id = new Identification(mzml, baseSeq, modifiedSeq, Enve.monoisotopicMass, RT, Enve.charge, new List<FlashLFQ.ProteinGroup> { pg }, myFormula);
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

    }
}
