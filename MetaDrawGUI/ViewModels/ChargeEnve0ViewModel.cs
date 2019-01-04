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
    public class ChargeEnve0ViewModel : INotifyPropertyChanged
    {
        private PlotModel chargeEnveModel0;

        public PlotModel ChargeEnveModel0
        {
            get
            {
                return this.chargeEnveModel0;
            }
            set
            {
                this.chargeEnveModel0 = value;
                NotifyPropertyChanged("ChargeEnveModel0");
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

        public ChargeEnve0ViewModel()
        {
            // Create the plot model
            var tmp = new PlotModel { Title = "ChargeEnve Spectrum Annotation", Subtitle = "using OxyPlot" };

            // Set the Model property, the INotifyPropertyChanged event will make the WPF Plot control update its content
            this.ChargeEnveModel0 = tmp;
        }

        public void UpdataModelForChargeEnve0(MsDataScan MsScanForDraw, ChargeDeconEnvelope chargeDeconEnvelope)
        {
            var isoEnves = chargeDeconEnvelope.isotopicEnvelopes;
            string scanNum = MsScanForDraw.OneBasedScanNumber.ToString();

            PlotModel model = new PlotModel { Title = "Spectrum anotation of Scan " + scanNum, DefaultFontSize = 15 };
            model.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "m/z", Minimum = 0, Maximum = MsScanForDraw.MassSpectrum.XArray.Max() * 1.02 });
            model.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "Intensity(counts)", Minimum = 0, Maximum = MsScanForDraw.MassSpectrum.YArray.Max() * 1.2 });

            for (int i = 0; i < isoEnves.Count; i++)
            {
                foreach (var isoEnve in isoEnves)
                {
                    foreach (var peak in isoEnve.peaks)
                    {
                        var sPeak = new LineSeries();
                        sPeak.Color = OxyColors.DimGray;
                        sPeak.StrokeThickness = 1.5;
                        sPeak.Points.Add(new DataPoint(peak.mz, 0));
                        sPeak.Points.Add(new DataPoint(peak.mz, peak.intensity));
                        model.Series.Add(sPeak);
                    }

                }
                
            }
            model.Axes[0].AxisChanged += XAxisChanged;
            // Set the Model property, the INotifyPropertyChanged event will make the WPF Plot control update its content
            this.ChargeEnveModel0 = model;
        }

        public void ResetDeconModel()
        {
            // Create the plot model
            var tmp = new PlotModel { Title = "ChargeEnve Spectrum Annotation", Subtitle = "using OxyPlot" };

            // Set the Model property, the INotifyPropertyChanged event will make the WPF Plot control update its content
            this.ChargeEnveModel0 = tmp;
        }

        private void XAxisChanged(object sender, AxisChangedEventArgs e)
        {
            double fold = (this.chargeEnveModel0.Axes[0].ActualMaximum - this.chargeEnveModel0.Axes[0].ActualMinimum) / (this.chargeEnveModel0.Axes[0].AbsoluteMaximum - this.chargeEnveModel0.Axes[0].AbsoluteMinimum);
            this.chargeEnveModel0.Axes[1].Minimum = 0;
            this.chargeEnveModel0.Axes[1].Maximum = this.chargeEnveModel0.Axes[1].AbsoluteMaximum * 0.6 * fold;

            foreach (var series in this.chargeEnveModel0.Series)
            {
                if (series is LineSeries)
                {
                    var x = (LineSeries)series;
                    if (x.Points[1].X >= this.chargeEnveModel0.Axes[0].ActualMinimum && x.Points[1].X <= this.chargeEnveModel0.Axes[0].ActualMaximum)
                    {
                        if (x.Points[1].Y > this.chargeEnveModel0.Axes[1].Maximum)
                        {
                            this.chargeEnveModel0.Axes[1].Maximum = x.Points[1].Y * 1.2;
                        }
                    }

                }
            }
        }
    }
}
