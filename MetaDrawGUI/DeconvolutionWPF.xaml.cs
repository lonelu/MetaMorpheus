using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.IO;
using ViewModels;
using EngineLayer;
using MzLibUtil;
using System.Text.RegularExpressions;
using MassSpectrometry;
using System.Globalization;
using System.ComponentModel;
using TaskLayer;
using Chemistry;


namespace MetaDrawGUI
{
    /// <summary>
    /// Interaction logic for DeconvolutionWPF.xaml
    /// </summary>
    public partial class DeconvolutionWPF : UserControl
    {
        MainWindow MainWindow = Application.Current.MainWindow as MetaDrawGUI.MainWindow;

        public DeconvolutionWPF()
        {
            InitializeComponent();

            PopulateChoice();

            UpdatePanel();

            plotView.DataContext = MainWindow.thanos.deconvolutor;

            plotViewDecon.DataContext = MainWindow.thanos.deconvolutor;

            plotViewXIC.DataContext = MainWindow.thanos.deconvolutor;

            //dataGridPsms.DataContext = thanos.deconvolutor;

            dataGridDeconNums.DataContext = MainWindow.thanos.deconvolutor;

            dataGridChargeEnves.DataContext = MainWindow.thanos.deconvolutor;
        }


        private void PopulateChoice()
        {
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
            txtMinAssumedChargeState.Text = MainWindow.thanos.DeconvolutionParameter.DeconvolutionMinAssumedChargeState.ToString();
            txtMaxAssumedChargeState.Text = MainWindow.thanos.DeconvolutionParameter.DeconvolutionMaxAssumedChargeState.ToString();
            txtDeconvolutionToleranc.Text = MainWindow.thanos.DeconvolutionParameter.DeconvolutionMassTolerance.ToString();
            txtIntensityRatioLimit.Text = MainWindow.thanos.DeconvolutionParameter.DeconvolutionIntensityRatio.ToString();
            CbIsLookPartner.IsChecked = MainWindow.thanos.DeconvolutionParameter.ToGetPartner;
            TxtPartnerMassDiff.Text = MainWindow.thanos.DeconvolutionParameter.PartnerMassDiff.ToString();
            TxtMaxLabelNum.Text = MainWindow.thanos.DeconvolutionParameter.MaxmiumLabelNumber.ToString();
            TxtPartnerRatio.Text = MainWindow.thanos.DeconvolutionParameter.PartnerPairRatio.ToString();
            TxtParnerTolerance.Text = MainWindow.thanos.DeconvolutionParameter.ParterMassTolerance.ToString();
        }

        private void UpdateField()
        {
            MainWindow.thanos.DeconvolutionParameter.DeconvolutionMinAssumedChargeState = int.Parse(txtMinAssumedChargeState.Text);
            MainWindow.thanos.DeconvolutionParameter.DeconvolutionMaxAssumedChargeState = int.Parse(txtMaxAssumedChargeState.Text);
            MainWindow.thanos.DeconvolutionParameter.DeconvolutionMassTolerance = double.Parse(txtDeconvolutionToleranc.Text);
            MainWindow.thanos.DeconvolutionParameter.DeconvolutionIntensityRatio = double.Parse(txtIntensityRatioLimit.Text);
            MainWindow.thanos.DeconvolutionParameter.ToGetPartner = CbIsLookPartner.IsChecked.Value;
            MainWindow.thanos.DeconvolutionParameter.PartnerMassDiff = double.Parse(TxtPartnerMassDiff.Text);
            MainWindow.thanos.DeconvolutionParameter.MaxmiumLabelNumber = int.Parse(TxtMaxLabelNum.Text);
            MainWindow.thanos.DeconvolutionParameter.PartnerPairRatio = double.Parse(TxtPartnerRatio.Text);
            MainWindow.thanos.DeconvolutionParameter.ParterMassTolerance = double.Parse(TxtParnerTolerance.Text);

            MainWindow.thanos.ControlParameter.deconScanNum = Convert.ToInt32(txtDeconScanNum.Text);
            MainWindow.thanos.ControlParameter.modelStartNum = Convert.ToInt32(TxtDeconModel.Text);
            MainWindow.thanos.ControlParameter.DeconChargeMass = double.Parse(TxtDeconChargeMass.Text);
        }

