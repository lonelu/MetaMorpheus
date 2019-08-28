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

namespace MetaDrawGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        //Data file and Result file Data Gid
        private readonly ObservableCollection<RawDataForDataGrid> spectraFilesObservableCollection = new ObservableCollection<RawDataForDataGrid>();
        private readonly ObservableCollection<RawDataForDataGrid> resultFilesObservableCollection = new ObservableCollection<RawDataForDataGrid>();

        //All Scan Data Grid
        private readonly ObservableCollection<AllScansForDataGrid> allScansObservableCollection = new ObservableCollection<AllScansForDataGrid>();
        private readonly ObservableCollection<SpectrumForDataGrid> spectrumNumsObservableCollection = new ObservableCollection<SpectrumForDataGrid>();
    
        private string resultsFilePath;            

        private List<PsmFromTsv> psms = new List<PsmFromTsv>();
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

            plotView.DataContext = thanos.deconvolutor;

            plotViewDecon.DataContext = thanos.deconvolutor;

            plotViewXIC.DataContext = thanos.deconvolutor;

            thanos.psmAnnotationViewModel = new PsmAnnotationViewModel();

            plotAnnoView.DataContext = thanos;

            dataGridMassSpectraFiles.DataContext = spectraFilesObservableCollection;

            dataGridResultFiles.DataContext = resultFilesObservableCollection;

            dataGridPsms.DataContext = spectrumNumsObservableCollection;

            //dataGridPsms.DataContext = thanos.deconvolutor;

            dataGridDeconNums.DataContext = thanos.deconvolutor;

            dataGridChargeEnves.DataContext = thanos.deconvolutor;

            dataGridAllScanNums.DataContext = allScansObservableCollection;

            dataGridGlycoResultFiles.DataContext = resultFilesObservableCollection;

            dataGridGlyco.DataContext = thanos.sweetor;

            dataGridGlyco.DataContext = thanos.sweetor;

            dataGridGlycan.DataContext = thanos.sweetor;

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

            foreach (string aSkill in Enum.GetNames(typeof(DeconvolutorSkill)))
            {
                {
                    cmbDeconAction.Items.Add(aSkill);
                }
            }
        }

        private void UpdatePanel()
        {
            //productMassToleranceTextBox.Text = CommonParameters.ProductMassTolerance.Value.ToString(CultureInfo.InvariantCulture);
            //productMassToleranceComboBox.SelectedIndex = CommonParameters.ProductMassTolerance is AbsoluteTolerance ? 0 : 1;
            txtMinAssumedChargeState.Text = thanos.DeconvolutionParameter.DeconvolutionMinAssumedChargeState.ToString();
            txtMaxAssumedChargeState.Text = thanos.DeconvolutionParameter.DeconvolutionMaxAssumedChargeState.ToString();
            txtDeconvolutionToleranc.Text = thanos.DeconvolutionParameter.DeconvolutionMassTolerance.ToString();
            txtIntensityRatioLimit.Text = thanos.DeconvolutionParameter.DeconvolutionIntensityRatio.ToString();
            TxtPartnerMassDiff.Text = thanos.DeconvolutionParameter.PartnerMassDiff.ToString();
            TxtMaxLabelNum.Text = thanos.DeconvolutionParameter.MaxmiumLabelNumber.ToString();
            TxtPartnerRatio.Text = thanos.DeconvolutionParameter.PartnerPairRatio.ToString();
            TxtScanNumCountRangeLow.Text = thanos.ControlParameter.LCTimeRange.Item1.ToString();
            TxtScanNumCountRangeHigh.Text = thanos.ControlParameter.LCTimeRange.Item2.ToString();
        }

        private void UpdateField()
        {
            thanos.DeconvolutionParameter.DeconvolutionMinAssumedChargeState = int.Parse(txtMinAssumedChargeState.Text);
            thanos.DeconvolutionParameter.DeconvolutionMaxAssumedChargeState = int.Parse(txtMaxAssumedChargeState.Text);
            thanos.DeconvolutionParameter.DeconvolutionMassTolerance = double.Parse(txtDeconvolutionToleranc.Text);
            thanos.DeconvolutionParameter.DeconvolutionIntensityRatio = double.Parse(txtIntensityRatioLimit.Text);
            thanos.DeconvolutionParameter.PartnerMassDiff = double.Parse(TxtPartnerMassDiff.Text);
            thanos.DeconvolutionParameter.MaxmiumLabelNumber = int.Parse(TxtMaxLabelNum.Text);
            thanos.DeconvolutionParameter.PartnerPairRatio = double.Parse(TxtPartnerRatio.Text);

            thanos.ControlParameter.LCTimeRange = new Tuple<double, double>(  double.Parse(TxtScanNumCountRangeLow.Text), double.Parse(TxtScanNumCountRangeHigh.Text));
            thanos.ControlParameter.deconScanNum = Convert.ToInt32(txtDeconScanNum.Text);
            thanos.ControlParameter.modelStartNum = Convert.ToInt32(TxtDeconModel.Text);
            thanos.ControlParameter.DeconChargeMass = double.Parse(TxtDeconChargeMass.Text);
        }

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

        private void AddAFile(string draggedFilePath)
        {
            // this line is NOT used because .xml.gz (extensions with two dots) mess up with Path.GetExtension
            //var theExtension = Path.GetExtension(draggedFilePath).ToLowerInvariant();

            // we need to get the filename before parsing out the extension because if we assume that everything after the dot
            // is the extension and there are dots in the file path (i.e. in a folder name), this will mess up
            var filename = Path.GetFileName(draggedFilePath);
            var theExtension = filename.Substring(filename.IndexOf(".")).ToLowerInvariant();

            switch (theExtension)
            {
                case ".raw":
                case ".mzml":
                    RawDataForDataGrid zz = new RawDataForDataGrid(draggedFilePath);
                    if (!SpectraFileExists(spectraFilesObservableCollection, zz)) { spectraFilesObservableCollection.Add(zz); }
                    break;
                case ".pep.XML":
                case ".pep.xml":
                    break;
                case ".psmtsv":
                case ".tsv":
                case ".txt":
                case ".csv":
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
    
        private void btnLoadData_Click(object sender, RoutedEventArgs e)
        {
            var spectraFilePath = spectraFilesObservableCollection.First().FilePath;
            if (spectraFilePath == null)
            {
                MessageBox.Show("Please add a spectra file.");
                return;
            }

            // load the spectra file
            (sender as Button).IsEnabled = false;
            btnAddFiles.IsEnabled = false;
            btnClearFiles.IsEnabled = false;
            thanos.msDataFile = thanos.spectraFileManager.LoadFile(spectraFilePath, new CommonParameters());
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
            psms = PsmTsvReader.ReadTsv(resultsFilePath, out warnings);
            foreach (var psm in psms)
            {
                spectrumNumsObservableCollection.Add(new SpectrumForDataGrid(psm.Ms2ScanNumber, psm.PrecursorScanNum, psm.PrecursorMz, psm.OrganismName));
            }
        }

        #region Deconvolution Control

        private void BtnLoadFlashDeconResults_Click(object sender, RoutedEventArgs e)
        {
            foreach (var collection in resultFilesObservableCollection)
            {
                resultsFilePath = collection.FilePath;
                if (resultsFilePath == null)
                {
                    continue;
                }
                // load the PSMs
                thanos.msFeatures.AddRange(TsvReader_MsFeature.ReadTsv(resultsFilePath));
            }

            DataGridFlashDeconEnvelopes.DataContext = thanos.deconvolutor;

            foreach (var feature in thanos.msFeatures)
            {
                thanos.deconvolutor.MsFeatureCollection.Add(new MsFeatureForDataGrid(feature));
            }
        }

        private void btnResetDecon_Click(object sender, RoutedEventArgs e)
        {
            ResetDataGridAndModel();
        }

        private void ResetDataGridAndModel()
        {
            thanos.deconvolutor.envolopCollection.Clear();
            thanos.deconvolutor.DeconModel = DeconViewModel.ResetDeconModel();
            thanos.deconvolutor.Model = MainViewModel.ResetViewModel();
            thanos.deconvolutor.chargeEnvelopesCollection.Clear();
            thanos.deconvolutor.XicModel = PeakViewModel.ResetViewModel();
        }

        //From raw file
        private void DataGridAllScanNums_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            UpdateField();
            if (dataGridAllScanNums.SelectedItem == null)
            {
                return;
            }
            
            var sele = (AllScansForDataGrid)dataGridAllScanNums.SelectedItem;

            thanos.msDataScan = thanos.msDataScans.Where(p => p.OneBasedScanNumber == sele.ScanNum).First();
            thanos.psmAnnotationViewModel.DrawPeptideSpectralMatch(thanos.msDataScan);

            if (TabDecon.IsSelected)
            {
                ResetDataGridAndModel();

                if (sele.MsOrder == 2)
                {
                    var ms2DataScan = thanos.msDataScans.Where(p => p.OneBasedScanNumber == sele.ScanNum).First();
                    thanos.deconvolutor.Model = MainViewModel.DrawScan(ms2DataScan);
                    thanos.msDataScan = thanos.msDataScans.Where(p => p.OneBasedScanNumber == sele.PrecursorScanNum).First();

                }
                else
                {
                    thanos.msDataScan = thanos.msDataScans.Where(p => p.OneBasedScanNumber == sele.ScanNum).First();
                    thanos.deconvolutor.Model = MainViewModel.DrawScan(thanos.msDataScan);
                }

                MzSpectrumBU mzSpectrumBU = new MzSpectrumBU(thanos.msDataScan.MassSpectrum.XArray, thanos.msDataScan.MassSpectrum.YArray, true);
                thanos.deconvolutor.IsotopicEnvelopes = mzSpectrumBU.MsDeconv_Deconvolute(thanos.msDataScan.ScanWindowRange, thanos.DeconvolutionParameter).OrderBy(p => p.MonoisotopicMass).ToList();


                int i = 1;
                foreach (var item in thanos.deconvolutor.IsotopicEnvelopes)
                {
                    thanos.deconvolutor.IsotopicEnvelopes[i - 1].ScanNum = thanos.msDataScan.OneBasedScanNumber;
                    thanos.deconvolutor.IsotopicEnvelopes[i - 1].RT = thanos.msDataScan.RetentionTime;
                    thanos.deconvolutor.envolopCollection.Add(new EnvolopForDataGrid(i, item.HasPartner, item.ExperimentIsoEnvelop.First().Item1, item.Charge, item.MonoisotopicMass, item.ExperimentIsoEnvelop.First().Item2));
                    i++;
                }

                //int ind = 1;
                //foreach (var theScanChargeEvelope in thanos.deconvolutor.ScanChargeEnvelopes)
                //{
                //    thanos.deconvolutor.chargeEnvelopesCollection.Add(new ChargeEnvelopesForDataGrid(ind, theScanChargeEvelope.isotopicMass, theScanChargeEvelope.MSE));
                //    ind++;
                //}

                double max = thanos.deconvolutor.mzSpectrumBU.YArray.Max();
                int indexMax = thanos.deconvolutor.mzSpectrumBU.YArray.ToList().IndexOf(max);

                thanos.deconvolutor.Mz_zs = ChargeDecon.FindChargesForPeak(thanos.deconvolutor.mzSpectrumBU, indexMax);

                //thanos.deconvolutor.Mz_zs_list = ChargeDecon.FindChargesForScan(thanos.deconvolutor.mzSpectrumBU);

                int ind = 1;
                foreach (var mz_z in thanos.deconvolutor.Mz_zs)
                {
                    thanos.deconvolutor.chargeEnvelopesCollection.Add(new ChargeEnvelopesForDataGrid(ind, mz_z.Value.Mz, mz_z.Key, mz_z.Value.Intensity));
                    ind++;
                }
            }
        }

        //From result file
        private void DataGridScanNums_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            UpdateField();
            if (dataGridPsms.SelectedItem == null)
            {
                return;
            }

            ResetDataGridAndModel();

            var sele = (SpectrumForDataGrid)dataGridPsms.SelectedItem;

            var ms2DataScan = thanos.msDataScans.Where(p => p.OneBasedScanNumber == sele.ScanNum).First();
            var psm = psms.Where(p => p.Ms2ScanNumber == sele.ScanNum).First();
            thanos.deconvolutor.Model = MainViewModel.DrawPeptideSpectralMatch(ms2DataScan, psm);
            thanos.msDataScan = thanos.msDataScans.Where(p => p.OneBasedScanNumber == sele.PrecursorScanNum).First();

            thanos.deconvolutor.IsotopicEnvelopes = thanos.deconvolutor.mzSpectrumBU.MsDeconv_Deconvolute(thanos.msDataScan.ScanWindowRange, thanos.DeconvolutionParameter).OrderBy(p => p.MonoisotopicMass).ToList();

            int i = 1;
            foreach (var item in thanos.deconvolutor.IsotopicEnvelopes)
            {
                thanos.deconvolutor.envolopCollection.Add(new EnvolopForDataGrid(i, item.HasPartner, item.ExperimentIsoEnvelop.First().mz, item.Charge, item.MonoisotopicMass, item.TotalIntensity));
                i++;
            }


            //thanos.deconvolutor.ScanChargeEnvelopes = thanos.deconvolutor.mzSpectrumBU.ChargeDeconvolution(thanos.deconvolutor.IsotopicEnvelopes);
            //int ind = 1;
            //foreach (var theScanChargeEvelope in thanos.deconvolutor.ScanChargeEnvelopes)
            //{
            //    thanos.deconvolutor.chargeEnvelopesCollection.Add(new ChargeEnvelopesForDataGrid(ind, theScanChargeEvelope.isotopicMass, theScanChargeEvelope.MSE));
            //    ind++;
            //}

            double max = thanos.deconvolutor.mzSpectrumBU.YArray.Max();
            int indexMax = thanos.deconvolutor.mzSpectrumBU.YArray.ToList().IndexOf(max);

            thanos.deconvolutor.Mz_zs = ChargeDecon.FindChargesForPeak(thanos.deconvolutor.mzSpectrumBU, indexMax);
            int ind = 1;
            foreach (var mz_z in thanos.deconvolutor.Mz_zs)
            {
                thanos.deconvolutor.chargeEnvelopesCollection.Add(new ChargeEnvelopesForDataGrid(ind, mz_z.Value.Mz, mz_z.Key, mz_z.Value.Intensity));
                ind++;
            }
        }

        //From FlashDecon
        private void DataGridFlashDeconEnvelopes_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            UpdateField();
            if (DataGridFlashDeconEnvelopes.SelectedItem == null)
            {
                return;
            }

            ResetDataGridAndModel();

            var sele = (MsFeatureForDataGrid)DataGridFlashDeconEnvelopes.SelectedItem;

            thanos.msDataScan = thanos.msDataScans.Where(p => p.OneBasedScanNumber == sele.ScanNum).First();
            thanos.deconvolutor.Model = MainViewModel.DrawScan(thanos.msDataScan, sele.MsFeature);
            
        }

        //From Decon isoEnvelop
        private void DataGridDeconNums_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            UpdateField();
            if (dataGridDeconNums.SelectedItem == null)
            {
                return;
            }
            var sele = (EnvolopForDataGrid)dataGridDeconNums.SelectedItem;

            var envo = thanos.deconvolutor.IsotopicEnvelopes[sele.Ind - 1];
            thanos.deconvolutor.DeconModel = DeconViewModel.UpdataModelForDecon(thanos.msDataScan, envo);
            thanos.deconvolutor.XicModel = PeakViewModel.DrawXic(envo.MonoisotopicMass, envo.Charge, thanos.msDataScan.RetentionTime, thanos.msDataFile, new PpmTolerance(5), 5.0, 3, "");
        }

        //From Charge Decon chargeEnvelop
        private void DataGridChargeEnves_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            UpdateField();
            if (dataGridChargeEnves.SelectedItem == null)
            {
                return;
            }
            var sele = (ChargeEnvelopesForDataGrid)dataGridChargeEnves.SelectedItem;
            
            thanos.deconvolutor.Model = ChargeEnveViewModel.UpdataModelForChargeEnve(thanos.msDataScan, thanos.deconvolutor.Mz_zs);
        }

        #endregion

        #region Glyco Control

        private void BtnDrawGlycan_Click(object sender, RoutedEventArgs e)
        {
            glyCanvas.Children.Clear();

            if (TxtGlycans.Text != null)
            {
                GlycanStructureAnnotation.DrawGlycan(glyCanvas, TxtGlycans.Text, 50);
            }
        }

        private void BtnAddGlycoResultFiles_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog1 = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Result Files(*.csv;*.psmtsv;*.txt)|*.csv;*.psmtsv;*.txt",
                FilterIndex = 1,
                RestoreDirectory = true,
                Multiselect = true
            };
            if (openFileDialog1.ShowDialog() == true)
                foreach (var rawDataFromSelected in openFileDialog1.FileNames.OrderBy(p => p))
                {
                    AddAFile(rawDataFromSelected);
                }
            dataGridGlycoResultFiles.Items.Refresh();
        }

        private void BtnClearGlycoResultFiles_Click(object sender, RoutedEventArgs e)
        {
            thanos.sweetor.GlycoResultCollection.Clear();
        }

        private void BtnLoadGlycoResults_Click(object sender, RoutedEventArgs e)
        {
            // load the spectra file
            (sender as Button).IsEnabled = false;
            BtnAddGlycoResultFiles.IsEnabled = false;
            btnClearGlycoResultFiles.IsEnabled = false;

            foreach (var collection in resultFilesObservableCollection)
            {
                resultsFilePath = collection.FilePath;
                if (resultsFilePath == null)
                {
                    continue;
                }
                // load the PSMs
                thanos.simplePsms.AddRange(TsvReader_Glyco.ReadTsv(resultsFilePath));
            }

            foreach (var psm in thanos.simplePsms)
            {
                thanos.sweetor.GlycoStrucureCollection.Add(new GlycoStructureForDataGrid( psm.ScanNum));
            }
        }

        private void BtnLoadMsFeatureResults_Click(object sender, RoutedEventArgs e)
        {
            foreach (var collection in resultFilesObservableCollection)
            {
                resultsFilePath = collection.FilePath;
                if (resultsFilePath == null)
                {
                    continue;
                }
                // load the PSMs
                thanos.msFeatures.AddRange(TsvReader_MsFeature.ReadTsv(resultsFilePath));
            }

            foreach (var feature in thanos.msFeatures)
            {
                thanos.sweetor.MsFeatureCollection.Add(new MsFeatureForDataGrid(feature));
            }
        }

        private void DataGridGlyco_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {       
            if (dataGridGlyco.SelectedItem == null)
            {
                return;
            }

            var sele = (GlycoStructureForDataGrid)dataGridGlyco.SelectedItem;
            thanos.msDataScan = thanos.msDataScans.Where(p => p.OneBasedScanNumber == sele.ScanNum).First();
            var selePsm = thanos.simplePsms.Where(p => p.ScanNum == sele.ScanNum).First();
            selePsm.MatchedIons = SimplePsm.GetMatchedIons(selePsm.glycoPwsm, selePsm.PrecursorMass, selePsm.ChargeState, thanos.CommonParameters, thanos.msDataScan);
            thanos.psmAnnotationViewModel.DrawPeptideSpectralMatch(thanos.msDataScan, selePsm);

            //Draw Glycan
            glyCanvas.Children.Clear();          
            GlycanStructureAnnotation.DrawGlycan(glyCanvas, sele.Structure, 50);
        }

        private void BtnLoadGlycans_Click(object sender, RoutedEventArgs e)
        {
            thanos.sweetor.NGlycans = Glycan.LoadGlycan(GlobalVariables.NGlycanLocation).ToList();
            foreach (var glycan in thanos.sweetor.NGlycans)
            {
                thanos.sweetor.glycanDatabaseCollection.Add(new GlycanDatabaseForDataGrid(glycan.GlyId, Glycan.GetKindString(glycan.Kind), glycan.Struc));
            }
        }

        private void BtnSearchGlycan_Click(object sender, RoutedEventArgs e)
        {
            if (TxtGlycanKind.Text != null)
            {
                var x = thanos.sweetor.NGlycans.Where(p => Glycan.GetKindString(p.Kind) == TxtGlycanKind.Text).ToList();

                if (x.Count > 0)
                {
                    thanos.sweetor.glycanDatabaseCollection.Clear();
                    foreach (var glycan in x)
                    {
                        thanos.sweetor.glycanDatabaseCollection.Add(new GlycanDatabaseForDataGrid(glycan.GlyId, Glycan.GetKindString(glycan.Kind), glycan.Struc));
                    }
                }
                
            }
        }

        private void DataGridGlycan_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            if (dataGridGlycan.SelectedItem == null)
            {
                return;
            }

            var sele = (GlycanDatabaseForDataGrid)dataGridGlycan.SelectedItem;
            glyCanvasLeft.Children.Clear();
            GlycanStructureAnnotation.DrawGlycan(glyCanvasLeft, sele.Structure, 50);
        }

        #endregion

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
                case Skill.account_ScanInfo_BoxCar:
                    action = thanos.ExtractScanInfo_BoxCar;
                    break;
                case Skill.sweet_pGlcoResult:
                    action = thanos.WritePGlycoResult;
                    break;
                case Skill.plot_glycoFamilcy:
                    action = thanos.PlotGlycoFamily;
                    break;
                case Skill.account_ScanInfo_Shotgun:
                    action = thanos.ExtractScanInfo_Shotgun;
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
                default:
                    break;
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

            foreach (var aResultfileGrid in resultFilesObservableCollection)
            {
                var aResultfilePath = aResultfileGrid.FilePath;
                if (aResultfilePath == null)
                {
                    continue;
                }
                thanos.ResultFilePaths.Add(aResultfilePath);
            }

            if (action != null)
            {
                action();
            }
        }

        private void BtnDeconStart_Click(object sender, RoutedEventArgs e)
        {
            UpdateField();

            DeconvolutorSkill deconvolutorSkills = ((DeconvolutorSkill)cmbDeconAction.SelectedIndex);

            switch (deconvolutorSkills)
            {
                case DeconvolutorSkill.DeconSeleScan:
                    action = thanos.deconvolutor.Decon;
                    break;
                case DeconvolutorSkill.PlotAvaragineModel:
                    action = thanos.deconvolutor.PlotDeconModel;
                    break;
                case DeconvolutorSkill.DeconQuant:
                    action = thanos.deconvolutor.DeconQuant;
                    break;
                case DeconvolutorSkill.DeconAllChargeParsi:
                    action = thanos.deconvolutor.DeconAllChargeParsi;
                    break;
                case DeconvolutorSkill.DeconPeak_Neucode:
                    action = thanos.deconvolutor.DeconPeak_NeuCode;
                    break;
                case DeconvolutorSkill.DeconChargeByPeak:
                    action = thanos.deconvolutor.DeconChargeByPeak;
                    break;
                case DeconvolutorSkill.DeconDrawTwoScan:
                    action = thanos.deconvolutor.PlotTwoScan;
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
