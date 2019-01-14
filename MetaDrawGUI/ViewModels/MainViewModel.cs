using System.Linq;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.Annotations;
using System.ComponentModel;
using MassSpectrometry;

namespace ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private PlotModel privateModel;

        public PlotModel Model
        {
            get
            {
                return this.privateModel;
            }
            set
            {
                this.privateModel = value;
                NotifyPropertyChanged("Model");
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
    

        public MainViewModel()
        {
            // Create the plot model
            var tmp = new PlotModel { Title = "Spectrum Annotation", Subtitle = "using OxyPlot" };

            // Set the Model property, the INotifyPropertyChanged event will make the WPF Plot control update its content
            this.Model = tmp;
        }

        //Just draw an scan w/o annotation
        public void UpdateScanModel(MsDataScan MsScanForDraw)
        {
            this.Model = DrawScan(MsScanForDraw);
        }

        public PlotModel DrawScan(MsDataScan MsScanForDraw)
        {
            var x = MsScanForDraw.MassSpectrum.XArray;
            var y = MsScanForDraw.MassSpectrum.YArray;

            string scanNum = MsScanForDraw.OneBasedScanNumber.ToString();


            PlotModel model = new PlotModel { Title = "Spectrum anotation of Scan " + scanNum, DefaultFontSize = 15 };
            model.Axes.Add(new LinearAxis {
                Position = AxisPosition.Bottom,
                Title = "m/z",
                Minimum = 0,
                Maximum = x.Max() * 1.02,
                AbsoluteMinimum = 0,
                AbsoluteMaximum = x.Max() * 1.02
            });
            model.Axes.Add(new LinearAxis {
                Position = AxisPosition.Left,
                Title = "Intensity(counts)",
                Minimum = 0,
                Maximum = y.Max() * 1.2,
                AbsoluteMinimum = 0,
                AbsoluteMaximum = y.Max() * 1.2
            });

            LineSeries[] s0 = new LineSeries[x.Length];
            LineSeries[] s1 = new LineSeries[x.Length];
            LineSeries[] s2 = new LineSeries[x.Length];

            //Draw the ms/ms scan peaks
            for (int i = 0; i < x.Length; i++)
            {
                s0[i] = new LineSeries();
                s0[i].Color = OxyColors.Black;
                s0[i].StrokeThickness = 1;
                s0[i].Points.Add(new DataPoint(x[i], 0));
                s0[i].Points.Add(new DataPoint(x[i], y[i]));
                model.Series.Add(s0[i]);
            }
            model.Axes[0].AxisChanged += XAxisChanged;
            // Set the Model property, the INotifyPropertyChanged event will make the WPF Plot control update its content
            return model;

        }

        public void ResetViewModel()
        {
            // Create the plot model
            var tmp = new PlotModel { Title = "Spectrum Annotation", Subtitle = "using OxyPlot" };

            // Set the Model property, the INotifyPropertyChanged event will make the WPF Plot control update its content
            this.Model = tmp;
        }

        private void XAxisChanged(object sender, AxisChangedEventArgs e)
        {
            double fold = (this.Model.Axes[0].ActualMaximum - this.Model.Axes[0].ActualMinimum) / (this.Model.Axes[0].AbsoluteMaximum - this.Model.Axes[0].AbsoluteMinimum);
            this.Model.Axes[1].Minimum = 0;
            this.Model.Axes[1].Maximum = this.Model.Axes[1].AbsoluteMaximum * 0.6 * fold;

            foreach (var series in this.Model.Series)
            {
                if (series is LineSeries)
                {
                    var x = (LineSeries)series;
                    if (x.Points[1].X >= this.Model.Axes[0].ActualMinimum && x.Points[1].X <= this.Model.Axes[0].ActualMaximum)
                    {
                        if (x.Points[1].Y > this.Model.Axes[1].Maximum)
                        {
                            this.Model.Axes[1].Maximum = x.Points[1].Y * 1.2;
                        }
                    }

                }
            }
        }
    }
}
