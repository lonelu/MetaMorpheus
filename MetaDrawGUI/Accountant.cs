using System;
using System.Collections.Generic;
using BoxCar;
using System.IO;
using MassSpectrometry;
using EngineLayer;
using TaskLayer;
using System.Linq;

namespace MetaDrawGUI
{
    public class Accountant
    {
        //For BoxCar study
        public void ExtractScanNumTime(List<string> MsDataFilePaths, MyFileManager spectraFileManager, Tuple<double, double> timeRange)
        {
            //fullNum, msxNum, MS2Num
            List<Tuple<int, int, int>> tuples = new List<Tuple<int, int, int>>();

            foreach (var filePath in MsDataFilePaths)
            {
                var msDataFile = spectraFileManager.LoadFile(filePath, new CommonParameters());
                var scanSets = BoxCarFunctions.GenerateScanSet(msDataFile);


                var fullNum = scanSets.Where(p => p.Ms1scans.First().RetentionTime > timeRange.Item1 && p.Ms1scans.First().RetentionTime < timeRange.Item2).Sum(p => p.Ms1scans.Count);
                var msxNum = scanSets.Where(p => p.Ms1scans.First().RetentionTime > timeRange.Item1 && p.Ms1scans.First().RetentionTime < timeRange.Item2).Sum(p => p.BoxcarScans.Count);
                var MS2Num = scanSets.Where(p => p.Ms1scans.First().RetentionTime > timeRange.Item1 && p.Ms1scans.First().RetentionTime < timeRange.Item2).Sum(p => p.Ms2scans.Count);

                tuples.Add(new Tuple<int, int, int>(fullNum, msxNum, MS2Num));

                var scans = msDataFile.GetAllScansList();

                var times = ExtractTime(scans);

                WriteExtractedTime(filePath, Path.GetFileNameWithoutExtension(filePath) + "_time", times);

            }

            WriteExtractedNum(MsDataFilePaths.First(), "scan_count", tuples);
        }

        private List<Tuple<double, double, double, double, string, string, string>> ExtractTime(List<MsDataScan> scans)
        {
            List<Tuple<double, double, double, double, string, string, string>> times = new List<Tuple<double, double, double, double, string, string, string>>();


            for (int i = 1; i < scans.Count - 1; i++)
            {
                string scanType = "";
                string previousScanType = "";
                string nextScanType = "";

                if (scans[i].ScanFilter.Contains("Full ms "))
                {
                    scanType = "Full";
                }
                else if (scans[i].ScanFilter.Contains("msx ms"))
                {
                    scanType = "Msx";

                }
                else if (scans[i].ScanFilter.Contains("ms2"))
                {
                    scanType = "Ms2";
                }


                if (scans[i - 1].ScanFilter.Contains("Full ms "))
                {
                    previousScanType = "Full";
                }
                else if (scans[i - 1].ScanFilter.Contains("msx ms"))
                {
                    previousScanType = "Msx";

                }
                else if (scans[i - 1].ScanFilter.Contains("ms2"))
                {
                    previousScanType = "Ms2";
                }


                if (scans[i + 1].ScanFilter.Contains("Full ms "))
                {
                    nextScanType = "Full";
                }
                else if (scans[i + 1].ScanFilter.Contains("msx ms"))
                {
                    nextScanType = "Msx";

                }
                else if (scans[i + 1].ScanFilter.Contains("ms2"))
                {
                    nextScanType = "Ms2";
                }

                double previousTime = (scans[i].RetentionTime - scans[i - 1].RetentionTime) * 60000;

                double nextTime = (scans[i + 1].RetentionTime - scans[i].RetentionTime) * 60000;


                times.Add(new Tuple<double, double, double, double, string, string, string>(scans[i].RetentionTime, scans[i].InjectionTime.Value, previousTime, nextTime, scanType, previousScanType, nextScanType));

            }

            return times;
        }

