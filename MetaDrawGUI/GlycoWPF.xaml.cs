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

            PopulateChoice();

            plotAnnoView.DataContext = MainWindow.thanos;

            //dataGridGlycoResultFiles.DataContext = resultFilesObservableCollection;

            dataGridGlyco.DataContext = MainWindow.thanos.sweetor;

            dataGridGlyco.DataContext = MainWindow.thanos.sweetor;

            dataGridGlycan.DataContext = MainWindow.thanos.sweetor;
        }

        private void PopulateChoice()
        {
            foreach (string aSkill in Enum.GetNames(typeof(SweetorSkill)))
            {
                {
                    cmbStweetorAction.Items.Add(aSkill);
                }
            }
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

        private void BtnClearGlycoResultFiles_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.thanos.sweetor.GlycoResultCollection.Clear();
        }

        private void DataGridGlyco_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            if (dataGridGlyco.SelectedItem == null)
            {
                return;
            }

            var sele = (GlycoStructureForDataGrid)dataGridGlyco.SelectedItem;
            MainWindow.thanos.msDataScan = MainWindow.thanos.msDataScans.Where(p => p.OneBasedScanNumber == sele.ScanNum).First();
            var selePsm = MainWindow.thanos.simplePsms.Where(p => p.Ms2ScanNumber == sele.ScanNum).First();
            selePsm.MatchedIons = SimplePsm.GetMatchedIons(selePsm.PeptideWithMod, selePsm.PrecursorMass, selePsm.ChargeState, MainWindow.thanos.CommonParameters, MainWindow.thanos.msDataScan);
            MainWindow.thanos.PsmAnnoModel = PsmAnnotationViewModel.DrawScanMatch(MainWindow.thanos.msDataScan, selePsm.MatchedIons);

            //Draw Glycan
            glyCanvas.Children.Clear();
            GlycanStructureAnnotation.DrawGlycan(glyCanvas, sele.Structure, 50);
        }

        private void BtnLoadGlycans_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.thanos.sweetor.NGlycans = GlycanDatabase.LoadGlycan(GlobalVariables.GlycanLocations.First()).ToList();
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

        private void BtnStweetorAct_Click(object sender, RoutedEventArgs e)
        {
            SweetorSkill sweetorSkill = ((SweetorSkill)cmbStweetorAction.SelectedIndex);

            switch (sweetorSkill)
            {
                case SweetorSkill.PlotGlycoFamily:
                    MainWindow.action = MainWindow.thanos.sweetor.PlotGlycoFamily;
                    break;
                case SweetorSkill.BuildGlycoFamily:
                    MainWindow.action = MainWindow.thanos.sweetor.BuildGlycoFamily;
                    break;
                case SweetorSkill.Write_GlycoResult:
                    MainWindow.action = MainWindow.thanos.sweetor.Write_GlycoResult;
                    break;
                case SweetorSkill.FilterSemiTrypsinResult:
                    MainWindow.action = MainWindow.thanos.sweetor.FilterSemiTrypsinResult;
                    break;
                case SweetorSkill.FilterPariedScan:
                    MainWindow.action = MainWindow.thanos.sweetor.FilterPariedScan;
                    break;
                case SweetorSkill.Compare_Byonic_MetaMorpheus_EachScan:
                    MainWindow.action = MainWindow.thanos.sweetor.Compare_Byonic_MetaMorpheus_EachScan;
                    break;
                case SweetorSkill.MetaMorpheus_coisolation_Evaluation:
                    MainWindow.action = MainWindow.thanos.sweetor.MetaMorpheus_coisolation_Evaluation;
                    break;
                case SweetorSkill.Compare_Seq_overlap:
                    MainWindow.action = MainWindow.thanos.sweetor.Compare_Seq_overlap;
                    break;
                default:
                    break;
            }

            if (MainWindow.action != null)
            {
                MainWindow.action();
            }
        }
    }
}
