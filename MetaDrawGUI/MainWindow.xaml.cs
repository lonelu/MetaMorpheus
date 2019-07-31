﻿using System;
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


        //File path and file manage
        private string spectraFilePath;
        private MyFileManager spectraFileManager = new MyFileManager(true);
        private MsDataFile MsDataFile = null;
        private string resultsFilePath;
               
        private CommonParameters CommonParameters = new CommonParameters();
        private DeconvolutionParameter DeconvolutionParameter = new DeconvolutionParameter();


        private List<PsmFromTsv> psms = new List<PsmFromTsv>();


        public Thanos thanos = new Thanos();
        public Action action { get; set; }

        //Glyco
        private readonly ObservableCollection<RawDataForDataGrid> GlycoResultObservableCollection = new ObservableCollection<RawDataForDataGrid>();

        private readonly ObservableCollection<GlycoStructureForDataGrid> GlycoStrucureObservableCollection = new ObservableCollection<GlycoStructureForDataGrid>();

        private readonly ObservableCollection<MsFeatureForDataGrid> MsFeatureObservableCollection = new ObservableCollection<MsFeatureForDataGrid>();

        private readonly ObservableCollection<GlycanDatabaseForDataGrid> glycanDatabaseCollection = new ObservableCollection<GlycanDatabaseForDataGrid>();
        private List<Glycan> NGlycans { get; set; }
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

            thanos.deconvolutor.mainViewModel = new MainViewModel();

            plotView.DataContext = thanos.deconvolutor;

            plotViewDecon.DataContext = thanos.deconvolutor;

            plotViewXIC.DataContext = thanos.deconvolutor;

            thanos.deconvolutor.chargeDeconViewModel = new ChargeEnveViewModel();

            plotViewChargeEnve.DataContext = thanos.deconvolutor;

            thanos.psmAnnotationViewModel = new PsmAnnotationViewModel();

            plotAnnoView.DataContext = thanos;

            dataGridMassSpectraFiles.DataContext = spectraFilesObservableCollection;

            dataGridResultFiles.DataContext = resultFilesObservableCollection;

            dataGridPsms.DataContext = spectrumNumsObservableCollection;

            dataGridDeconNums.DataContext = thanos.deconvolutor;

            dataGridChargeEnves.DataContext = thanos.deconvolutor;

            dataGridAllScanNums.DataContext = allScansObservableCollection;

            dataGridMutiproteaseCrosslink.DataContext = resultFilesObservableCollection;

            Title = "MetaDraw" + GlobalVariables.MetaMorpheusVersion;

            //CommonParameters = new CommonParameters();
            productMassToleranceComboBox.Items.Add("Da");
            productMassToleranceComboBox.Items.Add("ppm");
            UpdateFieldsFromPanel();

            dataGridGlycoResultFiles.DataContext = resultFilesObservableCollection;
            //dataGridGlyco.DataContext = GlycoStrucureObservableCollection;
            dataGridGlyco.DataContext = MsFeatureObservableCollection;
            dataGridGlycan.DataContext = glycanDatabaseCollection;
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

            thanos.deconvolutor.mainViewModel = new MainViewModel();
        }

        private void productMassToleranceTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (productMassToleranceComboBox.SelectedIndex == 0)
            {
                CommonParameters.ProductMassTolerance = new AbsoluteTolerance(double.Parse(productMassToleranceTextBox.Text, CultureInfo.InvariantCulture));
            }
            else
            {
                CommonParameters.ProductMassTolerance = new PpmTolerance(double.Parse(productMassToleranceTextBox.Text, CultureInfo.InvariantCulture));
            }
        }

        private void UpdateFieldsFromPanel()
        {
            //productMassToleranceTextBox.Text = CommonParameters.ProductMassTolerance.Value.ToString(CultureInfo.InvariantCulture);
            //productMassToleranceComboBox.SelectedIndex = CommonParameters.ProductMassTolerance is AbsoluteTolerance ? 0 : 1;
            txtMinAssumedChargeState.Text = DeconvolutionParameter.DeconvolutionMinAssumedChargeState.ToString();
            txtMaxAssumedChargeState.Text = DeconvolutionParameter.DeconvolutionMaxAssumedChargeState.ToString();
            txtDeconvolutionToleranc.Text = DeconvolutionParameter.DeconvolutionMassTolerance.ToString();
            txtIntensityRatioLimit.Text = DeconvolutionParameter.DeconvolutionIntensityRatio.ToString();
            TxtNeuCodeMassDefect.Text = DeconvolutionParameter.NeuCodeMassDefect.ToString();
            TxtNeuCodeMaxNum.Text = DeconvolutionParameter.MaxmiumNeuCodeNumber.ToString();
        }
    
        private void btnLoadData_Click(object sender, RoutedEventArgs e)
        {
            spectraFilePath = spectraFilesObservableCollection.First().FilePath;
            if (spectraFilePath == null)
            {
                MessageBox.Show("Please add a spectra file.");
                return;
            }

            // load the spectra file
            (sender as Button).IsEnabled = false;
            btnAddFiles.IsEnabled = false;
            btnClearFiles.IsEnabled = false;
            MsDataFile = spectraFileManager.LoadFile(spectraFilePath, new CommonParameters());
            thanos.msDataScans = MsDataFile.GetAllScansList();

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
            //TO DO: There is a bug with LoadPsms
            //LoadPsms(resultsFilePath);
            List<string> warnings;
            psms = PsmTsvReader.ReadTsv(resultsFilePath, out warnings);
            foreach (var psm in psms)
            {
                spectrumNumsObservableCollection.Add(new SpectrumForDataGrid(psm.Ms2ScanNumber, psm.PrecursorScanNum, psm.PrecursorMz, psm.OrganismName));
            }
        }

        private void LoadPsms(string filename)
        {
            string fileNameWithExtension = Path.GetFileName(resultsFilePath);
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(resultsFilePath);

            try
            {
                List<string> warnings; 
                foreach (var psm in PsmTsvReader.ReadTsv(filename, out warnings))
                {
                    if (psm.Filename == fileNameWithExtension || psm.Filename == fileNameWithoutExtension || psm.Filename.Contains(fileNameWithoutExtension))
                    {
                        psms.Add(psm);
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Could not open PSM file:\n" + e.Message);
            }
        }

        //Calculate Deconvolution time of each scan of all scans
        private void BtnDeconWatch_Click(object sender, RoutedEventArgs e)
        {
            var MS1Scans = thanos.msDataScans.Where(p => p.MsnOrder == 1).ToList();
            List<WatchEvaluation> evalution = new List<WatchEvaluation>();
            int i = 0;
            while (i < MS1Scans.Count)
            {
                var theScanNum = MS1Scans[i].OneBasedScanNumber;
                var theRT = MS1Scans[i].RetentionTime;
                MzSpectrumBU mzSpectrumBU = new MzSpectrumBU(MS1Scans[i].MassSpectrum.XArray, MS1Scans[i].MassSpectrum.YArray, true);

                var watch = System.Diagnostics.Stopwatch.StartNew();

                var isotopicEnvelopes = mzSpectrumBU.Deconvolute(MS1Scans[i].ScanWindowRange, DeconvolutionParameter).OrderBy(p => p.monoisotopicMass).ToList();
                watch.Stop();

                var watch0 = System.Diagnostics.Stopwatch.StartNew();

                var isotopicEnvelopesByParallel = mzSpectrumBU.ParallelDeconvolute(MS1Scans[i].ScanWindowRange, DeconvolutionParameter, 8).OrderBy(p => p.monoisotopicMass).ToList();
                watch0.Stop();

                var watch1 = System.Diagnostics.Stopwatch.StartNew();

                var chargeDecon = mzSpectrumBU.ChargeDeconvolution(MS1Scans[i].OneBasedScanNumber, MS1Scans[i].RetentionTime, isotopicEnvelopes, new List<double?>());

                watch1.Stop();

                var theEvaluation = new WatchEvaluation(theScanNum, theRT, watch.ElapsedMilliseconds, watch0.ElapsedMilliseconds, watch1.ElapsedMilliseconds);
                evalution.Add(theEvaluation);
                i++;

            }

            var writtenFile = Path.Combine(Path.GetDirectoryName(spectraFilesObservableCollection.First().FilePath), "watches.mytsv");
            using (StreamWriter output = new StreamWriter(writtenFile))
            {
                output.WriteLine("ScanNum\tRT\tIsotopicDecon\tIsoTopicDeconByParallel\tChargeDecon");
                foreach (var theEvaluation in evalution)
                {
                    output.WriteLine(theEvaluation.TheScanNumber.ToString() + "\t" + theEvaluation.TheRT + "\t" + theEvaluation.WatchIsoDecon.ToString() + "\t" + theEvaluation.WatchIsoDeconByParallel.ToString() + "\t" + theEvaluation.WatchChaDecon.ToString());
                }
            }

        }

        private void btnResetDecon_Click(object sender, RoutedEventArgs e)
        {
            ResetDataGridAndModel();
        }

        private void ResetDataGridAndModel()
        {
            thanos.deconvolutor.envolopObservableCollection.Clear();
            DeconViewModel.ResetDeconModel();
            thanos.deconvolutor.mainViewModel.ResetViewModel();
            thanos.deconvolutor.chargeEnvelopesObservableCollection.Clear();
            thanos.deconvolutor.chargeDeconViewModel.ResetDeconModel();
            PeakViewModel.ResetViewModel();
        }

        private void DataGridAllScanNums_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            if (dataGridAllScanNums.SelectedItem == null)
            {
                return;
            }
            
            var sele = (AllScansForDataGrid)dataGridAllScanNums.SelectedItem;

            thanos.msDataScan = thanos.msDataScans.Where(p => p.OneBasedScanNumber == sele.ScanNum).First();
            thanos.psmAnnotationViewModel.DrawPeptideSpectralMatch(thanos.msDataScan);

            if (TabDecon.IsSelected)
            {
                //ResetDataGridAndModel();

                if (sele.MsOrder == 2)
                {
                    var ms2DataScan = thanos.msDataScans.Where(p => p.OneBasedScanNumber == sele.ScanNum).First();
                    thanos.deconvolutor.mainViewModel.UpdateScanModel(ms2DataScan);
                    thanos.msDataScan = thanos.msDataScans.Where(p => p.OneBasedScanNumber == sele.PrecursorScanNum).First();

                }
                else
                {
                    thanos.msDataScan = thanos.msDataScans.Where(p => p.OneBasedScanNumber == sele.ScanNum).First();
                    thanos.deconvolutor.mainViewModel.UpdateScanModel(thanos.msDataScan);
                }

                MzSpectrumBU mzSpectrumBU = new MzSpectrumBU(thanos.msDataScan.MassSpectrum.XArray, thanos.msDataScan.MassSpectrum.YArray, true);
                thanos.deconvolutor.IsotopicEnvelopes = mzSpectrumBU.Deconvolute(thanos.msDataScan.ScanWindowRange, DeconvolutionParameter).OrderBy(p => p.monoisotopicMass).ToList();


                int i = 1;
                foreach (var item in thanos.deconvolutor.IsotopicEnvelopes)
                {
                    thanos.deconvolutor.IsotopicEnvelopes[i - 1].ScanNum = thanos.msDataScan.OneBasedScanNumber;
                    thanos.deconvolutor.IsotopicEnvelopes[i - 1].RT = thanos.msDataScan.RetentionTime;
                    thanos.deconvolutor.IsotopicEnvelopes[i - 1].ScanTotalIntensity = thanos.msDataScan.TotalIonCurrent;
                    thanos.deconvolutor.envolopObservableCollection.Add(new EnvolopForDataGrid(i, item.IsNeuCode, item.peaks.First().mz, item.charge, item.monoisotopicMass, item.totalIntensity));
                    i++;
                }

                int ind = 1;
                foreach (var theScanChargeEvelope in thanos.deconvolutor.ScanChargeEnvelopes)
                {
                    thanos.deconvolutor.chargeEnvelopesObservableCollection.Add(new ChargeEnvelopesForDataGrid(ind, theScanChargeEvelope.isotopicMass, theScanChargeEvelope.MSE));
                    ind++;
                }
            }
        }

        private void DataGridDeconNums_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            if (dataGridDeconNums.SelectedItem == null)
            {
                return;
            }
            var sele = (EnvolopForDataGrid)dataGridDeconNums.SelectedItem;

            var envo = thanos.deconvolutor.IsotopicEnvelopes[sele.Ind - 1];
            thanos.deconvolutor.DeconModel = DeconViewModel.UpdataModelForDecon(thanos.msDataScan, envo);
            
            thanos.deconvolutor.XicModel = PeakViewModel.DrawXic(envo.monoisotopicMass, envo.charge, thanos.msDataScan.RetentionTime, MsDataFile, new PpmTolerance(5), 5.0, 3, "");
        }

        private void DataGridChargeEnves_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            if (dataGridChargeEnves.SelectedItem == null)
            {
                return;
            }
            var sele = (ChargeEnvelopesForDataGrid)dataGridChargeEnves.SelectedItem;
            UpdateChargeDeconModel(sele.Ind, thanos.msDataScan);
        }

        private void DataGridScanNums_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            if (dataGridPsms.SelectedItem == null)
            {
                return;
            }

            ResetDataGridAndModel();

            var sele = (SpectrumForDataGrid)dataGridPsms.SelectedItem;

            var ms2DataScan = thanos.msDataScans.Where(p => p.OneBasedScanNumber == sele.ScanNum).First();
            var psm = psms.Where(p => p.Ms2ScanNumber == sele.ScanNum).First();
            thanos.deconvolutor.mainViewModel.DrawPeptideSpectralMatch(ms2DataScan, psm);
            thanos.msDataScan = thanos.msDataScans.Where(p => p.OneBasedScanNumber == sele.PrecursorScanNum).First();

            MzSpectrumBU mzSpectrumBU = new MzSpectrumBU(thanos.msDataScan.MassSpectrum.XArray, thanos.msDataScan.MassSpectrum.YArray, true);
            thanos.deconvolutor.IsotopicEnvelopes = mzSpectrumBU.Deconvolute(thanos.msDataScan.ScanWindowRange, DeconvolutionParameter).OrderBy(p => p.monoisotopicMass).ToList();

            int i = 1;
            foreach (var item in thanos.deconvolutor.IsotopicEnvelopes)
            {
                thanos.deconvolutor.envolopObservableCollection.Add(new EnvolopForDataGrid(i, item.IsNeuCode, item.peaks.First().mz, item.charge, item.monoisotopicMass, item.totalIntensity));
                i++;
            }

            int ind = 1;
            foreach (var theScanChargeEvelope in thanos.deconvolutor.ScanChargeEnvelopes)
            {
                thanos.deconvolutor.chargeEnvelopesObservableCollection.Add(new ChargeEnvelopesForDataGrid(ind, theScanChargeEvelope.isotopicMass, theScanChargeEvelope.MSE));
                ind++;
            }
        }

        private void UpdateChargeDeconModel(int ind, MsDataScan msDataScan)
        {
            var envo = thanos.deconvolutor.ScanChargeEnvelopes[ind - 1];
            thanos.deconvolutor.chargeDeconViewModel.UpdataModelForChargeEnve(msDataScan, envo);
        }

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
            GlycoResultObservableCollection.Clear();
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
                GlycoStrucureObservableCollection.Add(new GlycoStructureForDataGrid( psm.ScanNum));
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
                MsFeatureObservableCollection.Add(new MsFeatureForDataGrid(feature.MonoMass, feature.Abundance, feature.ApexRT));
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
            selePsm.MatchedIons = SimplePsm.GetMatchedIons(selePsm.glycoPwsm, selePsm.PrecursorMass, selePsm.ChargeState, CommonParameters, thanos.msDataScan);
            thanos.psmAnnotationViewModel.DrawPeptideSpectralMatch(thanos.msDataScan, selePsm);

            //Draw Glycan
            glyCanvas.Children.Clear();          
            GlycanStructureAnnotation.DrawGlycan(glyCanvas, sele.Structure, 50);
        }

        private void BtnLoadMutiProteaseCrosslink_Click(object sender, RoutedEventArgs e)
        {
            resultsFilePath = resultFilesObservableCollection.First().FilePath;
            if (resultsFilePath == null)
            {
                MessageBox.Show("Please add a result file.");
                return;
            }

            // load the spectra file
            (sender as Button).IsEnabled = false;

            MultiproteaseCrosslink.Read(resultsFilePath);
            
        }

        private void BtnLoadGlycans_Click(object sender, RoutedEventArgs e)
        {
            NGlycans = Glycan.LoadGlycan(GlobalVariables.NGlycanLocation).ToList();
            foreach (var glycan in NGlycans)
            {
                glycanDatabaseCollection.Add(new GlycanDatabaseForDataGrid(glycan.GlyId, Glycan.GetKindString(glycan.Kind), glycan.Struc));
            }
        }

        private void BtnSearchGlycan_Click(object sender, RoutedEventArgs e)
        {
            if (TxtGlycanKind.Text != null)
            {
                var x = NGlycans.Where(p => Glycan.GetKindString(p.Kind) == TxtGlycanKind.Text).ToList();

                if (x.Count > 0)
                {
                    glycanDatabaseCollection.Clear();
                    foreach (var glycan in x)
                    {
                        glycanDatabaseCollection.Add(new GlycanDatabaseForDataGrid(glycan.GlyId, Glycan.GetKindString(glycan.Kind), glycan.Struc));
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

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            Skill skill = ((Skill)cmbAction.SelectedIndex);

            switch (skill)
            {
                case Skill.accumulate_intensity:
                    action = thanos.Accumulate;
                    break;
                case Skill.merge_boxcarScan:
                    action = thanos.MergeBoxCarScan;
                    break;
                case Skill.account_scanInfo:
                    action = thanos.ExtractScanInfor;
                    break;
                case Skill.sweet_pGlcoResult:
                    action = thanos.WritePGlycoResult;
                    break;
                case Skill.plot_glycoFamilcy:
                    action = thanos.PlotGlycoFamily;
                    break;
                case Skill.account_glycoScanInfo:
                    action = thanos.ExtractGlycoScanInfor;
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

            action();
        }

        private void BtnDeconStart_Click(object sender, RoutedEventArgs e)
        {
            thanos.DeconvolutionParameter.DeconvolutionMinAssumedChargeState = int.Parse(txtMinAssumedChargeState.Text);
            thanos.DeconvolutionParameter.DeconvolutionMaxAssumedChargeState = int.Parse(txtMaxAssumedChargeState.Text);
            thanos.DeconvolutionParameter.DeconvolutionMassTolerance = double.Parse(txtDeconvolutionToleranc.Text);
            thanos.DeconvolutionParameter.DeconvolutionIntensityRatio = double.Parse(txtIntensityRatioLimit.Text);
            thanos.DeconvolutionParameter.MaxmiumNeuCodeNumber = int.Parse(TxtNeuCodeMaxNum.Text);
            thanos.DeconvolutionParameter.NeuCodeMassDefect = double.Parse(TxtNeuCodeMassDefect.Text);
            thanos.DeconvolutionParameter.NeuCodePairRatio = int.Parse(TxtNeuCodeRatio.Text);

            thanos.deconvolutor.deconScanNum = Convert.ToInt32(txtDeconScanNum.Text);
            thanos.deconvolutor.modelStartNum = Convert.ToInt32(TxtDeconModel.Text);

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
                default:
                    break;
            }

            action();
        }
    }
}
