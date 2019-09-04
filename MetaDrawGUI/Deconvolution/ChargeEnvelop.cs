using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MassSpectrometry;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using MetaDrawGUI;

namespace MassSpectrometry
{
    public class ChargeEnvelop
    {
        public List<(int charge, MzPeak peak, IsoEnvelop isoEnvelop)> distributions { get; set; } = new List<(int charge, MzPeak peak, IsoEnvelop isoEnvelop)>();

        public int FirstIndex { get; set; }

        public double FirstMz { get; set; }

        public double FirstIntensity { get; set; }

        public double Mse { get; set; }

        public double UnUsedMzsRatio { get; set; }

        public int IsoEnveNum
        {
            get
            {
                return distributions.Where(p => p.isoEnvelop != null).Count();
            }
        }

        //This MSE method cannot distinguish anything
        public void GetMSE()
        {
            var model = new GaussianModel();
            var solver = new LevenbergMarquardtSolver();
            Vector<double> dataX = new DenseVector(distributions.Select(p=>(double)p.charge).ToArray());
            //double max = distributions.Max(k => k.peak.Intensity); //This can be used to normalize intensity.
            Vector<double> dataY = new DenseVector(distributions.Select(p => p.peak.Intensity).ToArray());
            List<Vector<double>> iterations = new List<Vector<double>>();
            int pointCount = dataX.Count;
            var solverOptions = new SolverOptions(true, 0.0001, 0.0001, 1000, new DenseVector(new[] { 10.0, 10.0 }));
            NonlinearSolver nonlinearSolver = (solver as NonlinearSolver);
            nonlinearSolver.Estimate(model, solverOptions, pointCount, dataX, model.LogTransform(dataY), ref iterations);
            IntensityModel = model.GetPowerETo1ValueVector(pointCount, dataX, iterations[iterations.Count - 1]).ToArray();
            Mse = model.GetPowerEMSE(pointCount, dataX, dataY, iterations[iterations.Count - 1]);
        }

        public double[] IntensityModel { get; set; }
    }
}
