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
    /// Interaction logic for GlycoWPF.xaml
    /// </summary>
    public partial class GlycoWPF : UserControl
    {
        MainWindow MainWindow = Application.Current.MainWindow as MetaDrawGUI.MainWindow;

        public GlycoWPF()
        {
            InitializeComponent();

            plotAnnoView.DataContext = MainWindow.thanos;

            //dataGridGlycoResultFiles.DataContext = resultFilesObservableCollection;

            dataGridGlyco.DataContext = MainWindow.thanos.sweetor;

            dataGridGlyco.DataContext = MainWindow.thanos.sweetor;

            dataGridGlycan.DataContext = MainWindow.thanos.sweetor;
        }

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
                    MainWindow.AddAFile(rawDataFromSelected);
                }
            dataGridGlycoResultFiles.Items.Refresh();
        }

        private void BtnClearGlycoResultFiles_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.thanos.sweetor.GlycoResultCollection.Clear();
        }

        private void BtnLoadGlycoResults_Click(object sender, RoutedEventArgs e)
        {
            // load the spectra file
            (sender as Button).IsEnabled = false;
            BtnAddGlycoResultFiles.IsEnabled = false;
            btnClearGlycoResultFiles.IsEnabled = false;

            foreach (var collection in MainWindow.resultFilesObservableCollection)
            {
                MainWindow.resultsFilePath = collection.FilePath;
                if (MainWindow.resultsFilePath == null)
                {
                    continue;
                }
                // load the PSMs
                MainWindow.thanos.simplePsms.AddRange(TsvReader_Id.ReadTsv(MainWindow.resultsFilePath));
            }

            foreach (var psm in MainWindow.thanos.simplePsms)
            {
                MainWindow.thanos.sweetor.GlycoStrucureCollection.Add(new GlycoStructureForDataGrid(psm.ScanNum));
            }
        }

        private void BtnLoadMsFeatureResults_Click(object sender, RoutedEventArgs e)
        {
            foreach (var collection in MainWindow.resultFilesObservableCollection)
            {
                MainWindow.resultsFilePath = collection.FilePath;
                if (MainWindow.resultsFilePath == null)
                {
                    continue;
                }
                // load the PSMs
                MainWindow.thanos.msFeatures.AddRange(TsvReader_MsFeature.ReadTsv(MainWindow.resultsFilePath));
            }

            foreach (var feature in MainWindow.thanos.msFeatures)
            {
                MainWindow.thanos.sweetor.MsFeatureCollection.Add(new MsFeatureForDataGrid(feature));
            }
        }

        private void DataGridGlyco_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            if (dataGridGlyco.SelectedItem == null)
            {
                return;
            }

            var sele = (GlycoStructureForDataGrid)dataGridGlyco.SelectedItem;
            MainWindow.thanos.msDataScan = MainWindow.thanos.msDataScans.Where(p => p.OneBasedScanNumber == sele.ScanNum).First();
            var selePsm = MainWindow.thanos.simplePsms.Where(p => p.ScanNum == sele.ScanNum).First();
            selePsm.MatchedIons = SimplePsm.GetMatchedIons(selePsm.glycoPwsm, selePsm.PrecursorMass, selePsm.ChargeState, MainWindow.thanos.CommonParameters, MainWindow.thanos.msDataScan);
            MainWindow.thanos.PsmAnnoModel = PsmAnnotationViewModel.DrawScanMatch(MainWindow.thanos.msDataScan, selePsm.MatchedIons);

            //Draw Glycan
            glyCanvas.Children.Clear();
            GlycanStructureAnnotation.DrawGlycan(glyCanvas, sele.Structure, 50);
        }

        private void BtnLoadGlycans_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.thanos.sweetor.NGlycans = Glycan.LoadGlycan(GlobalVariables.NGlycanLocation).ToList();
            foreach (var glycan in MainWindow.thanos.sweetor.NGlycans)
            {
                MainWindow.thanos.sweetor.glycanDatabaseCollection.Add(new GlycanDatabaseForDataGrid(glycan.GlyId, Glycan.GetKindString(glycan.Kind), glycan.Struc));
            }

            //GlycanBox.GlobalOGlycans = Glycan.LoadGlycan(GlobalVariables.OGlycanLocation).ToArray();
            //GlycanBox.GlobalOGlycanModifications = GlycanBox.BuildGlobalOGlycanModifications(GlycanBox.GlobalOGlycans);
            //thanos.sweetor.OGlycanGroup = GlycanBox.BuildOGlycanBoxes(4).OrderBy(p => p.Mass).ToArray();
            //thanos.sweetor.WriteOGlycanGroupResult("C:\\Users\\Moon\\Desktop\\");
        }

        private void BtnSearchGlycan_Click(object sender, RoutedEventArgs e)
        {
            if (TxtGlycanKind.Text != null)
            {
                var x = MainWindow.thanos.sweetor.NGlycans.Where(p => Glycan.GetKindString(p.Kind) == TxtGlycanKind.Text).ToList();

                if (x.Count > 0)
                {
                    MainWindow.thanos.sweetor.glycanDatabaseCollection.Clear();
                    foreach (var glycan in x)
                    {
                        MainWindow.thanos.sweetor.glycanDatabaseCollection.Add(new GlycanDatabaseForDataGrid(glycan.GlyId, Glycan.GetKindString(glycan.Kind), glycan.Struc));
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
    }
}
