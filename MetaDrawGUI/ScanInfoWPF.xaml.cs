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
    /// Interaction logic for ScanInfoWPF.xaml
    /// </summary>
    public partial class ScanInfoWPF : UserControl
    {
        MainWindow MainWindow = Application.Current.MainWindow as MetaDrawGUI.MainWindow;

        public ScanInfoWPF()
        {
            InitializeComponent();

            plotView_ScanInfo.DataContext = MainWindow.thanos.accountant;
        }

        #region ScanInfo Control

        private void BtnDrawInfo_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.thanos.accountant.ScanInfoModel = ScanInfoViewModel.DrawScanInfo_PT_Model(MainWindow.thanos.accountant.ScanInfos);
        }

        private void BtnDrawInfo_IJ_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.thanos.accountant.ScanInfoModel = ScanInfoViewModel.DrawScanInfo_IJ_Model(MainWindow.thanos.accountant.ScanInfos);
        }

        private void BtnSavePNG_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.thanos.SavePNG(MainWindow.thanos.accountant.ScanInfoModel);
        }

        #endregion
    }
}
