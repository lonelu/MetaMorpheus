using EngineLayer;
using MassSpectrometry;
using OxyPlot;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using ViewModels;

namespace MetaDrawGUI
{
    public enum DeconvolutorSkill
    {
        DeconSeleScan = 0,
        PlotAvaragineModel = 1,
        DeconQuant = 2,
        DeconAllChargeParsi = 3,
        DeconWatch = 4,
        DeconPeak_Neucode = 5,
        DeconChargeByPeak = 6,
        DeconDrawTwoScan = 7
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

        public List<ChargeDeconEnvelope> ScanChargeEnvelopes { get; set; } = new List<ChargeDeconEnvelope>();
        public List<IsoEnvelop> IsotopicEnvelopes { get; set; } = new List<IsoEnvelop>();
        public Dictionary<int, MzPeak> Mz_zs { get; set; } = new Dictionary<int, MzPeak>();
        public List<Dictionary<int, MzPeak>> Mz_zs_list { get; set; } = new List<Dictionary<int, MzPeak>>();

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

        public MzSpectrumBU mzSpectrumBU
        {
            get
            {
                return new MzSpectrumBU(_thanos.msDataScan.MassSpectrum.XArray, _thanos.msDataScan.MassSpectrum.YArray, true);
            }
        }

        public int[] indexByY
        {
            get
            {
                return mzSpectrumBU.ExtractIndicesByY().ToArray();
            }
        }

        //TO DO:this need to be change when the mzSpectrumBU change.
        public int DeconPeakInd { get; set; } = 0;

        public HashSet<double> seenPeaks { get; set; } = new HashSet<double>();

