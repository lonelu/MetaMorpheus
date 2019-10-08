using MassSpectrometry;
using OxyPlot;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using ViewModels;
using System.Threading;
using System.Threading.Tasks;
using System;
using Chemistry;

namespace MetaDrawGUI
{
    public enum DeconvolutorSkill
    {
        DeconSeleScan = 0,
        PlotAvaragineModel = 1,
        DeconQuant = 2,
        DeconTotalPartners = 3,
        DeconWatch = 4,
        DeconIsoByPeak = 5,
        DeconChargeByPeak = 6,
        DeconDrawTwoScan = 7,
        DeconDrawIntensityDistribution = 8,
        DeconCompareBoxVsNormalId = 9,
        IdFragmentationOptimize = 10
    }

    public class Deconvolutor: INotifyPropertyChanged
    {
        //TO DO: this is not the best way to link deconvolutor to thanos.
        public Thanos _thanos { get; set; }

        //Isotopic deconvolution envolop Data Grid
        private ObservableCollection<EnvolopForDataGrid> envolopObservableCollection = new ObservableCollection<EnvolopForDataGrid>();
        public ObservableCollection<EnvolopForDataGrid> envolopCollection
        {
            get
            {
                return envolopObservableCollection;
            }
            set
            {
                envolopObservableCollection = value;
                NotifyPropertyChanged("envolopCollection");
            }
        }

        //Charge deconvolution envolop Data Grid
        private ObservableCollection<ChargeEnvelopesForDataGrid> chargeEnvelopesObservableCollection = new ObservableCollection<ChargeEnvelopesForDataGrid>();
        public ObservableCollection<ChargeEnvelopesForDataGrid> chargeEnvelopesCollection
        {
            get
            {
                return chargeEnvelopesObservableCollection;
            }
            set
            {
                chargeEnvelopesObservableCollection = value;
                NotifyPropertyChanged("chargeEnvelopesCollection");
            }
        }

        private ObservableCollection<MsFeatureForDataGrid> MsFeatureObservableCollection = new ObservableCollection<MsFeatureForDataGrid>();
        public ObservableCollection<MsFeatureForDataGrid> MsFeatureCollection
        {
            get
            {
                return MsFeatureObservableCollection;
            }
            set
            {
                MsFeatureObservableCollection = value;
                NotifyPropertyChanged("MsFeatureCollection");
            }
        }

        public List<IsoEnvelop> IsotopicEnvelopes { get; set; } = new List<IsoEnvelop>();
        public List<(int charge, double mz, double intensity, int index)> Mz_zs { get; set; } = new List<(int charge, double mz, double intensity, int index)>();
        public List<ChargeEnvelop> ChargeEnvelops { get; set; } = new List<ChargeEnvelop>();

        //View model
        private MainViewModel mainViewModel = new MainViewModel();
        public PlotModel Model
        {
            get
            {
                return mainViewModel.privateModel;
            }
            set
            {
                mainViewModel.privateModel = value;
                NotifyPropertyChanged("Model");
            }
        }

        private DeconViewModel deconViewModel = new DeconViewModel();
        public PlotModel DeconModel
        {
            get
            {
                return deconViewModel.privateModel;
            }
            set
            {
                deconViewModel.privateModel = value;
                NotifyPropertyChanged("DeconModel");
            }
        }