        #region Deconvolution Control

        private void BtnLoadDeconResults_Click(object sender, RoutedEventArgs e)
        {
            foreach (var collection in MainWindow.resultFilesObservableCollection)
            {
                MainWindow.resultsFilePath = collection.FilePath;
                if (MainWindow.resultsFilePath == null)
                {
                    continue;
                }
                MainWindow.thanos.ResultFilePaths.Add(MainWindow.resultsFilePath);
                // load the PSMs
                MainWindow.thanos.msFeatures.AddRange(TsvReader_MsFeature.ReadTsv(MainWindow.resultsFilePath));
            }

            DataGridFlashDeconEnvelopes.DataContext = MainWindow.thanos.deconvolutor;

            foreach (var feature in MainWindow.thanos.msFeatures)
            {
                MainWindow.thanos.deconvolutor.MsFeatureCollection.Add(new MsFeatureForDataGrid(feature));
            }
        }

        private void BtnLoadTopResults_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.thanos.simplePsms.Clear();
            foreach (var collection in MainWindow.resultFilesObservableCollection)
            {
                MainWindow.resultsFilePath = collection.FilePath;
                if (MainWindow.resultsFilePath == null)
                {
                    continue;
                }
                MainWindow.thanos.ResultFilePaths.Add(MainWindow.resultsFilePath);
                // load the PSMs
                MainWindow.thanos.simplePsms.AddRange(TsvReader_Id.ReadTsv(MainWindow.resultsFilePath));
            }

            foreach (var psm in MainWindow.thanos.simplePsms)
            {
                MainWindow.spectrumNumsObservableCollection.Add(new SpectrumForDataGrid(psm.Ms2ScanNumber, 0, psm.PrecursorMz, ""));
            }
        }

        private void btnResetDecon_Click(object sender, RoutedEventArgs e)
        {
            ResetDataGridAndModel();
        }

        private void ResetDataGridAndModel()
        {
            MainWindow.thanos.deconvolutor.envolopCollection.Clear();
            MainWindow.thanos.deconvolutor.DeconModel = DeconViewModel.ResetDeconModel();
            MainWindow.thanos.deconvolutor.Model = MainViewModel.ResetViewModel();
            MainWindow.thanos.deconvolutor.chargeEnvelopesCollection.Clear();
            MainWindow.thanos.deconvolutor.XicModel = PeakViewModel.ResetViewModel();
        }

