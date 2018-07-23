using System.Linq;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.Annotations;
using System.ComponentModel;
using MassSpectrometry;
using MetaDrawGUI;

namespace ViewModels
{
    public class ChargeEnveViewModel : INotifyPropertyChanged
    {
        private PlotModel chargeEnveModel;

        public PlotModel ChargeEnveModel
        {
            get
            {
                return this.chargeEnveModel;
            }
            set
            {
                this.chargeEnveModel = value;
                NotifyPropertyChanged("ChargeEnveModel");
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public ChargeEnveViewModel()
        {
            // Create the plot model
            var tmp = new PlotModel { Title = "ChargeEnve Spectrum Annotation", Subtitle = "using OxyPlot" };

            // Set the Model property, the INotifyPropertyChanged event will make the WPF Plot control update its content
            this.ChargeEnveModel = tmp;
        }

        public void UpdataModelForChargeEnve(MsDataScan MsScanForDraw, ChargeDeconEnvelope chargeDeconEnvelope)
        {
            var x = chargeDeconEnvelope.mzFit;
            var y = chargeDeconEnvelope.intensitiesFit;
            var z = chargeDeconEnvelope.intensitiesModel;
            string scanNum = MsScanForDraw.OneBasedScanNumber.ToString();

            PlotModel model = new PlotModel { Title = "Spectrum anotation of Scan " + scanNum, DefaultFontSize = 15 };
            model.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "m/z", Minimum = 0, Maximum = MsScanForDraw.MassSpectrum.XArray.Max() * 1.02 });
            model.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "Intensity(counts)", Minimum = 0, Maximum = chargeDeconEnvelope.intensitiesFit.Max() * 1.2 });

            LineSeries[] sPeaks = new LineSeries[chargeDeconEnvelope.mzFit.Length];
            LineSeries[] sPeaksModel = new LineSeries[chargeDeconEnvelope.mzFit.Length];

            for (int i = 0; i < x.Length; i++)
            {
                sPeaks[i] = new LineSeries();
                sPeaks[i].Color = OxyColors.DimGray;
                sPeaks[i].StrokeThickness = 1.5;
                sPeaks[i].Points.Add(new DataPoint(x[i], 0));
                sPeaks[i].Points.Add(new DataPoint(x[i], y[i]));
                model.Series.Add(sPeaks[i]);
            }

            for (int i = 0; i < x.Length; i++)
            {
                sPeaksModel[i] = new LineSeries();
                sPeaksModel[i].Color = OxyColors.Red;
                sPeaksModel[i].StrokeThickness = 0.75;
                sPeaksModel[i].Points.Add(new DataPoint(x[i], 0));
                sPeaksModel[i].Points.Add(new DataPoint(x[i], z[i]));
                model.Series.Add(sPeaksModel[i]);
            }

            // Set the Model property, the INotifyPropertyChanged event will make the WPF Plot control update its content
            this.ChargeEnveModel = model;
        }

        public void ResetDeconModel()
        {
            // Create the plot model
            var tmp = new PlotModel { Title = "ChargeEnve Spectrum Annotation", Subtitle = "using OxyPlot" };

            // Set the Model property, the INotifyPropertyChanged event will make the WPF Plot control update its content
            this.ChargeEnveModel = tmp;
        }
    }
}