        private void WriteExtractedTime(string FilePath, string name, List<Tuple<double, double, double, double, string, string, string>> times)
        {
            var writtenFile = Path.Combine(Path.GetDirectoryName(FilePath), name + ".tsv");
            using (StreamWriter output = new StreamWriter(writtenFile))
            {
                output.WriteLine("RetentionTime\tInjectTime\tPreviousTime\tNextTime\tScanType\tPreviousScanType\tNextScanType");
                for (int i = 0; i < times.Count; i++)
                {
                    output.WriteLine(times[i].Item1 + "\t" + times[i].Item2 + "\t" + times[i].Item3 + "\t" + times[i].Item4 + "\t" + times[i].Item5 + "\t" + times[i].Item6 + "\t" + times[i].Item7);
                }
            }
        }

        private void WriteExtractedNum(string FilePath, string name, List<Tuple<int, int, int>> tuples)
        {
            var writtenFile = Path.Combine(Path.GetDirectoryName(FilePath), name + ".tsv");
            using (StreamWriter output = new StreamWriter(writtenFile))
            {
                output.WriteLine("FullTotal\tMsxTotal\tMS2Total");
                for (int i = 0; i < tuples.Count; i++)
                {
                    output.WriteLine(tuples[i].Item1 + "\t" + tuples[i].Item2 + "\t" + tuples[i].Item3);
                }
            }
        }

        //For Glyco study, especially HCD triggered methods
        public void ExtractScanInfo_Glyco(List<string> MsDataFilePaths, MyFileManager spectraFileManager, Tuple<double, double> timeRange)
        {
            //fullNum, MS2Num, HCDNum, ETDNum
            List<Tuple<int, int, int, int, int>> tuples = new List<Tuple<int, int, int, int, int>>();

            foreach (var filePath in MsDataFilePaths)
            {
                var msDataFile = spectraFileManager.LoadFile(filePath, new CommonParameters());
                var scanSets = BoxCarFunctions.GenerateScanSet(msDataFile);


                var fullNum = scanSets.Where(p => p.Ms1scans.First().RetentionTime > timeRange.Item1 && p.Ms1scans.First().RetentionTime < timeRange.Item2).Sum(p => p.Ms1scans.Count);
                //var msxNum = scanSets.Where(p => p.Ms1scans.First().RetentionTime > timeRange.Item1 && p.Ms1scans.First().RetentionTime < timeRange.Item2).Sum(p => p.BoxcarScans.Count);
                var MS2_Num = scanSets.Where(p => p.Ms1scans.First().RetentionTime > timeRange.Item1 && p.Ms1scans.First().RetentionTime < timeRange.Item2).Sum(p => p.Ms2scans.Count);
                var HCD_Num = scanSets.Where(p => p.Ms1scans.First().RetentionTime > timeRange.Item1 && p.Ms1scans.First().RetentionTime < timeRange.Item2).Sum(p => p.Ms2scans.Where(k => k.ScanFilter.Contains("hcd") && !k.ScanFilter.Contains("etd")).Count());
                var ETD_Num = scanSets.Where(p => p.Ms1scans.First().RetentionTime > timeRange.Item1 && p.Ms1scans.First().RetentionTime < timeRange.Item2).Sum(p => p.Ms2scans.Where(k => k.ScanFilter.Contains("etd") && !k.ScanFilter.Contains("hcd")).Count());
                var EThcD_Num = scanSets.Where(p => p.Ms1scans.First().RetentionTime > timeRange.Item1 && p.Ms1scans.First().RetentionTime < timeRange.Item2).Sum(p => p.Ms2scans.Where(k => k.ScanFilter.Contains("etd") && k.ScanFilter.Contains("hcd")).Count());


                tuples.Add(new Tuple<int, int, int, int, int>(fullNum, MS2_Num, HCD_Num, ETD_Num, EThcD_Num));

                var scans = msDataFile.GetAllScansList();

                var times = ExtractTime_Glyco(scans);

                WriteExtractedTime_Glyco(filePath, Path.GetFileNameWithoutExtension(filePath) + "_time", times);

            }

            WriteExtractedNum_Glyco(MsDataFilePaths.First(), "scan_count", tuples);
        }

