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
        public void GetMeasureDependsTest()
        {
            var orgModel = new TabularModelHandler("dss2.bim");
            var measures = orgModel.Model.AllMeasures.ToList();
            //Assert.AreEqual(58, measures.Count);
            //string measureName = "Distinct Count Sales Orders";
            //var measure = measures.Where(m => m.Name == measureName).FirstOrDefault();
            //Assert.IsNotNull(measure);
            //Assert.AreEqual(measureName, measure.Name);
            //Assert.AreEqual("[Reseller Distinct Count Sales Order] + [Internet Distinct Count Sales Order]", measure.Expression);
            //Assert.AreEqual("Sales Territory", measure.Table.Name);
            //var depends = measure.DependsOn;
            //var relatedMeasures = depends.Measures.ToList();
            //Assert.AreEqual(2, relatedMeasures.Count);
            //Assert.AreEqual("Reseller Sales", relatedMeasures[0].Table.Name);
            //Assert.AreEqual("Internet Sales", relatedMeasures[1].Table.Name);

            //var refBy = relatedMeasures[0].ReferencedBy; //measureName = "Distinct Count Sales Orders";
            //Assert.AreEqual(1, refBy.Count);

            foreach(var m in measures)
            {
                PrintMeasure(m);
                TestContext.WriteLine($"\nMeasure ===> {m.Expression}");
                
                foreach(var dm in m.DependsOn.Measures)
                {
                    TestContext.WriteLine($"Measures: {dm.Name}");
                }

                foreach (var dm in m.DependsOn.Tables)
                {
                    TestContext.WriteLine($"Tables: {dm.Name}");
                }

                foreach (var dm in m.DependsOn.Columns)
                {
                    TestContext.WriteLine($"Columns: [{dm.Table.Name}].[{dm.Name}]");
                }

                if((m.DependsOn.Measures?.ToArray().Length > 0 || m.DependsOn.Columns?.ToArray().Length > 0)
                    && m.DependsOn.Tables?.ToArray().Length == 0 )
                {
                    TestContext.WriteLine("Expanded measure expression:");
                }
            }
        }

        private void PrintMeasure(Measure measure)
        {
            TestContext.WriteLine($"\n{measure.Name} := {measure.Expression}");
            if (measure.DependsOn.Measures?.ToArray().Length == 0 &&
                    measure.DependsOn.Columns?.ToArray().Length == 1)
            {
                string exp = measure.Expression.Replace($"[{measure.DependsOn.Columns.ToArray()[0].Name}]",
                    $"[{measure.DependsOn.Columns.ToArray()[0].Table.Name}].[{measure.DependsOn.Columns.ToArray()[0].Name}]");

                TestContext.WriteLine($"<===> {exp}");
            }

            if (measure.DependsOn.Measures?.ToArray().Length > 0)
            {
                foreach(var m in measure.DependsOn.Measures)
                {
                    PrintMeasure(m);
                }
            }
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
            var orgModel = new TabularModelHandler("dss2.bim");
            var columns = orgModel.Model.AllColumns.ToList();
            //Assert.AreEqual(203, columns.Count);
            foreach (var col in columns)
            {
                TestContext.WriteLine($"[{col.Table.Name}].[{col.Name}] ({col.DataType})");
            }
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

        [TestMethod]
        public void GetRelatedTablesFromColumnsTest()
        {
            var orgModel = new TabularModelHandler("dss2.bim");
            var columns = orgModel.Model.AllColumns.ToList();
            List<Column> selectedColumns = new List<Column>();
            List<Measure> selectedMeasures = new List<Measure>();
            HashSet<Table> selectedTables = new HashSet<Table>();

            var booked_Accounts = columns.Where(c => c.Name == "Booked_Accounts").First(); // orig
            Assert.IsNotNull(booked_Accounts);
            selectedColumns.Add(booked_Accounts);
            selectedTables.Add(booked_Accounts.Table);

            var yearMonth = columns.Where(c => c.Name == "YearMonth").First(); // date
            Assert.IsNotNull(yearMonth);
            selectedColumns.Add(yearMonth);
            selectedTables.Add(yearMonth.Table);

            var lc_group = columns.Where(c => c.Name == "Life_Cycle_Group").First(); // groupcontrol
            Assert.IsNotNull(lc_group);
            selectedColumns.Add(lc_group);
            selectedTables.Add(lc_group.Table);

            var month = columns.Where(c => c.Name == "Month" && c.Table.Name != "Test_Date").First(); // vint
            Assert.IsNotNull(month);
            Assert.AreEqual("Test_Vint", month.Table.Name);
            selectedColumns.Add(month);
            selectedTables.Add(month.Table);

            var pd2 = orgModel.Model.AllMeasures.Where(m => m.Name == "PD2").First();
            Assert.IsNotNull(pd2);
            selectedMeasures.Add(pd2);
            selectedTables.Add(pd2.Table);

            var relationships = orgModel.Model.Relationships;
            List<Relationship> relatedRelaships = new List<Relationship>();

            HashSet<Table> loopTables = new HashSet<Table>(selectedTables);
            Table factTable = pd2.Table;

            // remove fact table first
            loopTables.Remove(factTable);
            foreach (var t in selectedTables)
            {
                foreach(var r in relationships)
                {
                    if(r.FromTable == factTable)
                    {
                        loopTables.Remove(r.ToTable);
                    }
                }
            }
            // all direct tables are removed. remain tables at least one hub away
        }
    }
}
