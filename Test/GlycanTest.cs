using Chemistry;
using EngineLayer;
using EngineLayer.CrosslinkSearch;
using EngineLayer.CrosslinkAnalysis;
using EngineLayer.Indexing;
using MassSpectrometry;
using NUnit.Framework;
using Proteomics;
using System.Collections.Generic;
using System.Linq;
using TaskLayer;
using UsefulProteomicsDatabases;
using Nett;
using System;
using System.IO;
using MetaDrawGUI;

namespace Test
{
    [TestFixture]
    public static class GlycanTest
    {
        [Test]
        public static void GlycanTest_CalculateGlycan()
        {
            var t = Glycan.CalculateGlycan("(N(F)(N(H(H)(H(H)))))");

        }
    }
}