        private List<Tuple<double, double, double, double, string, string, string>> ExtractTime_Glyco(List<MsDataScan> scans)
        {
            List<Tuple<double, double, double, double, string, string, string>> times = new List<Tuple<double, double, double, double, string, string, string>>();


            for (int i = 1; i < scans.Count - 1; i++)
            {
                string scanType = "";
                string previousScanType = "";
                string nextScanType = "";

                if (scans[i].ScanFilter.Contains("Full ms "))
                {
                    scanType = "Full";
                }
                else if (scans[i].ScanFilter.Contains("hcd") && !scans[i].ScanFilter.Contains("etd"))
                {
                    scanType = "hcd";

                }
                else if (!scans[i].ScanFilter.Contains("hcd") && scans[i].ScanFilter.Contains("etd"))
                {
                    scanType = "etd";
                }
                else if (scans[i].ScanFilter.Contains("hcd") && scans[i].ScanFilter.Contains("etd"))
                {
                    scanType = "ethcd";
                }


                if (scans[i - 1].ScanFilter.Contains("Full ms "))
                {
                    previousScanType = "Full";
                }
                else if (scans[i - 1].ScanFilter.Contains("hcd") && !scans[i - 1].ScanFilter.Contains("etd"))
                {
                    previousScanType = "hcd";

                }
                else if (!scans[i - 1].ScanFilter.Contains("hcd") && scans[i - 1].ScanFilter.Contains("etd"))
                {
                    previousScanType = "etd";
                }
                else if (scans[i - 1].ScanFilter.Contains("hcd") && scans[i - 1].ScanFilter.Contains("etd"))
                {
                    previousScanType = "ethcd";
                }



                if (scans[i + 1].ScanFilter.Contains("Full ms "))
                {
                    nextScanType = "Full";
                }
                else if (scans[i + 1].ScanFilter.Contains("hcd") && !scans[i + 1].ScanFilter.Contains("etd"))
                {
                    nextScanType = "hcd";

                }
                else if (!scans[i + 1].ScanFilter.Contains("hcd") && scans[i + 1].ScanFilter.Contains("etd"))
                {
                    nextScanType = "etd";
                }
                else if (scans[i + 1].ScanFilter.Contains("hcd") && scans[i + 1].ScanFilter.Contains("etd"))
                {
                    nextScanType = "ethcd";
                }


                double previousTime = (scans[i].RetentionTime - scans[i - 1].RetentionTime) * 60000;

                double nextTime = (scans[i + 1].RetentionTime - scans[i].RetentionTime) * 60000;


                times.Add(new Tuple<double, double, double, double, string, string, string>(scans[i].RetentionTime, scans[i].InjectionTime.Value, previousTime, nextTime, scanType, previousScanType, nextScanType));

            }

            return times;
        }

        private void WriteExtractedTime_Glyco(string FilePath, string name, List<Tuple<double, double, double, double, string, string, string>> times)
        {
            var writtenFile = Path.Combine(Path.GetDirectoryName(FilePath), name + ".tsv");
            using (StreamWriter output = new StreamWriter(writtenFile))
            {
                output.WriteLine("RetentionTime\tInjectTime\tPreviousTime\tNextTime\tScanType\tPreviousScanType\tNextScanType");
                for (int i = 0; i < times.Count; i++)
                {
                    output.WriteLine(times[i].Item1 + "\t" + times[i].Item2 + "\t" + times[i].Item3 + "\t" + times[i].Item4 + "\t" + times[i].Item5 + "\t" + times[i].Item6 + "\t" + times[i].Item7);
                }
            }
        }

        private void WriteExtractedNum_Glyco(string FilePath, string name, List<Tuple<int, int, int, int, int>> tuples)
        {
            var writtenFile = Path.Combine(Path.GetDirectoryName(FilePath), name + ".tsv");
            using (StreamWriter output = new StreamWriter(writtenFile))
            {
                output.WriteLine("FullTotal\tMs2Total\tHcdTotal\tEtdTotal\tEThcDTotal");
                for (int i = 0; i < tuples.Count; i++)
                {
                    output.WriteLine(tuples[i].Item1 + "\t" + tuples[i].Item2 + "\t" + tuples[i].Item3 + "\t" + tuples[i].Item4 + "\t" + tuples[i].Item5);
                }
            }
        }


