using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViewModels;
using System.ComponentModel;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Series;
using MassSpectrometry;

namespace MetaDrawGUI
{
    public enum DeconvolutorSkill
    {
        DeconSeleScan = 0,
        PlotAvaragineModel = 1,
        DeconQuant = 2,
        DeconChargeParsi = 3
    }

    public class Deconvolutor: INotifyPropertyChanged
    {
        public DeconViewModel deconViewModel { get; set; }

        public MsDataScan msDataScan { get; set; }
        public int modelStartNum { get; set; }

        public PlotModel DeconModel
        {
            get
            {
                return deconViewModel.privateModel;
            }
            set
            {
                deconViewModel.privateModel = value;
                NotifyPropertyChanged("DeconModel");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }


        public void Deconvolute()
        {
            if (msDataScan != null)
            {
                MzSpectrumBU mzSpectrumBU = new MzSpectrumBU(msDataScan.MassSpectrum.XArray, msDataScan.MassSpectrum.YArray, true);
                deconViewModel.UpdateModelForDeconModel(mzSpectrumBU, modelStartNum);
                DeconModel = deconViewModel.privateModel;
            }
            else
            {

                var mzSpectrumBU = new MzSpectrumBU(new double[] { 1 }, new double[] { 1 }, true);
                deconViewModel.UpdateModelForDeconModel(mzSpectrumBU, modelStartNum);
                DeconModel = deconViewModel.privateModel;
            }
        }
    
    }
}
