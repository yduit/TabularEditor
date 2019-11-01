using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TabularEditor.TOMWrapper;

namespace TOMWrapperTest
{
    [TestClass]
    public class ModelMetadataTests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void DataSourceTest()
        {
            var orgModel = new TabularModelHandler("AdventureWorks2.bim");
            var datasources = orgModel.Model.DataSources;
            TestContext.WriteLine("Data Sources:");
            foreach(var ds in datasources)
            {
                var pds = ds as ProviderDataSource;
                if (pds != null)
                {
                    TestContext.WriteLine($"name: {pds.Name}");
                    TestContext.WriteLine($"connectionString: {pds.ConnectionString}");
                    TestContext.WriteLine($"provider: {pds.Provider}");
                }
            }
        }
        [TestMethod]
        public void TableListTest()
        {
            var orgModel = new TabularModelHandler("AdventureWorks2.bim");
            var tables = orgModel.Model.Tables;
            Assert.AreEqual(14, tables.Count);
            TestContext.WriteLine("Tables having measures:");
            foreach(var t in tables)
            {
                if(t.Measures?.Count > 0)
                {
                    TestContext.WriteLine($"Table [{t.Name}] has {t.Measures.Count} measures");
                }
            }
        }

        [TestMethod]
        public void MeasureListTest()
        {
            var orgModel = new TabularModelHandler("AdventureWorks2.bim");
            var measures = orgModel.Model.AllMeasures.ToList();
            Assert.AreEqual(58, measures.Count);
        }

        [TestMethod]
        public void GetMeasureByNameTest()
        {
            var orgModel = new TabularModelHandler("AdventureWorks2.bim");
            var measures = orgModel.Model.AllMeasures.ToList();
            Assert.AreEqual(58, measures.Count);
            string measureName = "Distinct Count Sales Orders";
            var measure = measures.Where(m => m.Name == measureName).FirstOrDefault();
            Assert.IsNotNull(measure);
            Assert.AreEqual(measureName, measure.Name);
            Assert.AreEqual("[Reseller Distinct Count Sales Order] + [Internet Distinct Count Sales Order]", measure.Expression);
            Assert.AreEqual("Sales Territory", measure.Table.Name);
            var depends = measure.DependsOn;
            var relatedMeasures= depends.Measures.ToList();
            Assert.AreEqual(2, relatedMeasures.Count);
            Assert.AreEqual("Reseller Sales", relatedMeasures[0].Table.Name);
            Assert.AreEqual("Internet Sales", relatedMeasures[1].Table.Name);

            var refBy = relatedMeasures[0].ReferencedBy; //measureName = "Distinct Count Sales Orders";
            Assert.AreEqual(1, refBy.Count);
        }

        [TestMethod]
        public void GetRelatedTablesInMeasureTest()
        {
            var orgModel = new TabularModelHandler("AdventureWorks2.bim");
            var measures = orgModel.Model.AllMeasures.ToList();
            Assert.AreEqual(58, measures.Count);
            string measureName = "Distinct Count Sales Orders";
            var measure = measures.Where(m => m.Name == measureName).FirstOrDefault();
            Assert.IsNotNull(measure);
            Assert.AreEqual(measureName, measure.Name);
            Assert.AreEqual("[Reseller Distinct Count Sales Order] + [Internet Distinct Count Sales Order]", measure.Expression);
            Assert.AreEqual("Sales Territory", measure.Table.Name);

            // process expression to leaf level measure
            //var regx = new Regex(@"(?<func1>[a-z]+)\(\[(?<numerator>.+)\]\)/(?<func2>[a-z]+)\(\[(?<denominator>.+)\]\)", RegexOptions.IgnoreCase);

            var regx2 = new Regex(@"('.*?')?\[.*?\]", RegexOptions.IgnoreCase); // or: @"\[[^]]+\]"  @"\[.*?\]"

            foreach (var m in measures)
            {

                MatchCollection matches = regx2.Matches(m.Expression);
                if (matches.Count > 2)
                {
                    var exprSet = new HashSet<string>();
                    foreach(Match ma in matches)
                    {
                        exprSet.Add(ma.Value);
                        TestContext.WriteLine($"{m.Name} => {ma.Value}");
                    }
                }
            }
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
