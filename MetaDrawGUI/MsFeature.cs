﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaDrawGUI
{
    public class MsFeature
    {
        public MsFeature(string line, char[] split, Dictionary<string, int> parsedHeader)
        {
            generateMsFeatures(line, split, parsedHeader);
        }

        public int id { get; set; }
        public double MonoMass { get; set; }
        public double Abundance { get; set; }
        public double ApexRT { get; set; }

        private void generateMsFeatures(string line, char[] split, Dictionary<string, int> parsedHeader)
        {
            var spl = line.Split(split);

            MonoMass = double.Parse(spl[parsedHeader[TsvHeader_MsFeature.monoMass]]);
            Abundance = double.Parse(spl[parsedHeader[TsvHeader_MsFeature.abundance]]);
            ApexRT = double.Parse(spl[parsedHeader[TsvHeader_MsFeature.apexRT]]);

        }
    }
}
