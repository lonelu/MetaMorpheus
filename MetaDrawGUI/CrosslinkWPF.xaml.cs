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
using MetaDrawGUI.Crosslink;
using ViewModels;
using System.ComponentModel;
using EngineLayer;
using EngineLayer.CrosslinkSearch;
using System.IO;

namespace MetaDrawGUI
{
    /// <summary>
    /// Interaction logic for CrosslinkWPF.xaml
    /// </summary>
    public partial class CrosslinkWPF : UserControl
    {
        MainWindow MainWindow = Application.Current.MainWindow as MetaDrawGUI.MainWindow;

        public CrosslinkWPF()
        {
            InitializeComponent();

            dataGridIncorrectCsms.DataContext = MainWindow.thanos.crosslinkHandler;

            plotCrosslinkView.DataContext = MainWindow.thanos.crosslinkHandler;
        }

        private void BtnValidate_Click(object sender, RoutedEventArgs e)
        {
            var incorrectCsms = CrosslinkHandler.validateIncorrectCrosslinks(MainWindow.thanos.simplePsms.ToArray());


            foreach (var ic in incorrectCsms)
            {
                MainWindow.thanos.crosslinkHandler.IncorrectCsmsCollection.Add(new SpectrumForDataGrid(ic.Ms2ScanNumber, 0, ic.PrecursorMass, ""));
            }

            TbkCrosslinkValidateResult.Text = CrosslinkHandler.ValidateOutput(MainWindow.thanos.simplePsms, Convert.ToDouble(TbCrosslinkFdrCutOff.Text));
        }

        private void BtnWriteIncorrectFile_Click(object sender, RoutedEventArgs e)
        {
            var fdr = Convert.ToDouble(TbCrosslinkFdrCutOff.Text);

            //QValue incorrect result
            var incorrectCsms = CrosslinkHandler.validateIncorrectCrosslinks(MainWindow.thanos.simplePsms.Where(p => p.DecoyContamTarget == "T" && p.QValue <= fdr).ToArray());

            if (incorrectCsms.Count > 0)
            {
                var filepath = MainWindow.thanos.ResultFilePaths.First();

                var ForderPath = Path.Combine(Path.GetDirectoryName(filepath), Path.GetFileNameWithoutExtension(filepath) + "_incorrectCsms.mytsv");

                TsvReader_Id.WriteTsv(ForderPath, incorrectCsms, "Cross");
            }


            //PEP_QValue incorrect result
            var pep_psms_filtered = MainWindow.thanos.simplePsms.Where(p => p.DecoyContamTarget == "T" && p.PEP_QValue <= fdr).ToArray();
            var pep_incorrectCsms = CrosslinkHandler.validateIncorrectCrosslinks(pep_psms_filtered);


            if (pep_incorrectCsms.Count > 0)
            {
                var filepath = MainWindow.thanos.ResultFilePaths.First();

                var ForderPath = Path.Combine(Path.GetDirectoryName(filepath), Path.GetFileNameWithoutExtension(filepath) + "_pep_incorrectCsms.mytsv");

                TsvReader_Id.WriteTsv(ForderPath, pep_incorrectCsms, "Cross");
            }

            //PEP_QValue incorrect pecolator
            string percolatorFath = Path.Combine(Path.GetDirectoryName(MainWindow.thanos.ResultFilePaths.First()), "Crosslinks_Percolator.tsv");
            string incorrect_percolatorFath = Path.Combine(Path.GetDirectoryName(MainWindow.thanos.ResultFilePaths.First()), "Crosslinks_Percolator_incorrect.tsv");

            List<string> incorrect_percolators = new List<string>();

            StreamReader reader = new StreamReader(percolatorFath);
            int lineCount = 0;
            string line;
            char[] split = new char[] { '\t' };
            while (reader.Peek() > 0)
            {
                lineCount++;
                line = reader.ReadLine();

                if (lineCount > 2)
                {
                    var x = line.Split(split);

                    var y = pep_incorrectCsms.Where(p => p.Ms2ScanNumber == int.Parse(x[2]));

                    if (y.Count() >0)
                    {
                        foreach (var z in y)
                        {
                            if (line.Contains(z.FullSeq.Substring(0, z.FullSeq.Length - 4)) && line.Contains(z.BetaPeptideFullSequence.Substring(0, z.BetaPeptideFullSequence.Length - 4)))
                            {
                                incorrect_percolators.Add(line);
                            }
                        }
                    }
                }
                else
                {
                    incorrect_percolators.Add(line);
                }
            }
            reader.Close();

            using (StreamWriter output = new StreamWriter(incorrect_percolatorFath))
            {
                foreach (var heh in incorrect_percolators)
                {
                    output.WriteLine(heh);
                }
            }


        }

        private void CheckIfNumber(object sender, TextCompositionEventArgs e)
        {
            bool result = true;
            foreach (var character in e.Text)
            {
                if (!Char.IsDigit(character) && !(character == '.') && !(character == '-'))
                {
                    result = false;
                }
            }
            e.Handled = !result;
        }

        private void DataGridIncorrectCsms_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            return;

            //The function is not working now.
            if (dataGridIncorrectCsms.SelectedItem == null)
            {
                return;
            }

            var sele = (SpectrumForDataGrid)dataGridIncorrectCsms.SelectedItem;

            if (MainWindow.thanos.msDataScans.Where(p => p.OneBasedScanNumber == sele.ScanNum).Count() > 0)
            {
                MainWindow.thanos.msDataScan = MainWindow.thanos.msDataScans.Where(p => p.OneBasedScanNumber == sele.ScanNum).First();
                var selePsm = MainWindow.thanos.simplePsms.Where(p => p.Ms2ScanNumber == sele.ScanNum).First();
                if (selePsm.MatchedIons ==null)
                {
               //     var fragmentsForEachAlphaLocalizedPossibility = CrosslinkedPeptide.XlGetTheoreticalFragments(MassSpectrometry.DissociationType.HCD,
               //Crosslinker., possibleAlphaXlSites, selePsm.BetaPeptideWithMod.MonoisotopicMass, selePsm.PeptideWithMod).ToList();
                }
                MainWindow.thanos.crosslinkHandler.CrosslinkModel = PsmAnnotationViewModel.DrawScanMatch(MainWindow.thanos.msDataScan, selePsm.MatchedIons, selePsm.BetaPeptideMatchedIons);
            }
        }

    }
}