        //Extract precursor from all ms2Scans
        public void ExtractPrecursorInfo(List<string> MsDataFilePaths, MyFileManager spectraFileManager)
        {
            foreach (var filePath in MsDataFilePaths)
            {
                var msDataFile = spectraFileManager.LoadFile(filePath, new CommonParameters());

                var ms2Scans = msDataFile.GetAllScansList().Where(p=>p.MsnOrder!=1);

                WriteExtractPrecursorInfo(filePath, Path.GetFileNameWithoutExtension(filePath) + "_PrecursorInfo", ms2Scans);
            }

        }

        private void WriteExtractPrecursorInfo(string FilePath, string name, IEnumerable<MsDataScan> msDataScans)
        {
            var writtenFile = Path.Combine(Path.GetDirectoryName(FilePath), name + ".tsv");
            using (StreamWriter output = new StreamWriter(writtenFile))
            {
                output.WriteLine("ScanNum\tRT\tIsolationMz\tSelectedIonMZ\tMonoisotopicGuessMZ\tChargeStateGuess\tSelectedIonIntensity\tMonoisotopicGuessIntensity");
                foreach (var s in msDataScans)
                {
                    output.WriteLine(
                        s.OneBasedScanNumber + "\t" +
                        s.RetentionTime + "\t" +
                        (s.IsolationMz.HasValue? s.IsolationMz: -1) + "\t" +                    
                        (s.SelectedIonMZ.HasValue ? s.SelectedIonMZ.Value : -1) + "\t" +                       
                        (s.SelectedIonMonoisotopicGuessMz.HasValue ? s.SelectedIonMonoisotopicGuessMz : -1) + "\t" +
                        (s.SelectedIonChargeStateGuess.HasValue ? s.SelectedIonChargeStateGuess.Value : -1) + "\t" +
                        (s.SelectedIonIntensity.HasValue ? s.SelectedIonIntensity : -1) + "\t" +
                        (s.SelectedIonMonoisotopicGuessIntensity.HasValue? s.SelectedIonMonoisotopicGuessIntensity: -1)
                    );
                }

            }
        }

        //Extract precursor info with deconvolution from all ms2Scans
        public void ExtractPrecursorInfo_Decon(List<string> MsDataFilePaths, MyFileManager spectraFileManager)
        {
            foreach (var filePath in MsDataFilePaths)
            {
                var msDataFile = spectraFileManager.LoadFile(filePath, new CommonParameters());
                var commonPara = new CommonParameters(deconvolutionMaxAssumedChargeState: 6);
                var scans = MetaMorpheusTask.GetMs2Scans(msDataFile, null, commonPara).Where(p=>p.PrecursorCharge>1);



                WriteExtractPrecursorInfo_Decon(filePath, Path.GetFileNameWithoutExtension(filePath) + "_DeconPrecursorInfo", scans);
            }
        }

        private void WriteExtractPrecursorInfo_Decon(string FilePath, string name, IEnumerable<Ms2ScanWithSpecificMass> scans)
        {
            var writtenFile = Path.Combine(Path.GetDirectoryName(FilePath), name + ".tsv");
            using (StreamWriter output = new StreamWriter(writtenFile))
            {
                output.WriteLine("ScanNum\tRT\tPrecursorMass\tPrecursorMz\tCharge");
                foreach (var s in scans)
                {
                    output.WriteLine(
                        s.OneBasedScanNumber + "\t" +
                        s.RetentionTime + "\t" +
                        s.PrecursorMass + "\t" +
                        s.PrecursorMonoisotopicPeakMz + "\t" +
                        s.PrecursorCharge
                        
                    );
                }

            }
        }

    }
}
