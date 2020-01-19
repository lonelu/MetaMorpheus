using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.IO;
using ViewModels;
using EngineLayer;
using System.Collections.Generic;
using MzLibUtil;
using System.Text.RegularExpressions;
using MassSpectrometry;
using System.Globalization;
using System.ComponentModel;
using TaskLayer;
using System.Threading.Tasks;
using Chemistry;

namespace MetaDrawGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        //Data file and Result file Data Gid
        public readonly ObservableCollection<RawDataForDataGrid> spectraFilesObservableCollection = new ObservableCollection<RawDataForDataGrid>();
        public readonly ObservableCollection<RawDataForDataGrid> resultFilesObservableCollection = new ObservableCollection<RawDataForDataGrid>();

        //All Scan Data Grid
        public readonly ObservableCollection<AllScansForDataGrid> allScansObservableCollection = new ObservableCollection<AllScansForDataGrid>();
        public readonly ObservableCollection<SpectrumForDataGrid> spectrumNumsObservableCollection = new ObservableCollection<SpectrumForDataGrid>();
    
        public string resultsFilePath;            

        public Thanos thanos = new Thanos();
        public Action action { get; set; }

        //MultiproteaseCrosslink
        private readonly ObservableCollection<RawDataForDataGrid> MutiProteaseCrosslinkResultFilesObservableCollection = new ObservableCollection<RawDataForDataGrid>();

        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropChanged(string name)
        {
            var eh = this.PropertyChanged;
            if (eh != null)
            {
                eh(this, new PropertyChangedEventArgs(name));
            }
        }

        public MainWindow()
        {           
            InitializeComponent();

            PopulateChoice();

            dataGridMassSpectraFiles.DataContext = spectraFilesObservableCollection;

            dataGridResultFiles.DataContext = resultFilesObservableCollection;

            dataGridPsms.DataContext = spectrumNumsObservableCollection;

            dataGridAllScanNums.DataContext = allScansObservableCollection;

            Title = "MetaDraw" + GlobalVariables.MetaMorpheusVersion;

            //CommonParameters = new CommonParameters();
            productMassToleranceComboBox.Items.Add("Da");
            productMassToleranceComboBox.Items.Add("ppm");
            UpdatePanel();


        }

        private void PopulateChoice()
        {
            foreach (string aSkill in Enum.GetNames(typeof(Skill)))
            {
                {
                    cmbAction.Items.Add(aSkill);
                }
            }
        }

        private void UpdatePanel()
        {
            //productMassToleranceTextBox.Text = CommonParameters.ProductMassTolerance.Value.ToString(CultureInfo.InvariantCulture);
            //productMassToleranceComboBox.SelectedIndex = CommonParameters.ProductMassTolerance is AbsoluteTolerance ? 0 : 1;
            TxtScanNumCountRangeLow.Text = thanos.ControlParameter.LCTimeRange.Item1.ToString();
            TxtScanNumCountRangeHigh.Text = thanos.ControlParameter.LCTimeRange.Item2.ToString();
        }

        private void UpdateField()
        {
            thanos.ControlParameter.LCTimeRange = new Tuple<double, double>(  double.Parse(TxtScanNumCountRangeLow.Text), double.Parse(TxtScanNumCountRangeHigh.Text));
        }

        #region Basic file handle function

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (true)
            {
                string[] files = ((string[])e.Data.GetData(DataFormats.FileDrop)).OrderBy(p => p).ToArray();

                if (files != null)
                {
                    foreach (var draggedFilePath in files)
                    {
                        if (Directory.Exists(draggedFilePath))
                        {
                            foreach (string file in Directory.EnumerateFiles(draggedFilePath, "*.*", SearchOption.AllDirectories))
                            {
                                AddAFile(file);
                            }
                        }
                        else
                        {
                            AddAFile(draggedFilePath);
                        }
                        dataGridMassSpectraFiles.CommitEdit(DataGridEditingUnit.Row, true);
                        dataGridMassSpectraFiles.Items.Refresh();
                    }
                }

            }
        }

        private void btnClearFiles_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button).Name == "btnClearResultFiles")
                resultFilesObservableCollection.Clear();
            else
                spectraFilesObservableCollection.Clear();
        }

        private void btnAddFiles_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog1 = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Spectra Files(*.raw;*.mzML)|*.raw;*.mzML",
                FilterIndex = 1,
                RestoreDirectory = true,
                Multiselect = true
            };
            if (openFileDialog1.ShowDialog() == true)
                foreach (var rawDataFromSelected in openFileDialog1.FileNames.OrderBy(p => p))
                {
                    AddAFile(rawDataFromSelected);
                }
            dataGridMassSpectraFiles.Items.Refresh();
        }

        private void btnAddResults_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog1 = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Result Files(*.csv;*..psmtsv)|*.csv;*.psmtsv",
                FilterIndex = 1,
                RestoreDirectory = true,
                Multiselect = true
            };
            if (openFileDialog1.ShowDialog() == true)
                foreach (var rawDataFromSelected in openFileDialog1.FileNames.OrderBy(p => p))
                {
                    AddAFile(rawDataFromSelected);
                }
            dataGridResultFiles.Items.Refresh();
        }

        public void AddAFile(string draggedFilePath)
        {
            // this line is NOT used because .xml.gz (extensions with two dots) mess up with Path.GetExtension
            //var theExtension = Path.GetExtension(draggedFilePath).ToLowerInvariant();

            // we need to get the filename before parsing out the extension because if we assume that everything after the dot
            // is the extension and there are dots in the file path (i.e. in a folder name), this will mess up
            var filename = Path.GetFileName(draggedFilePath);
            var theExtension = filename.Split('.').Last().ToLowerInvariant();

            switch (theExtension)
            {
                case "raw":
                case "mzml":
                    RawDataForDataGrid zz = new RawDataForDataGrid(draggedFilePath);
                    if (!SpectraFileExists(spectraFilesObservableCollection, zz)) { spectraFilesObservableCollection.Add(zz); }
                    break;
                case "pep.XML":
                case "pep.xml":
                    break;
                case "psmtsv":
                case "tsv":
                case "txt":
                case "csv":
                    RawDataForDataGrid resultFileDataGrid = new RawDataForDataGrid(draggedFilePath);
                    if (!SpectraFileExists(resultFilesObservableCollection, resultFileDataGrid)) { resultFilesObservableCollection.Add(resultFileDataGrid); }
                    break;
                default:
                    break;
            }
        }

        private bool SpectraFileExists(ObservableCollection<RawDataForDataGrid> rDOC, RawDataForDataGrid zzz)
        {
            foreach (RawDataForDataGrid rdoc in rDOC)
                if (rdoc.FileName == zzz.FileName) { return true; }
            return false;
        }

        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            resultFilesObservableCollection.Clear();

            spectraFilesObservableCollection.Clear();

            spectrumNumsObservableCollection.Clear();

            thanos.deconvolutor.Model = MainViewModel.ResetViewModel();
        }
        
        #endregion

        private void btnLoadData_Click(object sender, RoutedEventArgs e)
        {
            var spectraFilePath = spectraFilesObservableCollection.First().FilePath;
            if (spectraFilePath == null)
            {
                MessageBox.Show("Please add a spectra file.");
                return;
            }

            foreach (var spectrafileGrid in spectraFilesObservableCollection)
            {
                var aSpectraFilePath = spectrafileGrid.FilePath;
                if (aSpectraFilePath == null)
                {
                    continue;
                }

                thanos.MsDataFilePaths.Add(aSpectraFilePath);
            }

            // load the spectra file
            (sender as Button).IsEnabled = false;
            btnAddFiles.IsEnabled = false;
            btnClearFiles.IsEnabled = false;
            thanos.msDataFile = thanos.spectraFileManager.LoadFile(spectraFilePath, new CommonParameters(trimMs1Peaks:false, trimMsMsPeaks:false));
            thanos.msDataScans = thanos.msDataFile.GetAllScansList();

            foreach (var iScan in thanos.msDataScans)
            {
                allScansObservableCollection.Add(new AllScansForDataGrid(iScan.OneBasedScanNumber, iScan.OneBasedPrecursorScanNumber,iScan.RetentionTime, iScan.MsnOrder, iScan.IsolationMz));
            }
        }

        private void BtnLoadResults_Click(object sender, RoutedEventArgs e)
        {
            resultsFilePath = resultFilesObservableCollection.First().FilePath;
            if (resultsFilePath == null)
            {
                MessageBox.Show("Please add a result file.");
                return;
            }

            // load the spectra file
            (sender as Button).IsEnabled = false;
            btnAddResultFiles.IsEnabled = false;
            btnClearResultFiles.IsEnabled = false;

            // load the PSMs
            List<string> warnings;

            foreach (var aResultfileGrid in resultFilesObservableCollection)
            {
                var aResultfilePath = aResultfileGrid.FilePath;
                if (aResultfilePath == null)
                {
                    continue;
                }
                thanos.ResultFilePaths.Add(aResultfilePath);

                var psms = TsvReader_Id.ReadTsv(resultsFilePath);
                foreach (var psm in psms)
                {
                    spectrumNumsObservableCollection.Add(new SpectrumForDataGrid(psm.Ms2ScanNumber, 0, psm.PrecursorMass, ""));
                }
                thanos.simplePsms.AddRange(psms);
            }
        }

        //From raw file
        private void DataGridAllScanNums_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            //UpdateField();
            //if (dataGridAllScanNums.SelectedItem == null)
            //{
            //    return;
            //}

            //var sele = (AllScansForDataGrid)dataGridAllScanNums.SelectedItem;

            //thanos.msDataScan = thanos.msDataScans.Where(p => p.OneBasedScanNumber == sele.ScanNum).First();

            //if (TabCrosslink.IsSelected)
            //{

            //}

            //if (TabDecon.IsSelected)
            //{
            //    ResetDataGridAndModel();

            //    if (sele.MsOrder == 2)
            //    {
            //        thanos.msDataScan = thanos.msDataScans.Where(p => p.OneBasedScanNumber == sele.ScanNum).First();
            //        thanos.deconvolutor.Model = MainViewModel.DrawScan(thanos.msDataScan);


            //        FindChargeDecon();
            //    }
            //    else
            //    {
            //        thanos.msDataScan = thanos.msDataScans.Where(p => p.OneBasedScanNumber == sele.ScanNum).First();
            //        thanos.deconvolutor.Model = MainViewModel.DrawScan(thanos.msDataScan);
            //    }

            //    MzSpectrumXY mzSpectrumXY = new MzSpectrumXY(thanos.msDataScan.MassSpectrum.XArray, thanos.msDataScan.MassSpectrum.YArray, true);
            //    thanos.deconvolutor.IsotopicEnvelopes = IsoDecon.MsDeconv_Deconvolute(mzSpectrumXY, thanos.msDataScan.ScanWindowRange, thanos.DeconvolutionParameter).ToList();


            //    int i = 1;
            //    foreach (var item in thanos.deconvolutor.IsotopicEnvelopes)
            //    {
            //        thanos.deconvolutor.IsotopicEnvelopes[i - 1].ScanNum = thanos.msDataScan.OneBasedScanNumber;
            //        thanos.deconvolutor.IsotopicEnvelopes[i - 1].RT = thanos.msDataScan.RetentionTime;
            //        thanos.deconvolutor.envolopCollection.Add(new EnvolopForDataGrid(i, item.HasPartner, item.ExperimentIsoEnvelop.First().Mz, item.Charge, item.MonoisotopicMass, item.TotalIntensity, item.IntensityRatio, item.MsDeconvScore, item.MsDeconvSignificance));
            //        i++;
            //    }

            //    if (thanos.msDataScan.MsnOrder == 1)
            //    {
            //        //double max = thanos.deconvolutor.mzSpectrumXY.YArray.Max();
            //        //int indexMax = thanos.deconvolutor.mzSpectrumXY.YArray.ToList().IndexOf(max);


            //        //thanos.deconvolutor.ChargeEnvelops = ChargeDecon.FindChargesForScan(thanos.deconvolutor.mzSpectrumXY, thanos.DeconvolutionParameter);
            //        //thanos.deconvolutor.ChargeEnvelops = ChargeDecon.QuickFindChargesForScan(thanos.deconvolutor.mzSpectrumXY, thanos.DeconvolutionParameter);
            //        List<IsoEnvelop> isoEnvelops;
            //        //thanos.deconvolutor.ChargeEnvelops = ChargeDecon.QuickChargeDeconForScan(thanos.deconvolutor.mzSpectrumXY, thanos.DeconvolutionParameter, out isoEnvelops);
            //        thanos.deconvolutor.ChargeEnvelops = ChargeDecon.ChargeDeconIsoForScan(thanos.deconvolutor.mzSpectrumXY, thanos.DeconvolutionParameter, out isoEnvelops);


            //        int ind = 1;
            //        foreach (var chargeEnvelop in thanos.deconvolutor.ChargeEnvelops)
            //        {
            //            thanos.deconvolutor.chargeEnvelopesCollection.Add(new ChargeEnvelopesForDataGrid(ind, chargeEnvelop.MonoMass, chargeEnvelop.UnUsedMzsRatio, chargeEnvelop.IsoEnveNum, chargeEnvelop.ChargeDeconScore, chargeEnvelop.IntensityRatio, chargeEnvelop.mzs_box));
            //            ind++;
            //        }
            //    }

            //}
        }

        //From result file
        private void DataGridScanNums_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            //UpdateField();
            if (dataGridPsms.SelectedItem == null)
            {
                return;
            }

            //ResetDataGridAndModel();

            var sele = (SpectrumForDataGrid)dataGridPsms.SelectedItem;

            if (TabCrosslink.IsSelected && thanos.msDataScans.Where(p => p.OneBasedScanNumber == sele.ScanNum).Count() > 0)
            {
                thanos.msDataScan = thanos.msDataScans.Where(p => p.OneBasedScanNumber == sele.ScanNum).First();
                var selePsm = thanos.psms.Where(p => p.Ms2ScanNumber == sele.ScanNum).First();
                thanos.crosslinkHandler.CrosslinkModel = PsmAnnotationViewModel.DrawScanMatch(thanos.msDataScan, selePsm.MatchedIons, selePsm.BetaPeptideMatchedIons);
            }

            //if (TabDecon.IsSelected)
            //{
            //    //var ms2DataScan = thanos.msDataScans.Where(p => p.OneBasedScanNumber == sele.ScanNum).First();
            //    //var psm = thanos.psms.Where(p => p.Ms2ScanNumber == sele.ScanNum).First();
            //    //thanos.deconvolutor.Model = MainViewModel.DrawPeptideSpectralMatch(ms2DataScan, psm);
            //    //thanos.msDataScan = thanos.msDataScans.Where(p => p.OneBasedScanNumber == sele.PrecursorScanNum).First();

            //    //thanos.deconvolutor.IsotopicEnvelopes = IsoDecon.MsDeconv_Deconvolute(thanos.deconvolutor.mzSpectrumXY, thanos.msDataScan.ScanWindowRange, thanos.DeconvolutionParameter).OrderBy(p => p.MonoisotopicMass).ToList();

            //    //int i = 1;
            //    //foreach (var item in thanos.deconvolutor.IsotopicEnvelopes)
            //    //{
            //    //    thanos.deconvolutor.envolopCollection.Add(new EnvolopForDataGrid(i, item.HasPartner, item.ExperimentIsoEnvelop.First().Mz, item.Charge, item.MonoisotopicMass, item.TotalIntensity, item.IntensityRatio, item.MsDeconvScore, item.MsDeconvSignificance));
            //    //    i++;
            //    //}


            //    ////thanos.deconvolutor.ScanChargeEnvelopes = thanos.deconvolutor.mzSpectrumBU.ChargeDeconvolution(thanos.deconvolutor.IsotopicEnvelopes);
            //    ////int ind = 1;
            //    ////foreach (var theScanChargeEvelope in thanos.deconvolutor.ScanChargeEnvelopes)
            //    ////{
            //    ////    thanos.deconvolutor.chargeEnvelopesCollection.Add(new ChargeEnvelopesForDataGrid(ind, theScanChargeEvelope.isotopicMass, theScanChargeEvelope.MSE));
            //    ////    ind++;
            //    ////}

            //    //double max = thanos.deconvolutor.mzSpectrumXY.YArray.Max();
            //    //int indexMax = thanos.deconvolutor.mzSpectrumXY.YArray.ToList().IndexOf(max);

            //    //thanos.deconvolutor.Mz_zs = ChargeDecon.FindChargesForPeak(thanos.deconvolutor.mzSpectrumXY, indexMax, thanos.DeconvolutionParameter);
            //    //int ind = 1;
            //    //foreach (var mz_z in thanos.deconvolutor.Mz_zs)
            //    //{
            //    //    thanos.deconvolutor.chargeEnvelopesCollection.Add(new ChargeEnvelopesForDataGrid(ind, mz_z.mz.ToMass(mz_z.charge), mz_z.intensity, 0, 0, 0, null));
            //    //    ind++;
            //    //}
            //}

        }     

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            UpdateField();
            Skill skill = ((Skill)cmbAction.SelectedIndex);

            switch (skill)
            {
                case Skill.accumulate_intensity:
                    action = thanos.Accumulate;
                    break;
                case Skill.merge_boxcarScan:
                    action = thanos.MergeBoxCarScan;
                    break;

                case Skill.sweet_pGlcoResult:
                    action = thanos.WritePGlycoResult;
                    break;
                case Skill.plot_glycoFamilcy:
                    action = thanos.PlotGlycoFamily;
                    break;
                case Skill.account_ScanInfo:
                    action = thanos.ExtractScanInfo;
                    break;
                case Skill.sweetor_glycoFamily:
                    action = thanos.BuildGlycoFamily;
                    break;
                case Skill.account_ExtractPrecursorInfo:
                    action = thanos.ExtractPrecursorInfo;
                    break;
                case Skill.acount_ExtractPrecursorInfo_Decon:
                    action = thanos.ExtractPrecursorInfo_Decon;
                    break;
                case Skill.fixPrecursor_BoxCarScan:
                    action = thanos.FixPrecursorBoxCarScan;
                    break;
                default:
                    break;
            }

            if (action != null)
            {
                action();
            }
        }

    }
}