        public void Decon()
        {
            _thanos.msDataScan = _thanos.msDataScans.Where(p => p.OneBasedScanNumber == _thanos.ControlParameter.deconScanNum).First();
            MzSpectrumBU mzSpectrumBU = new MzSpectrumBU(_thanos.msDataScan.MassSpectrum.XArray, _thanos.msDataScan.MassSpectrum.YArray, true);

            //IsotopicEnvelopes = mzSpectrumBU.DeconvoluteBU(msDataScan.ScanWindowRange, DeconvolutionParameter).OrderBy(p => p.monoisotopicMass).ToList();
            //IsotopicEnvelopes = mzSpectrumBU.Deconvolute(msDataScan.ScanWindowRange, _thanos.DeconvolutionParameter).OrderBy(p => p.monoisotopicMass).ToList();
            //IsotopicEnvelopes = mzSpectrumBU.ParallelDeconvolute(msDataScan.ScanWindowRange, DeconvolutionParameter, 8).OrderBy(p => p.monoisotopicMass).ToList();
            IsotopicEnvelopes = mzSpectrumBU.MsDeconv_Deconvolute(_thanos.msDataScan.ScanWindowRange, _thanos.DeconvolutionParameter).OrderBy(p => p.MonoisotopicMass).ToList();

            int i = 1;
            foreach (var item in IsotopicEnvelopes)
            {
                envolopObservableCollection.Add(new EnvolopForDataGrid(i, item.IsNeuCode, item.ExperimentIsoEnvelop.First().Item1, item.Charge, item.MonoisotopicMass, item.TotalIntensity));
                i++;
            }

            Model = MainViewModel.DrawScan(_thanos.msDataScan);

            double max = _thanos.deconvolutor.mzSpectrumBU.YArray.Max();
            int indexMax = _thanos.deconvolutor.mzSpectrumBU.YArray.ToList().IndexOf(max);

            _thanos.deconvolutor.Mz_zs = ChargeDecon.FindChargesForPeak(_thanos.deconvolutor.mzSpectrumBU, indexMax);
            int ind = 1;
            foreach (var mz_z in _thanos.deconvolutor.Mz_zs)
            {
                _thanos.deconvolutor.chargeEnvelopesCollection.Add(new ChargeEnvelopesForDataGrid(ind, mz_z.Value.Mz, mz_z.Key, mz_z.Value.Intensity));
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

        public void DeconPeak_NeuCode()
        {
            if (DeconPeakInd < mzSpectrumBU.Size)
            {
                if (!seenPeaks.Contains(mzSpectrumBU.XArray[indexByY[DeconPeakInd]]))
                {
                    var envo = mzSpectrumBU.DeconvolutePeak_NeuCode(indexByY[DeconPeakInd], _thanos.DeconvolutionParameter);
                    if (envo != null)
                    {
                        foreach (var p in envo.peaks)
                        {
                            seenPeaks.Add(p.mz);
                        }
                        if (envo.Partner != null)
                        {
                            foreach (var p in envo.Partner.peaks)
                            {
                                seenPeaks.Add(p.mz);
                            }
                        }
                        //_thanos.deconvolutor.DeconModel = DeconViewModel.UpdataModelForDecon(_thanos.msDataScan, envo);

                    }
                }
            }
            DeconPeakInd++;
        }

        public void PlotDeconModel()
        {
            if (_thanos.msDataScan != null)
            {
                MzSpectrumBU mzSpectrumBU = new MzSpectrumBU(_thanos.msDataScan.MassSpectrum.XArray, _thanos.msDataScan.MassSpectrum.YArray, true);                
                DeconModel = DeconViewModel.UpdateModelForDeconModel(mzSpectrumBU, _thanos.ControlParameter.modelStartNum);
            }
            else
            {

                var mzSpectrumBU = new MzSpectrumBU(new double[] { 1 }, new double[] { 1 }, true);                
                DeconModel = DeconViewModel.UpdateModelForDeconModel(mzSpectrumBU, _thanos.ControlParameter.modelStartNum);
            }
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

        public void DeconAllChargeParsi()
        {
            var chargeDeconPerMS1Scans = _thanos.msDataFileDecon.ChargeDeconvolutionFile(_thanos.msDataScans, _thanos.CommonParameters, _thanos.DeconvolutionParameter);
            List<ChargeParsi> chargeParsis = _thanos.msDataFileDecon.ChargeParsimony(chargeDeconPerMS1Scans, new SingleAbsoluteAroundZeroSearchMode(2.2), new SingleAbsoluteAroundZeroSearchMode(5));

            var total = _thanos.msDataScans.Where(p => p.MsnOrder == 2).Count();
            int ms2ScanBeAssigned = chargeParsis.Sum(p => p.MS2ScansCount);
            int a0 = chargeParsis.Where(p => p.MS2ScansCount == 0).Count();
            int a1 = chargeParsis.Where(p => p.MS2ScansCount == 1).Count();
            int a2 = chargeParsis.Where(p => p.MS2ScansCount == 2).Count();
            int a3 = chargeParsis.Where(p => p.MS2ScansCount == 3).Count();
            int a4 = chargeParsis.Where(p => p.MS2ScansCount == 4).Count();
            int a5 = chargeParsis.Where(p => p.MS2ScansCount == 5).Count();
            int a6 = chargeParsis.Where(p => p.MS2ScansCount == 6).Count();
            int a7 = chargeParsis.Where(p => p.MS2ScansCount == 7).Count();
            int a8 = chargeParsis.Where(p => p.MS2ScansCount == 8).Count();
            int a9 = chargeParsis.Where(p => p.MS2ScansCount == 9).Count();
            int a10 = chargeParsis.Where(p => p.MS2ScansCount == 10).Count();
            int a11 = chargeParsis.Where(p => p.MS2ScansCount == 11).Count();
            int a12 = chargeParsis.Where(p => p.MS2ScansCount == 12).Count();
            int a13 = chargeParsis.Where(p => p.MS2ScansCount == 13).Count();
            int a14 = chargeParsis.Where(p => p.MS2ScansCount == 14).Count();
            int a15 = chargeParsis.Where(p => p.MS2ScansCount == 15).Count();
            int a16 = chargeParsis.Where(p => p.MS2ScansCount == 16).Count();
            int a17 = chargeParsis.Where(p => p.MS2ScansCount == 17).Count();
            int a18 = chargeParsis.Where(p => p.MS2ScansCount == 18).Count();
            int a19 = chargeParsis.Where(p => p.MS2ScansCount == 19).Count();
            int a20 = chargeParsis.Where(p => p.MS2ScansCount == 20).Count();
            var test = chargeParsis.Where(p => p.ExsitedMS1Scans.Contains(2076)).ToList();
            _thanos.msDataFileDecon.ChargeDeconWriteToTSV(chargeDeconPerMS1Scans, Path.GetDirectoryName(_thanos.MsDataFilePaths.First()), "ChargeDecon");
        }

        public void DeconWatch()
        {
            var MS1Scans = _thanos.msDataScans.Where(p => p.MsnOrder == 1).ToList();
            List<WatchEvaluation> evalution = new List<WatchEvaluation>();
            int i = 0;
            while (i < MS1Scans.Count)
            {
                var theScanNum = MS1Scans[i].OneBasedScanNumber;
                var theRT = MS1Scans[i].RetentionTime;
                MzSpectrumBU mzSpectrumBU = new MzSpectrumBU(MS1Scans[i].MassSpectrum.XArray, MS1Scans[i].MassSpectrum.YArray, true);

                var watch = System.Diagnostics.Stopwatch.StartNew();

                var isotopicEnvelopes = mzSpectrumBU.Deconvolute(MS1Scans[i].ScanWindowRange, _thanos.DeconvolutionParameter).OrderBy(p => p.monoisotopicMass).ToList();
                watch.Stop();

                var watch1 = System.Diagnostics.Stopwatch.StartNew();

                var chargeDecon = mzSpectrumBU.ChargeDeconvolution(isotopicEnvelopes);

                watch1.Stop();

                var theEvaluation = new WatchEvaluation(theScanNum, theRT, watch.ElapsedMilliseconds, watch1.ElapsedMilliseconds);
                evalution.Add(theEvaluation);
                i++;

            }

            var writtenFile = Path.Combine(Path.GetDirectoryName(_thanos.MsDataFilePaths.First()), "watches.mytsv");
            using (StreamWriter output = new StreamWriter(writtenFile))
            {
                output.WriteLine("ScanNum\tRT\tIsotopicDecon\tIsoTopicDeconByParallel\tChargeDecon");
                foreach (var theEvaluation in evalution)
                {
                    output.WriteLine(theEvaluation.TheScanNumber.ToString() + "\t" + theEvaluation.TheRT + "\t" + theEvaluation.WatchIsoDecon.ToString() + "\t" + theEvaluation.WatchChaDecon.ToString());
                }
            }
        }

        public void DeconChargeByPeak()
        {
            MzSpectrumBU mzSpectrumBU = new MzSpectrumBU(_thanos.msDataScan.MassSpectrum.XArray, _thanos.msDataScan.MassSpectrum.YArray, true);

            double deconChargeMass = _thanos.ControlParameter.DeconChargeMass;

            int index = ChargeDecon.GetCloestIndex(deconChargeMass, mzSpectrumBU.XArray);

            var theMz_zs = ChargeDecon.FindChargesForPeak(_thanos.deconvolutor.mzSpectrumBU, index);

            _thanos.deconvolutor.Mz_zs_list.Add(theMz_zs);

            int ind = 1;
            foreach (var mz_z in theMz_zs)
            {
                _thanos.deconvolutor.chargeEnvelopesCollection.Add(new ChargeEnvelopesForDataGrid(ind, mz_z.Value.Mz, mz_z.Key, mz_z.Value.Intensity));
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

    }
}
