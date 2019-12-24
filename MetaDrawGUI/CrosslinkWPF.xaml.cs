﻿using System;
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
using MetaDrawGUI.Crosslink;
using ViewModels;
using System.ComponentModel;

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
            var incorrectCsms = CrosslinkHandler.validateIncorrectCrosslinks(MainWindow.thanos.psms.ToArray(), Convert.ToDouble(TbCrosslinkFdrCutOff.Text));
            foreach (var ic in incorrectCsms)
            {
                MainWindow.thanos.crosslinkHandler.IncorrectCsmsCollection.Add(new SpectrumForDataGrid(ic.Ms2ScanNumber, ic.PrecursorScanNum, ic.PrecursorMz, ic.OrganismName));
            }

            TbkCrosslinkValidateResult.Text = CrosslinkHandler.Out(MainWindow.thanos.psms, Convert.ToDouble(TbCrosslinkFdrCutOff.Text));
            if (MainWindow.thanos.psms.GroupBy(p=>p.Filename).Count() > 1)
            {
                TbkCrosslinkValidateResult.Text += CrosslinkHandler.OutSplit(MainWindow.thanos.psms, Convert.ToDouble(TbCrosslinkFdrCutOff.Text));
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
            if (dataGridIncorrectCsms.SelectedItem == null)
            {
                return;
            }

            var sele = (SpectrumForDataGrid)dataGridIncorrectCsms.SelectedItem;
            MainWindow.thanos.msDataScan = MainWindow.thanos.msDataScans.Where(p => p.OneBasedScanNumber == sele.ScanNum).First();
            var selePsm = MainWindow.thanos.psms.Where(p => p.Ms2ScanNumber == sele.ScanNum).First();
            MainWindow.thanos.crosslinkHandler.CrosslinkModel = PsmAnnotationViewModel.DrawScanMatch(MainWindow.thanos.msDataScan, selePsm.MatchedIons, selePsm.BetaPeptideMatchedIons);
        }
    }
}