        private void FindChargeDecon()
        {
            string scanFilter = MainWindow.thanos.msDataScan.ScanFilter;
            var precursor_string = scanFilter.Split(' ').Where(p => p.Contains("@"));
            List<string> precursors = new List<string>();

            foreach (var pr in precursor_string)
            {
                precursors.Add(pr.Split('@').First());
            }

            if (precursors.Count > 1)
            {
                int ind = MainWindow.thanos.msDataScan.OneBasedScanNumber - 1;
                bool findit = false;
                while (ind > MainWindow.thanos.msDataScan.OneBasedScanNumber - 10 && !findit)
                {
                    if (MainWindow.thanos.msDataScans[ind].MsnOrder == 2)
                    {
                        ind--;
                        continue;
                    }

                    MzSpectrumXY mzSpectrumXY = new MzSpectrumXY(MainWindow.thanos.msDataScans[ind].MassSpectrum.XArray, MainWindow.thanos.msDataScans[ind].MassSpectrum.YArray, true);

                    List<IsoEnvelop> isoEnvelops;
                    var chargeEnves = ChargeDecon.QuickChargeDeconForScan(mzSpectrumXY, MainWindow.thanos.DeconvolutionParameter, out isoEnvelops);

                    foreach (var ce in chargeEnves)
                    {
                        var test = ce.mzs_box.Select(p => p.ToString("0.000") + "0").ToList();
                        if (test.First() == precursors.First() && test.Last() == precursors.Last())
                        {

                            findit = true;

                            MainWindow.thanos.deconvolutor.DeconModel = ChargeEnveViewModel.DrawCharEnvelopMatch(MainWindow.thanos.msDataScans[ind], ce);

                            //thanos.deconvolutor.chargeEnvelopesCollection.Add(new ChargeEnvelopesForDataGrid(ind, ce.FirstMz, ce.FirstIntensity, ce.UnUsedMzsRatio, ce.IsoEnveNum, ce.ChargeDeconScore, ce.mzs_box));

                            break;
                        }
                    }

                    ind--;
                }
            }
            else if (precursors.Count == 1)
            {
                int ind = MainWindow.thanos.msDataScan.OneBasedScanNumber - 1;
                bool findit = false;
                while (ind > MainWindow.thanos.msDataScan.OneBasedScanNumber - 10 && !findit)
                {
                    if (MainWindow.thanos.msDataScans[ind].MsnOrder == 2)
                    {
                        ind--;
                        continue;
                    }

                    MzSpectrumXY mzSpectrumXY = new MzSpectrumXY(MainWindow.thanos.msDataScans[ind].MassSpectrum.XArray, MainWindow.thanos.msDataScans[ind].MassSpectrum.YArray, true);

                    var isos = IsoDecon.MsDeconv_Deconvolute(mzSpectrumXY, MainWindow.thanos.msDataScan.ScanWindowRange, MainWindow.thanos.DeconvolutionParameter).OrderBy(p => p.MonoisotopicMass).ToList();

                    foreach (var iso in isos)
                    {
                        var test = iso.ExperimentIsoEnvelop.First().Mz.ToString("0.000") + "0";
                        if (test == precursors.First())
                        {
                            findit = true;

                            MainWindow.thanos.deconvolutor.DeconModel = DeconViewModel.UpdataModelForDecon(MainWindow.thanos.msDataScans[ind], iso);
                        }

                    }

                    ind--;
                }
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

            MainWindow.thanos.msDataScan = MainWindow.thanos.msDataScans.Where(p => p.OneBasedScanNumber == sele.ScanNum).First();
            MainWindow.thanos.deconvolutor.Model = MainViewModel.DrawScan(MainWindow.thanos.msDataScan, sele.MsFeature);

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

            var envo = MainWindow.thanos.deconvolutor.IsotopicEnvelopes[sele.Ind - 1];
            MainWindow.thanos.deconvolutor.DeconModel = DeconViewModel.UpdataModelForDecon(MainWindow.thanos.msDataScan, envo);
            MainWindow.thanos.deconvolutor.XicModel = PeakViewModel.DrawXic(envo.MonoisotopicMass, envo.Charge, MainWindow.thanos.msDataScan.RetentionTime, MainWindow.thanos.msDataFile, new PpmTolerance(5), 5.0, 3, "");
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

            if (MainWindow.thanos.deconvolutor.ChargeEnvelops.Count == 0)
            {
                return;
            }

            var blockes = ChargeDecon.GenerateBoxes(MainWindow.thanos.deconvolutor.IsotopicEnvelopes);

            var ce = MainWindow.thanos.deconvolutor.ChargeEnvelops.ElementAt(sele.Ind - 1);

            MainWindow.thanos.deconvolutor.Model = ChargeEnveViewModel.DrawCharEnvelopMatch(MainWindow.thanos.msDataScan, ce, blockes);

            MainWindow.thanos.deconvolutor.DeconModel = ChargeEnveViewModel.DrawCharEnvelopModel(MainWindow.thanos.msDataScan, ce);
        }

        #endregion

        private void BtnDeconStart_Click(object sender, RoutedEventArgs e)
        {
            UpdateField();

            DeconvolutorSkill deconvolutorSkills = ((DeconvolutorSkill)cmbDeconAction.SelectedIndex);

            switch (deconvolutorSkills)
            {
                case DeconvolutorSkill.DeconSeleScan:
                    MainWindow.action = MainWindow.thanos.deconvolutor.Decon;
                    break;
                case DeconvolutorSkill.PlotAvaragineModel:
                    MainWindow.action = MainWindow.thanos.deconvolutor.PlotDeconModel;
                    break;
                case DeconvolutorSkill.DeconQuant:
                    MainWindow.action = MainWindow.thanos.deconvolutor.DeconQuant;
                    break;
                case DeconvolutorSkill.DeconTotalPartners:
                    MainWindow.action = MainWindow.thanos.deconvolutor.DeconTotalPartners;
                    break;
                case DeconvolutorSkill.DeconWatch:
                    MainWindow.action = MainWindow.thanos.deconvolutor.DeconWatch;
                    break;
                case DeconvolutorSkill.DeconIsoByPeak:
                    MainWindow.action = MainWindow.thanos.deconvolutor.DeconIsoByPeak;
                    break;
                case DeconvolutorSkill.DeconChargeByPeak:
                    MainWindow.action = MainWindow.thanos.deconvolutor.DeconChargeByPeak;
                    break;
                case DeconvolutorSkill.DeconDrawTwoScan:
                    MainWindow.action = MainWindow.thanos.deconvolutor.PlotTwoScan;
                    break;
                case DeconvolutorSkill.DeconDrawIntensityDistribution:
                    MainWindow.action = MainWindow.thanos.deconvolutor.PlotIntensityDistribution;
                    break;
                case DeconvolutorSkill.DeconCompareBoxVsNormalId:
                    MainWindow.action = MainWindow.thanos.deconvolutor.DeconCompareBoxVsNormalId;
                    break;
                case DeconvolutorSkill.IdFragmentationOptimize:
                    MainWindow.action = MainWindow.thanos.deconvolutor.IdFragmentationOptimize;
                    break;
                case DeconvolutorSkill.IdProteoformOverlap:
                    MainWindow.action = MainWindow.thanos.deconvolutor.NumberOfProteoformOverlap;
                    break;
                case DeconvolutorSkill.FindMaxQuantPartner:
                    MainWindow.action = MainWindow.thanos.deconvolutor.FindMaxQuantPartner;
                    break;
                default:
                    break;
            }

            if (MainWindow.action != null)
            {
                MainWindow.action();
            }
        }

        private void BtnMatch_Click(object sender, RoutedEventArgs e)
        {
            if (TxtBaseSeq.Text == null && TxtBaseSeq.Text.Trim() == "")
            {
                return;
            }

            List<(int, string, double)> mods = new List<(int, string, double)>();
            if (TxtMod1.Text != null && TxtMod1.Text.Trim() != "")
            {
                var x = TxtMod1.Text.Split(',');
                mods.Add((int.Parse(x[0]), x[1], double.Parse(x[2])));
            }
            if (TxtMod2.Text != null && TxtMod2.Text.Trim() != "")
            {
                var y = TxtMod2.Text.Split(',');
                mods.Add((int.Parse(y[0]), y[1], double.Parse(y[2])));
            }
            if (TxtMod3.Text != null && TxtMod3.Text.Trim() != "")
            {
                var z = TxtMod3.Text.Split(',');
                mods.Add((int.Parse(z[0]), z[1], double.Parse(z[2])));
            }

            var pep = MainWindow.thanos.PeptideFromInput(TxtBaseSeq.Text.Trim(), mods);
            if (MainWindow.thanos.msDataScan != null)
            {
                var matched = MainWindow.thanos.DoPeptideSpectrumMatch(pep);

                MainWindow.thanos.deconvolutor.Model = PsmAnnotationViewModel.DrawScanMatch(MainWindow.thanos.msDataScan, matched);
            }
        }
    }
}
