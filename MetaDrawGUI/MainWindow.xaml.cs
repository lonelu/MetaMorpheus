﻿using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.IO;
using ViewModels;
using EngineLayer;
using EngineLayer.CrosslinkSearch;
using System.Collections.Generic;
using MzLibUtil;
using System.Text.RegularExpressions;
using MassSpectrometry;
using System.Globalization;
using System.ComponentModel;

namespace MetaDrawGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private readonly ObservableCollection<RawDataForDataGrid> spectraFilesObservableCollection = new ObservableCollection<RawDataForDataGrid>();
        private readonly ObservableCollection<RawDataForDataGrid> resultFilesObservableCollection = new ObservableCollection<RawDataForDataGrid>();
        private MainViewModel mainViewModel;
        private MsDataFile MsDataFile = null;   
        private List<PsmDraw> PSMs = null;
        private readonly ObservableCollection<SpectrumForDataGrid> spectrumNumsObservableCollection = new ObservableCollection<SpectrumForDataGrid>();
        private CommonParameters CommonParameters;

       
        private readonly ObservableCollection<EnvolopForDataGrid> envolopObservableCollection = new ObservableCollection<EnvolopForDataGrid>();
        public List<IsotopicEnvelope> IsotopicEnvelopes { get; set; } = new List<IsotopicEnvelope>();
        public DeconViewModel DeconViewModel { get; set; }
        public List<MsDataScan> msDataScans { get; set; } = null;

        private readonly ObservableCollection<ChargeEnvelopesForDataGrid> chargeEnvelopesObservableCollection = new ObservableCollection<ChargeEnvelopesForDataGrid>();
        public ChargeEnveViewModel ChargeDeconViewModel { get; set; }
        public List<ChargeDeconEnvelope> ScanChargeEnvelopes { get; set; } = new List<ChargeDeconEnvelope>();
        public ChargeEnve0ViewModel ChargeDecon0ViewModel { get; set; }

        private MsDataFileDecon msDataFileDecon = new MsDataFileDecon();

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

            mainViewModel = new MainViewModel();

            plotView.DataContext = mainViewModel;

            DeconViewModel = new DeconViewModel();

            plotViewDecon.DataContext = DeconViewModel;

            ChargeDeconViewModel = new ChargeEnveViewModel();

            plotViewChargeEnve.DataContext = ChargeDeconViewModel;

            ChargeDecon0ViewModel = new ChargeEnve0ViewModel();

            plotViewChargeEnve0.DataContext = ChargeDecon0ViewModel;

            dataGridMassSpectraFiles.DataContext = spectraFilesObservableCollection;

            dataGridResultFiles.DataContext = resultFilesObservableCollection;

            dataGridScanNums.DataContext = spectrumNumsObservableCollection;

            dataGridDeconNums.DataContext = envolopObservableCollection;

            dataGridChargeEnves.DataContext = chargeEnvelopesObservableCollection;

            Title = "MetaDraw: version " + GlobalVariables.MetaMorpheusVersion;

            CommonParameters = new CommonParameters();
            productMassToleranceComboBox.Items.Add("Da");
            productMassToleranceComboBox.Items.Add("ppm");
            UpdateFieldsFromPanel();
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
            btnReset.IsEnabled = true;
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
            btnReset.IsEnabled = true;
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

        /*private void UpdateOutputFolderTextbox()
        {
            if (spectraFilesObservableCollection.Any())
            {
                // if current output folder is blank and there is a spectra file, use the spectra file's path as the output path
                if (string.IsNullOrWhiteSpace(txtBoxOutputFolder.Text))
                {
                    var pathOfFirstSpectraFile = Path.GetDirectoryName(spectraFilesObservableCollection.Where(p => p.Use).First().FilePath);
                    txtBoxOutputFolder.Text = Path.Combine(pathOfFirstSpectraFile, @"$DATETIME");
                }
                // else do nothing (do not override if there is a path already there; might clear user-defined path)
            }
            else
            {
                // no spectra files; clear the output folder from the GUI
                txtBoxOutputFolder.Clear();
            }
        }*/

        private void btnDraw_Click(object sender, RoutedEventArgs e)
        {

            mainViewModel.Model.InvalidatePlot(true);

            int x = Convert.ToInt32(txtScanNum.Text);

            UpdateModel(x);

        }

        private void btnReadResultFile_Click(object sender, RoutedEventArgs e)
        {
            btnReset.IsEnabled = true;

            if (!spectraFilesObservableCollection.Any())
            {
                return;
            }
            
            LoadScans loadScans = new LoadScans(spectraFilesObservableCollection.Where(b => b.Use).First().FilePath,null);

            MsDataFile = loadScans.Run();
            
            btnReadResultFile.IsEnabled = true;

            if (resultFilesObservableCollection.Count == 0)
            {
                MessageBox.Show("Please add result files.");
                return;
            }
            var resultFilePath = resultFilesObservableCollection.Where(b => b.Use).First().FilePath;
            PSMs = TsvResultReader.ReadTsv(resultFilePath);
            foreach (var item in PSMs)
            {
                spectrumNumsObservableCollection.Add(new SpectrumForDataGrid(item.ScanNumber, item.FullSequence));
            }
            dataGridScanNums.Items.Refresh();
            

            btnReadResultFile.IsEnabled = false;
            btnDraw.IsEnabled = true;
        }

        private void Row_DoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {

            if (sender != null)
            {
                try
                {
                    Regex regex = new Regex(@"\d+");
                    int x = Convert.ToInt32(regex.Match(sender.ToString()).Value);
                    UpdateModel(x);

                }
                catch (Exception)
                {
                    MessageBox.Show("Please check the data loaded.");
                }
            }
        }

        private void UpdateModel(int x)
        {
            if (MsDataFile == null)
            {
                MessageBox.Show("Please check the MS data loaded.");
                return;
            }

            var msScanForDraw = MsDataFile.GetAllScansList().Where(p => p.OneBasedScanNumber == x).First();

            PsmDraw psmDraw = PSMs.Where(p => p.ScanNumber == x).First();

            var lp = new List<ProductType>();
            if (CommonParameters.BIons)
            {
                lp.Add(ProductType.BnoB1ions);
            }
            if (CommonParameters.YIons)
            {
                lp.Add(ProductType.Y);
            }
            if (CommonParameters.CIons)
            {
                lp.Add(ProductType.C);
            }
            if (CommonParameters.ZdotIons)
            {
                lp.Add(ProductType.Zdot);
            }

            var pmm = PsmDraw.XlCalculateTotalProductMassesForSingle(psmDraw, lp, false);

            var matchedIonMassesListPositiveIsMatch = new MatchedIonInfo(pmm.ProductMz.Length);

            double pmmScore = PsmCross.XlMatchIons(msScanForDraw, CommonParameters.ProductMassTolerance, pmm.ProductMz, pmm.ProductName, matchedIonMassesListPositiveIsMatch);

            psmDraw.MatchedIonInfo = matchedIonMassesListPositiveIsMatch;

            mainViewModel.UpdateForSingle(msScanForDraw, psmDraw);

        }

        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            btnReset.IsEnabled = false;

            resultFilesObservableCollection.Clear();

            spectraFilesObservableCollection.Clear();

            spectrumNumsObservableCollection.Clear();

            btnReadResultFile.IsEnabled = true;

            mainViewModel = new MainViewModel();
        }

        private void clearText(object sender, RoutedEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            if (tb.Text.Equals("Scan Number"))
                tb.Text = string.Empty;
        }

        private void restoreText(object sender, RoutedEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            if(tb.Text.Equals(string.Empty))
                tb.Text = "Scan Number";
        }

        private void bCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            CommonParameters.BIons = true;
        }

        private void yCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            CommonParameters.YIons = true;
        }

        private void cCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            CommonParameters.CIons = true;
        }

        private void zdotCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            CommonParameters.ZdotIons = true;
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
            bCheckBox.IsChecked = CommonParameters.BIons;
            yCheckBox.IsChecked = CommonParameters.YIons;
            cCheckBox.IsChecked = CommonParameters.CIons;
            zdotCheckBox.IsChecked = CommonParameters.ZdotIons;
            productMassToleranceTextBox.Text = CommonParameters.ProductMassTolerance.Value.ToString(CultureInfo.InvariantCulture);
            productMassToleranceComboBox.SelectedIndex = CommonParameters.ProductMassTolerance is AbsoluteTolerance ? 0 : 1;
        }

        private void btnLoadData_Click(object sender, RoutedEventArgs e)
        {
            if (!spectraFilesObservableCollection.Any())
            {
                return;
            }

            LoadScans loadScans = new LoadScans(spectraFilesObservableCollection.Where(b => b.Use).First().FilePath, null);

            MsDataFile = loadScans.Run();
            msDataScans = MsDataFile.GetAllScansList();
        }

        private void btnDecon_Click(object sender, RoutedEventArgs e)
        {
            int x = Convert.ToInt32(txtDeconScanNum.Text);
            var msDataScan = msDataScans.Where(p => p.OneBasedScanNumber == x).First();
            //IsotopicEnvelopes = msDataScan.MassSpectrum.Deconvolute(msDataScan.ScanWindowRange, 3, 60, 5.0, 3).OrderBy(p => p.monoisotopicMass).ToList();
            MzSpectrumTD mzSpectrumTD = new MzSpectrumTD(msDataScan.MassSpectrum.XArray, msDataScan.MassSpectrum.YArray, true);
            IsotopicEnvelopes = mzSpectrumTD.DeconvoluteTD(msDataScan.ScanWindowRange, 3, 60, 5.0, 3).OrderBy(p => p.monoisotopicMass).ToList();
            int i=1;
            foreach (var item in IsotopicEnvelopes)
            {
                envolopObservableCollection.Add(new EnvolopForDataGrid(i, item.peaks.First().mz, item.charge, item.monoisotopicMass, item.totalIntensity));
                i++;
            }
            mainViewModel.UpdateScanModel(msDataScan);

            ScanChargeEnvelopes = mzSpectrumTD.ChargeDeconvolution(x, msDataScan.RetentionTime, IsotopicEnvelopes, msDataScans.Where(p => p.OneBasedPrecursorScanNumber == x).Select(p=>p.SelectedIonMZ).ToList());
            int ind = 1;
            foreach (var theScanChargeEvelope in ScanChargeEnvelopes)
            {
                chargeEnvelopesObservableCollection.Add(new ChargeEnvelopesForDataGrid(ind, theScanChargeEvelope.isotopicMass, theScanChargeEvelope.MSE));
                ind++;
            }        
        }

        private void UpdateDeconModel(int x, MsDataScan msDataScan)
        {
            var envo = IsotopicEnvelopes[x-1];
            DeconViewModel.UpdataModelForDecon(msDataScan, envo);
            //mainViewModel.UpdateDecon(mainViewModel.Model, envo);
        }

        private void Decon_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender != null)
            {
                try
                {
                    Regex regex = new Regex(@"\d+");
                    int x = Convert.ToInt32(regex.Match(sender.ToString()).Value);
                    var msDataScan = msDataScans.Where(p => p.OneBasedScanNumber == Convert.ToInt32(txtDeconScanNum.Text)).First();
                    UpdateDeconModel(x, msDataScan);

                }
                catch (Exception)
                {
                    MessageBox.Show("Please check the data loaded.");
                }
            }
        }

        private void UpdateChargeDeconModel(int ind, MsDataScan msDataScan)
        {
            var envo = ScanChargeEnvelopes[ind - 1];
            ChargeDeconViewModel.UpdataModelForChargeEnve(msDataScan, envo);
            ChargeDecon0ViewModel.UpdataModelForChargeEnve0(msDataScan, envo);
        }

        private void ChargeEnves_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender != null)
            {
                try
                {
                    Regex regex = new Regex(@"\d+");
                    int x = Convert.ToInt32(regex.Match(sender.ToString()).Value);
                    var msDataScan = msDataScans.Where(p => p.OneBasedScanNumber == Convert.ToInt32(txtDeconScanNum.Text)).First();
                    UpdateChargeDeconModel(x, msDataScan);

                }
                catch (Exception)
                {
                    MessageBox.Show("Please check the data loaded.");
                }
            }
        }

        private void btnResetDecon_Click(object sender, RoutedEventArgs e)
        {         
            envolopObservableCollection.Clear();
            DeconViewModel.ResetDeconModel();
            mainViewModel.ResetViewModel();
            chargeEnvelopesObservableCollection.Clear();
            ChargeDeconViewModel.ResetDeconModel();
            ChargeDecon0ViewModel.ResetDeconModel();
        }

        private void btnDeconAll_Click(object sender, RoutedEventArgs e)
        {
            var chargeDeconPerMS1Scans = msDataFileDecon.ChargeDeconvolutionFile(msDataScans, CommonParameters);

            //List<ChargeParsi> chargeParsis = ChargeParsimony(chargeEnvelopesList, new SingleAbsoluteAroundZeroSearchMode(2), new SingleAbsoluteAroundZeroSearchMode(5));
            List<ChargeParsi> chargeParsis = msDataFileDecon.ChargeParsimony(chargeDeconPerMS1Scans, new SingleAbsoluteAroundZeroSearchMode(2.2), new SingleAbsoluteAroundZeroSearchMode(5));

            var total = msDataScans.Where(p => p.MsnOrder == 2).Count();
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
            msDataFileDecon.ChargeDeconWriteToTSV(chargeDeconPerMS1Scans, Path.GetDirectoryName(spectraFilesObservableCollection.First().FilePath), "ChargeDecon");
        }

        private void BtnDeconTest_Click(object sender, RoutedEventArgs e)
        {
            var MS1Scans = msDataScans.Where(p => p.MsnOrder == 1).ToList();
            List<WatchEvaluation> evalution = new List<WatchEvaluation>();
            int i = 0;
            while (i < MS1Scans.Count)
            {
                var theScanNum = MS1Scans[i].OneBasedScanNumber;
                var theRT = MS1Scans[i].RetentionTime;
                MzSpectrumTD mzSpectrumTD = new MzSpectrumTD(MS1Scans[i].MassSpectrum.XArray, MS1Scans[i].MassSpectrum.YArray, true);

                var watch = System.Diagnostics.Stopwatch.StartNew();

                //var isotopicEnvelopes = MS1Scans[i].MassSpectrum.Deconvolute(MS1Scans[i].ScanWindowRange, 3, 60, 5.0, 3).OrderBy(p => p.monoisotopicMass).ToList();
                var isotopicEnvelopes = mzSpectrumTD.DeconvoluteTD(MS1Scans[i].ScanWindowRange, 3, 60, 5.0, 3).OrderBy(p => p.monoisotopicMass).ToList();
                watch.Stop();

                var watch1 = System.Diagnostics.Stopwatch.StartNew();

                var chargeDecon = mzSpectrumTD.ChargeDeconvolution(MS1Scans[i].OneBasedScanNumber, MS1Scans[i].RetentionTime, isotopicEnvelopes, new List<double?>());

                watch1.Stop();

                var theEvaluation = new WatchEvaluation();
                theEvaluation.TheScanNumber = theScanNum;
                theEvaluation.TheRT = theRT;
                theEvaluation.WatchIsoDecon = watch.ElapsedMilliseconds;
                theEvaluation.WatchChaDecon = watch1.ElapsedMilliseconds;
                evalution.Add(theEvaluation);
                i++;

            }

            var writtenFile = Path.Combine(Path.GetDirectoryName(spectraFilesObservableCollection.First().FilePath), "watches.mytsv");
            using (StreamWriter output = new StreamWriter(writtenFile))
            {
                output.WriteLine("ScanNum\tRT\tIsotopicDecon\tChargeDecon");
                foreach (var theEvaluation in evalution)
                {
                    output.WriteLine(theEvaluation.TheScanNumber.ToString() + "\t" + theEvaluation.TheRT + "\t" + theEvaluation.WatchIsoDecon.ToString() + "\t" + theEvaluation.WatchChaDecon.ToString());
                }
            }

        }

        private void BtnPerScanTime_Click(object sender, RoutedEventArgs e)
        {
            double[] scanTimes = new double[msDataScans.Count];
            for (int i = 0; i < msDataScans.Count; i++)
            {
                if (i == 0)
                {
                    scanTimes[i] = msDataScans[i].RetentionTime * 60 * 1000;
                }
                else
                {
                    scanTimes[i] = (msDataScans[i].RetentionTime - msDataScans[i - 1].RetentionTime) *60 * 1000;
                }
            }

            var writtenFile = Path.Combine(Path.GetDirectoryName(spectraFilesObservableCollection.First().FilePath), "timesOfMs1.mytsv");
            using (StreamWriter output = new StreamWriter(writtenFile))
            {
                output.WriteLine("ScanNum\tRT\tscanTime");
                for (int i = 0; i < msDataScans.Count; i++)
                {
                    if (msDataScans[i].MsnOrder == 1)
                    {
                        output.WriteLine(msDataScans[i].OneBasedScanNumber.ToString() + "\t" + msDataScans[i].RetentionTime.ToString() + "\t" + scanTimes[i].ToString());
                    }
                }
            }

            var writtenFile2 = Path.Combine(Path.GetDirectoryName(spectraFilesObservableCollection.First().FilePath), "timesOfMs2.mytsv");
            using (StreamWriter output = new StreamWriter(writtenFile2))
            {
                output.WriteLine("ScanNum\tRT\tscanTime");
                for (int i = 0; i < msDataScans.Count; i++)
                {
                    if (msDataScans[i].MsnOrder == 2)
                    {
                        output.WriteLine(msDataScans[i].OneBasedScanNumber.ToString() + "\t" + msDataScans[i].RetentionTime.ToString() + "\t" + scanTimes[i].ToString());
                    }
                }
            }
        }

        private void BtnDeconModel_Click(object sender, RoutedEventArgs e)
        {
            int x = Convert.ToInt32(txtDeconScanNum.Text);
            var msDataScan = msDataScans.Where(p => p.OneBasedScanNumber == x).First();          
            MzSpectrumTD mzSpectrumTD = new MzSpectrumTD(msDataScan.MassSpectrum.XArray, msDataScan.MassSpectrum.YArray, true);
            DeconViewModel.UpdateModelForDeconModel(mzSpectrumTD, Convert.ToInt32(TxtDeconModel.Text));
        }

        private void BtnDeconModelNeu_Click(object sender, RoutedEventArgs e)
        {
            int x = Convert.ToInt32(txtDeconScanNum.Text);
            var msDataScan = msDataScans.Where(p => p.OneBasedScanNumber == x).First();
            MzSpectrumBU mzSpectrumBU = new MzSpectrumBU(msDataScan.MassSpectrum.XArray, msDataScan.MassSpectrum.YArray, true);
            DeconViewModel.UpdateModelForDeconModel(mzSpectrumBU, Convert.ToInt32(TxtDeconModel.Text));
        }

        private void btnDeconNeu_Click(object sender, RoutedEventArgs e)
        {
            int x = Convert.ToInt32(txtDeconScanNumNeu.Text);
            var msDataScan = msDataScans.Where(p => p.OneBasedScanNumber == x).First();
            //IsotopicEnvelopes = msDataScan.MassSpectrum.Deconvolute(msDataScan.ScanWindowRange, 3, 60, 5.0, 3).OrderBy(p => p.monoisotopicMass).ToList();
            MzSpectrumBU mzSpectrumBU = new MzSpectrumBU(msDataScan.MassSpectrum.XArray, msDataScan.MassSpectrum.YArray, true);
            IsotopicEnvelopes = mzSpectrumBU.DeconvoluteBU(msDataScan.ScanWindowRange, 2, 10, 5.0, 3).OrderBy(p => p.massIndex).ToList();
            int i = 1;
            foreach (var item in IsotopicEnvelopes)
            {
                envolopObservableCollection.Add(new EnvolopForDataGrid(i, item.peaks.First().mz, item.charge, item.monoisotopicMass, item.totalIntensity));
                i++;
            }
            mainViewModel.UpdateScanModel(msDataScan);
        }

    }

    public class WatchEvaluation
    {
           public int TheScanNumber { get; set; }
           public double TheRT { get; set; }
           public double WatchIsoDecon { get; set; }
           public  double WatchChaDecon { get; set; }
    }
}
