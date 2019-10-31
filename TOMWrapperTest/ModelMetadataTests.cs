using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TabularEditor.TOMWrapper;

namespace TOMWrapperTest
{
    [TestClass]
    public class ModelMetadataTests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void TableListTest()
        {
            var orgModel = new TabularModelHandler("AdventureWorks2.bim");
            var tables = orgModel.Model.Tables;
            Assert.AreEqual(14, tables.Count);
        }

        [TestMethod]
        public void MeasureListTest()
        {
            var orgModel = new TabularModelHandler("AdventureWorks2.bim");
            var measures = orgModel.Model.AllMeasures.ToList();
            Assert.AreEqual(58, measures.Count);
        }

        [TestMethod]
        public void ColumnListTest()
        {
            var orgModel = new TabularModelHandler("AdventureWorks2.bim");
            var columns = orgModel.Model.AllColumns.ToList();
            Assert.AreEqual(203, columns.Count);
        }

        [TestMethod]
        public void RelationshipListTest()
        {
            var orgModel = new TabularModelHandler("AdventureWorks2.bim");
            var relationships = orgModel.Model.Relationships;
            Assert.AreEqual(22, relationships.Count);
            TestContext.WriteLine($"Total relationships count: {relationships.Count}");
            foreach(var r in relationships)
            {
                TestContext.WriteLine($"{r.FromTable.Name}.{r.FromColumn.Name} => {r.ToTable.Name}.{r.ToColumn.Name}");
            }
        }
    }
}
