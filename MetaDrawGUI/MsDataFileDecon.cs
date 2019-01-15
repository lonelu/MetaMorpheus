using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MassSpectrometry;
using System.Collections.Concurrent;
using EngineLayer;
using System.IO;

namespace MetaDrawGUI
{
    public class MsDataFileDecon
    {
        //public List<ChargeDeconEnvelope> chargeEnvelopesList { get; set; } = new List<ChargeDeconEnvelope>();

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
    }
}
