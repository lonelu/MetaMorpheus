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
using MetaDrawGUI.Crosslink;

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
        }

        private void BtnValidate_Click(object sender, RoutedEventArgs e)
        {
            CrosslinkHandler.validateCrosslinks(MainWindow.thanos.psms);
        
        }
    }
}
