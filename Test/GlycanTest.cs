﻿using Chemistry;
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
            var t0= Glycan.ReadGlycan("(N(F)(H))");
            var t1 = Glycan.ReadGlycan("(N(F)(N(H(H)(H(H)))))");
            var t2 = Glycan.ReadGlycan("(N(F)(N(H(H))))");

            var testAllChildren = Glycan.GetAllChildrenCombination(t2);

            List<string> testString = new List<string>();
            foreach (var aNode in testAllChildren)
            {
                testString.Add(Glycan.PrintOutGlycan(aNode));
            }
            Assert.AreEqual(testString.Count, 8);

        }

        public static void GlycanTest_PrintOutGlycan()
        {
            var t1 = Glycan.ReadGlycan("(N(F)(N(H(H)(H(H)))))");

            var testNode2String = Glycan.PrintOutGlycan(t1);

            Assert.AreEqual(testNode2String, "(N(F)(N(H(H)(H(H)))))");
        }

        [Test]
        public static void GlycanTest_GetNodeMass()
        {
            var t2 = Glycan.ReadGlycan("(N(F)(N(H(H))))");
            var x = Glycan.GetNodeMass(t2);
            Assert.AreEqual(x, 876.3223);
        }

        [Test]
        public static void GlycanTest_GetAllChildrenMass()
        {
            var t2 = Glycan.ReadGlycan("(N(F)(N(H(H))))");
            var x = Glycan.GetAllChildrenMass(t2);
        }
    }
}
