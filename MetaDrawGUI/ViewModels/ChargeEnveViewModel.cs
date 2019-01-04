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
            var charges = chargeDeconEnvelope.chargeStates;
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

                var peakAnno = new TextAnnotation();
                peakAnno.TextRotation = 90;
                peakAnno.Font = "Arial";
                peakAnno.FontSize = 12;
                peakAnno.TextColor = OxyColors.Red;
                peakAnno.StrokeThickness = 0;
                peakAnno.TextPosition = sPeaksModel[i].Points[1];
                peakAnno.Text = charges[i].ToString();
                model.Annotations.Add(peakAnno);
            }
            model.Axes[0].AxisChanged += XAxisChanged;
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

        private void XAxisChanged(object sender, AxisChangedEventArgs e)
        {
            double fold = (this.chargeEnveModel.Axes[0].ActualMaximum - this.chargeEnveModel.Axes[0].ActualMinimum) / (this.chargeEnveModel.Axes[0].AbsoluteMaximum - this.chargeEnveModel.Axes[0].AbsoluteMinimum);
            this.chargeEnveModel.Axes[1].Minimum = 0;
            this.chargeEnveModel.Axes[1].Maximum = this.chargeEnveModel.Axes[1].AbsoluteMaximum * 0.6 * fold;

            foreach (var series in this.chargeEnveModel.Series)
            {
                if (series is LineSeries)
                {
                    var x = (LineSeries)series;
                    if (x.Points[1].X >= this.chargeEnveModel.Axes[0].ActualMinimum && x.Points[1].X <= this.chargeEnveModel.Axes[0].ActualMaximum)
                    {
                        if (x.Points[1].Y > this.chargeEnveModel.Axes[1].Maximum)
                        {
                            this.chargeEnveModel.Axes[1].Maximum = x.Points[1].Y * 1.2;
                        }
                    }

                }
            }
        }
    }
}
