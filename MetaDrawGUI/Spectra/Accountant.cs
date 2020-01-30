using System;
using System.Collections.Generic;
using BoxCar;
using System.IO;
using MassSpectrometry;
using EngineLayer;
using TaskLayer;
using System.Linq;
using System.ComponentModel;
using ViewModels;
using OxyPlot;

namespace MetaDrawGUI
{
    public class Accountant : INotifyPropertyChanged
    {
        //View model
        private ScanInfoViewModel ScanInfoViewModel = new ScanInfoViewModel();
        public PlotModel ScanInfoModel
        {
            get
            {
                return ScanInfoViewModel.privateModel;
            }
            set
            {
                ScanInfoViewModel.privateModel = value;
                NotifyPropertyChanged("ScanInfoModel");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        //Scan Info
        public List<ScanInfo> ScanInfos { get; set; } = new List<ScanInfo>();
 
        //Extract Scan Info: scan type number and time.
        public void ExtractNumTime(List<string> MsDataFilePaths, MyFileManager spectraFileManager, Tuple<double, double> timeRange)
        {
            List<List<ScanInfo>> scanInfoSet = new List<List<ScanInfo>>();

            List<string> filePaths = new List<string>();

            foreach (var filePath in MsDataFilePaths)
            {          
                var msDataFile = spectraFileManager.LoadFile(filePath, new CommonParameters());

                var scans = msDataFile.GetAllScansList();

                var scanInfos = ExtractTime(scans, timeRange);

                scanInfoSet.Add(scanInfos);
                filePaths.Add(filePath);

                ScanInfos = scanInfos;

                WriteExtractedTime(filePath, Path.GetFileNameWithoutExtension(filePath) + "_time");

            }

            WriteExtractedNum(filePaths, scanInfoSet);
        }

        private List<ScanInfo> ExtractTime(List<MsDataScan> allScans, Tuple<double, double> timeRange)
        {
            var scans = allScans.Where(p => p.RetentionTime >= timeRange.Item1 && p.RetentionTime <= timeRange.Item2).ToList();

            List<ScanInfo> scanInfos = new List<ScanInfo>();

            for (int i = 0; i < scans.Count; i++)
            {
                string scanType = "";

                if (scans[i].ScanFilter.Contains("Full ms "))
                {
                    scanType = "Full";
                }
                else if (scans[i].ScanFilter.Contains("msx ms "))
                {
                    scanType = "Msx";

                }
                else
                {
                    if (scans[i].ScanFilter.Contains("msx ms2 "))
                    {
                        scanType = "MsxMs2";
                    }
                    else
                    {
                        if (scans[i].ScanFilter.Contains("hcd") && !scans[i].ScanFilter.Contains("etd"))
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
                    }

                }                     

                ScanInfo scanInfo = new ScanInfo(scans[i].OneBasedScanNumber, scans[i].RetentionTime, scans[i].InjectionTime.Value, scanType);

                scanInfos.Add(scanInfo);
            }

            for (int i = 1; i < scans.Count-1; i++)
            {
                scanInfos[i].PreviousScan = scanInfos[i - 1];
                scanInfos[i].NextScan = scanInfos[i + 1];



            }

            return scanInfos;
        }

        private void WriteExtractedTime(string FilePath, string name)
        {
            var writtenFile = Path.Combine(Path.GetDirectoryName(FilePath), name + ".tsv");
            using (StreamWriter output = new StreamWriter(writtenFile))
            {
                output.WriteLine("ScanNumber\tRetentionTime\tInjectTime\tPreviousTime\tScanType");
                foreach(var s in ScanInfos)
                {
                    output.WriteLine(s.OneBaseScanNumber + "\t" + s.RententionTime + "\t" + s.InjectTime + "\t" + s.PreviousTime + "\t"  + s.ScanType);
                }
            }
        }

        private void WriteExtractedNum(List<string> filePaths, List<List<ScanInfo>> scanInfoSet)
        {
            var writtenFile = Path.Combine(Path.GetDirectoryName(filePaths.First()), Path.GetFileNameWithoutExtension(filePaths.First()) + "scan_count" +  ".tsv");
            using (StreamWriter output = new StreamWriter(writtenFile))
            {
                output.WriteLine("File\tFullTotal\tMsxMs\tMsxMs2\tHcdTotal\tEtdTotal\tEThcDTotal");

                int i = 0;
                foreach (var s in scanInfoSet)
                {
                    output.WriteLine(filePaths[i]
                        + "\t" + s.Where(p => p.ScanType == "Full").Count()
                        + "\t" + s.Where(p => p.ScanType == "Msx").Count()
                        + "\t" + s.Where(p => p.ScanType == "MsxMs2").Count()
                        + "\t" + s.Where(p => p.ScanType == "hcd").Count()
                        + "\t" + s.Where(p => p.ScanType == "etd").Count()
                        + "\t" + s.Where(p => p.ScanType == "ethcd").Count()
                    );
                    i++;
                }
        
            }
        }


        //Extract precursor from all ms2Scans
        public void ExtractPrecursorInfo(List<string> MsDataFilePaths, MyFileManager spectraFileManager)
        {
            foreach (var filePath in MsDataFilePaths)
            {
                var msDataFile = spectraFileManager.LoadFile(filePath, new CommonParameters());

                var ms2Scans = msDataFile.GetAllScansList().Where(p => p.MsnOrder != 1);

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
                        (s.IsolationMz.HasValue ? s.IsolationMz : -1) + "\t" +
                        (s.SelectedIonMZ.HasValue ? s.SelectedIonMZ.Value : -1) + "\t" +
                        (s.SelectedIonMonoisotopicGuessMz.HasValue ? s.SelectedIonMonoisotopicGuessMz : -1) + "\t" +
                        (s.SelectedIonChargeStateGuess.HasValue ? s.SelectedIonChargeStateGuess.Value : -1) + "\t" +
                        (s.SelectedIonIntensity.HasValue ? s.SelectedIonIntensity : -1) + "\t" +
                        (s.SelectedIonMonoisotopicGuessIntensity.HasValue ? s.SelectedIonMonoisotopicGuessIntensity : -1)
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
                var scans = MetaMorpheusTask.GetMs2Scans(msDataFile, null, commonPara).Where(p => p.PrecursorCharge > 1);



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

    public class ScanInfo
    {
        public ScanInfo(int scanNumber, double rt, double it, string scan)
        {
            OneBaseScanNumber = scanNumber;
            RententionTime = rt;
            InjectTime = it;
            ScanType = scan;

        }

        public int OneBaseScanNumber { get; set; }
        public double RententionTime { get; set; }
        public double InjectTime { get; set; }
        public string ScanType { get; set; }
        public ScanInfo PreviousScan { get; set; }
        public ScanInfo NextScan { get; set; }

        public double PreviousTime
        {
            get
            {
                if (PreviousScan!= null)
                {
                    return RententionTime - PreviousScan.RententionTime;
                }
                return 0;
            }
        }

        public string types
        {
            get
            {
                var ptype = "";
                var pptype = "";
                if (PreviousScan!= null)
                {
                    ptype = PreviousScan.ScanType  + "_";
                    if (PreviousScan.PreviousScan!=null)
                    {
                        pptype = PreviousScan.PreviousScan.ScanType + "_";
                    }
                }
                var ntype = "";
                if (NextScan!= null)
                {
                    ntype = "_" + NextScan.ScanType;
                }

                return  pptype + ptype + "_" + ScanType + ntype;
            }
        }

    }
}