        private PeakViewModel peakViewModel = new PeakViewModel();
        public PlotModel XicModel
        {
            get
            {
                return peakViewModel.privateModel;
            }
            set
            {
                peakViewModel.privateModel = value;
                NotifyPropertyChanged("XicModel");
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

        public MzSpectrumXY mzSpectrumXY
        {
            get
            {
                return new MzSpectrumXY(_thanos.msDataScan.MassSpectrum.XArray, _thanos.msDataScan.MassSpectrum.YArray, true);
            }
        }

        public int[] indexByY
        {
            get
            {
                return mzSpectrumXY.ExtractIndicesByY().ToArray();
            }
        }

        public void Decon()
        {
            _thanos.msDataScan = _thanos.msDataScans.Where(p => p.OneBasedScanNumber == _thanos.ControlParameter.deconScanNum).First();
            MzSpectrumXY mzSpectrumXY = new MzSpectrumXY(_thanos.msDataScan.MassSpectrum.XArray, _thanos.msDataScan.MassSpectrum.YArray, true);

            //IsotopicEnvelopes = mzSpectrumBU.DeconvoluteBU(msDataScan.ScanWindowRange, DeconvolutionParameter).OrderBy(p => p.monoisotopicMass).ToList();
            //IsotopicEnvelopes = mzSpectrumBU.Deconvolute(msDataScan.ScanWindowRange, _thanos.DeconvolutionParameter).OrderBy(p => p.monoisotopicMass).ToList();
            //IsotopicEnvelopes = mzSpectrumBU.ParallelDeconvolute(msDataScan.ScanWindowRange, DeconvolutionParameter, 8).OrderBy(p => p.monoisotopicMass).ToList();
            IsotopicEnvelopes = IsoDecon.MsDeconv_Deconvolute(mzSpectrumXY, _thanos.msDataScan.ScanWindowRange, _thanos.DeconvolutionParameter).OrderBy(p => p.MonoisotopicMass).ToList();

            int i = 1;
            foreach (var item in IsotopicEnvelopes)
            {
                envolopObservableCollection.Add(new EnvolopForDataGrid(i, item.HasPartner, item.ExperimentIsoEnvelop.First().Mz, item.Charge, item.MonoisotopicMass, item.TotalIntensity, item.MsDeconvScore, item.MsDeconvSignificance));
                i++;
            }

            Model = MainViewModel.DrawScan(_thanos.msDataScan);

            double max = _thanos.deconvolutor.mzSpectrumXY.YArray.Max();
            int indexMax = _thanos.deconvolutor.mzSpectrumXY.YArray.ToList().IndexOf(max);

            _thanos.deconvolutor.Mz_zs = ChargeDecon.FindChargesForPeak(_thanos.deconvolutor.mzSpectrumXY, indexMax, _thanos.DeconvolutionParameter);
            int ind = 1;
            foreach (var mz_z in _thanos.deconvolutor.Mz_zs)
            {
                _thanos.deconvolutor.chargeEnvelopesCollection.Add(new ChargeEnvelopesForDataGrid(ind, mz_z.mz.ToMass(mz_z.charge), 0, 0, 0, null));
                ind++;
            }
            chargeEnvelopesCollection = chargeEnvelopesObservableCollection;

            //_thanos.deconvolutor.Mz_zs_list = ChargeDecon.FindChargesForScan(_thanos.deconvolutor.mzSpectrumBU);
            //int ind = 1;
            //foreach (var mz_z in _thanos.deconvolutor.Mz_zs_list.SelectMany(p=>p))
            //{
            //    _thanos.deconvolutor.chargeEnvelopesCollection.Add(new ChargeEnvelopesForDataGrid(ind, mz_z.Value.Mz, mz_z.Key, mz_z.Value.Intensity));
            //    ind++;
            //}
            //chargeEnvelopesCollection = chargeEnvelopesObservableCollection;
        }

        public void DeconIsoByPeak()
        {
            MzSpectrumXY mzSpectrumXY = new MzSpectrumXY(_thanos.msDataScan.MassSpectrum.XArray, _thanos.msDataScan.MassSpectrum.YArray, true);

            double deconChargeMass = _thanos.ControlParameter.DeconChargeMass;

            int index = ChargeDecon.GetCloestIndex(deconChargeMass, mzSpectrumXY.XArray);

            var envo = IsoDecon.MsDeconvExperimentPeak(mzSpectrumXY, index, _thanos.DeconvolutionParameter, 0);

            if (envo != null)
            {
                _thanos.deconvolutor.DeconModel = DeconViewModel.UpdataModelForDecon(_thanos.msDataScan, envo);
            }

        }

        public void PlotDeconModel()
        {

             //Model = DeconViewModel.DrawDeconModel(_thanos.ControlParameter.modelStartNum);
            //Model = DeconViewModel.DrawDeconModelWidth();
            Model = DeconViewModel.DrawChargeDeconModel();
        }

        public void DeconQuant()
        {         
            var ms1ScanForDecon = new List<MsDataScan>();
            foreach (var scan in _thanos.msDataScans.Where(p => p.MsnOrder == 1))
            {
                //if (scan.OneBasedScanNumber >= 2 && msDataScans.ElementAt(scan.OneBasedScanNumber-2).MsnOrder == 1)
                //if (scan.ScanWindowRange.Minimum == 349) //TO DO: this is temp special for the coon lab neocode data.              
                ms1ScanForDecon.Add(scan);

            }
            //var a = ms1ScanForDecon.Count();
            //var b = msDataScans.Where(p => p.MsnOrder == 1).Count();
            //var c = msDataScans.Where(p => p.MsnOrder == 2).Count();
            _thanos.msDataFileDecon.DeconQuantFile(ms1ScanForDecon, _thanos.MsDataFilePaths.First(), _thanos.CommonParameters, _thanos.DeconvolutionParameter);
        }

        public void DeconTotalPartners()
        {
            var MS1Scans = _thanos.msDataScans.Where(p => p.MsnOrder == 1).ToList();

            Tuple<int, double, int, int, double>[] isoInfo = new Tuple<int, double, int, int, double>[MS1Scans.Count];


            //for (int i = 0; i < MS1Scans.Count; i++)
            Parallel.For(0, MS1Scans.Count, i =>
            {
                var theScanNum = MS1Scans[i].OneBasedScanNumber;
                var theRT = MS1Scans[i].RetentionTime;

                MzSpectrumXY mzSpectrumXY = new MzSpectrumXY(MS1Scans[i].MassSpectrum.XArray, MS1Scans[i].MassSpectrum.YArray, true);
                var isos = IsoDecon.MsDeconv_Deconvolute(mzSpectrumXY, MS1Scans[i].ScanWindowRange, _thanos.DeconvolutionParameter).OrderBy(p => p.MsDeconvScore).ToList();

                isoInfo[i] = new Tuple<int, double, int, int, double>(theScanNum, theRT, isos.Count(), isos.Where(p => p.HasPartner).Count(), (double)isos.Where(p => p.HasPartner).Count() / (double)isos.Count());
            }
            );

            var writtenFile = Path.Combine(Path.GetDirectoryName(_thanos.MsDataFilePaths.First()), "partner_count.mytsv");
            using (StreamWriter output = new StreamWriter(writtenFile))
            {
                output.WriteLine("ScanNum\tRT\tIsoCount\tPartnerCount\tRatio");
                foreach (var theEvaluation in isoInfo)
                {
                    output.WriteLine(theEvaluation.Item1 + "\t" + theEvaluation.Item2 + "\t" + theEvaluation.Item3 + "\t" + theEvaluation.Item4
                        + "\t" + theEvaluation.Item5);
                }
            }
        }

        public void DeconWatch()
        {
            var MS1Scans = _thanos.msDataScans.Where(p => p.MsnOrder == 1).ToList();

            Tuple<int, double, int, long, int, long>[] evaluation = new Tuple<int, double, int, long, int, long>[MS1Scans.Count];

            for (int i = 0; i < MS1Scans.Count; i++)
            {
                var theScanNum = MS1Scans[i].OneBasedScanNumber;
                var theRT = MS1Scans[i].RetentionTime;
                MzSpectrumXY mzSpectrumXY = new MzSpectrumXY(MS1Scans[i].MassSpectrum.XArray, MS1Scans[i].MassSpectrum.YArray, true);

                var watch = System.Diagnostics.Stopwatch.StartNew();

                var isotopicEnvelopes = IsoDecon.MsDeconv_Deconvolute(mzSpectrumXY, MS1Scans[i].ScanWindowRange, _thanos.DeconvolutionParameter).OrderBy(p => p.MsDeconvScore).ToList();
                watch.Stop();

                var watch1 = System.Diagnostics.Stopwatch.StartNew();
                
                var chargeDecon = ChargeDecon.QuickChargeDeconForScan(mzSpectrumXY, _thanos.DeconvolutionParameter, out isotopicEnvelopes);

                watch1.Stop();

                evaluation[i] = new Tuple<int, double, int, long, int, long>(theScanNum, theRT, isotopicEnvelopes.Count, watch.ElapsedMilliseconds, chargeDecon.Count, watch1.ElapsedMilliseconds);
            }

            var writtenFile = Path.Combine(Path.GetDirectoryName(_thanos.MsDataFilePaths.First()), "watches_MetaDraw.mytsv");
            using (StreamWriter output = new StreamWriter(writtenFile))
            {
                output.WriteLine("ScanNum\tRT\tIsoCount\tIsotopicDeconTime\tChargeCount\tChargeDeconTime");
                foreach (var theEvaluation in evaluation)
                {
                    output.WriteLine(theEvaluation.Item1 + "\t" + theEvaluation.Item2 + "\t" + theEvaluation.Item3 + "\t" + theEvaluation.Item4
                        + "\t" + theEvaluation.Item5 + "\t" + theEvaluation.Item6 );
                }
            }
        }

        public void DeconChargeByPeak()
        {
            MzSpectrumXY mzSpectrumXY = new MzSpectrumXY(_thanos.msDataScan.MassSpectrum.XArray, _thanos.msDataScan.MassSpectrum.YArray, true);

            double deconChargeMass = _thanos.ControlParameter.DeconChargeMass;

            int index = ChargeDecon.GetCloestIndex(deconChargeMass, mzSpectrumXY.XArray);

            var theMz_zs = ChargeDecon.FindChargesForPeak(_thanos.deconvolutor.mzSpectrumXY, index, _thanos.DeconvolutionParameter);

            int ind = 1;
            foreach (var mz_z in theMz_zs)
            {
                _thanos.deconvolutor.chargeEnvelopesCollection.Add(new ChargeEnvelopesForDataGrid(ind, mz_z.mz.ToMass(mz_z.charge), 0, 0, 0, null));
                ind++;
            }
            chargeEnvelopesCollection = chargeEnvelopesObservableCollection;

            Model = ChargeEnveViewModel.UpdataModelForChargeEnve(Model, theMz_zs);

        }

        public void PlotTwoScan()
        {
            if (_thanos.msDataScan != null && _thanos.msDataScan.OneBasedScanNumber < _thanos.msDataScans.Count)
            {
                var anotherScan = _thanos.msDataScans[_thanos.msDataScan.OneBasedScanNumber];
                Model = ScanCompareViewModel.DrawScan(_thanos.msDataScan, anotherScan);
            }
        }

        public void PlotIntensityDistribution()
        {
            if (_thanos.msDataScan != null)
            {
                Model = DeconViewModel.DrawIntensityDistibution(_thanos.msDataScan);
            }
        }

        public void DeconCompareBoxVsNormalId()
        {
            HashSet<int> seenMs2ScanNum = new HashSet<int>();
            List<(int scanNum, double RT, int isMatch, double diff, int same)> diff_plot = new List<(int scanNum, double RT, int isMatch, double diff, int same)>();

            for (int i = 0; i < _thanos.msDataScans.Count; i++)
            {
                if (_thanos.msDataScans[i].MsnOrder == 1 || seenMs2ScanNum.Contains(i))
                {
                    continue;
                }

                seenMs2ScanNum.Add(i);
                seenMs2ScanNum.Add(i+1);

                if (_thanos.msDataScans[i+1].MsnOrder == 1)
                {
                    break;
                }


                var scanNum = i+1;
                var RT = _thanos.msDataScans[i].RetentionTime;
                var isMatch = 0;
                var diff = 0;
                var same = 0;

                var matched_d = _thanos.simplePsms.Where(p => p.ScanNum == i+1).ToList();
                var matched_n = _thanos.simplePsms.Where(p => p.ScanNum == i+2).ToList();

                if (matched_d.Count > 0 && matched_n.Count >0)
                {
                     isMatch = 3;
                     diff = matched_d.First().MatchedPeakNum - matched_n.First().MatchedPeakNum;
                    if (matched_d.First().PrecursorMz == matched_n.First().PrecursorMz)
                    {
                        same = 1;
                    }
                }
                else if(matched_d.Count == 0 && matched_n.Count > 0)
                {
                    isMatch = 2;
                    diff = -20;
                }
                else if (matched_d.Count > 0 && matched_n.Count == 0 )
                {
                    isMatch = 1;
                    diff = 20;
                }
                else
                {
                    isMatch = 0;
                }

                diff_plot.Add((scanNum, RT, isMatch, diff, same));
            }

            BoxMerger.WriteExtractedBoxVsNormal(_thanos.ResultFilePaths.First(), diff_plot);

            var test = diff_plot.Where(p => p.isMatch == 3 && p.same == 0).ToList();

            Model = ScanCompareViewModel.DrawBoxVsNormalId(diff_plot);
        }

        public void IdFragmentationOptimize()
        {
            List<(SimplePsm, MsDataScan)> x = new List<(SimplePsm, MsDataScan)>();
            foreach (var psm in _thanos.simplePsms)
            {
                var scan = _thanos.msDataScans.Where(p => p.OneBasedScanNumber == psm.ScanNum).First();
                x.Add((psm, scan));
            }

            var writtenFile = Path.Combine(Path.GetDirectoryName(_thanos.ResultFilePaths.First()), "FragmentationOptimization.mytsv");
            using (StreamWriter output = new StreamWriter(writtenFile))
            {
                output.WriteLine("ScanNum\tChargeState\tMonoMass\tMatchedPeakNum\tNterMatchedPeakNum\tCterMatchedPeakNum\tNterMatchedPeakRatio\tCterMatchedPeakRatio\tHcdEnergy\tScanFilter");
                foreach (var c in x)
                {
                    output.WriteLine(c.Item1.ScanNum + "\t" + c.Item1.ChargeState + "\t" + c.Item1.PrecursorMass + "\t" + c.Item1.MatchedPeakNum
                        + "\t" + c.Item1.NterMatchedPeakNum + "\t" + c.Item1.CTerMatchedPeakNum + "\t" + c.Item1.NTerMatchedPeakIntensityRatio + "\t" + c.Item1.CTerMatchedPeakIntensityRatio
                        + "\t" + c.Item2.HcdEnergy  + "\t" + c.Item2.ScanFilter
                        );
                }
            }

        }
    }
}
