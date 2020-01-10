using AibiSoft.Data.Metadata;
using AibiSoft.Data.PivotTable;
using AibiSoft.Data.StarSchema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using TabularEditor.TOMWrapper;

namespace AibiSoft.Data.Tests
{
    [TestClass]
    public class TOMBuilderTest
    {
        [TestMethod]
        public void SchemaTableTest1()
        {
            string connStr = "data source=localhost;initial catalog=DSS2;Integrated Security=sspi";
            var dbConnectin = new DatabaseConnectionManager(connStr, ADatabaseMetadataDiscovery.SQLServer);

            var databases = dbConnectin.Databases;
            var db = databases["DSS2"];
            
            var builder = new TOMBuilder();
            // get model
            var model = builder.Model;

            // datasource
            var ds = model.AddDataSource("My data source name");
            ds.ConnectionString = connStr;
            ds.Provider = "System.Data.SqlClient";
            ds.Password = "xyz123";
            ds.ImpersonationMode = TabularEditor.TOMWrapper.ImpersonationMode.ImpersonateAccount;
            // add tables
            builder.AddTable0(db.Tables.Where(t => t.Name == "Test_Vint").First());
            builder.AddTable0(db.Tables.Where(t => t.Name == "Test_Orig").First());
            builder.AddTable0(db.Tables.Where(t => t.Name == "Test_GroupControl").First());
            builder.AddTable0(db.Tables.Where(t => t.Name == "Test_Date").First());
            
            // add relationships

            // vint.origination_id => orig.id
            var vint_fk_origination_id = model.Tables["Test_Vint"].Columns["Origination_ID"];
            var orig_pk_id = model.Tables["Test_Orig"].Columns["ID"];
            vint_fk_origination_id.RelateTo(orig_pk_id);
            // orig.groupcontrol_id => groupcontrol.id
            var orig_fk_groupcontrol_id = model.Tables["Test_Orig"].Columns["GroupControl_ID"];
            var groupcontrol_pk_id = model.Tables["Test_GroupControl"].Columns["ID"];
            orig_fk_groupcontrol_id.RelateTo(groupcontrol_pk_id);
            // vint.month => date.DateKey
            var vint_fk_month = model.Tables["Test_Vint"].Columns["Month"];
            var date_datekey = model.Tables["Test_Date"].Columns["DateKey"];
            vint_fk_month.RelateTo(date_datekey);
            // orig.vintage_date => date.DateKey
            var vint_fk_vintage_date = model.Tables["Test_Orig"].Columns["Vintage_Date"];
            /*
            
            vint_fk_month.RelateTo(date_datekey);
            */
            var rel = model.AddRelationship();
            rel.FromColumn = vint_fk_vintage_date;
            rel.ToColumn = date_datekey;

            // add sum over sum measures
            model.Tables["Test_Vint"].AddMeasure("PD2", "[Sum_of_Default_Accounts]/[Sum_of_Booked_Accounts]");
            model.Tables["Test_Vint"].AddMeasure("AD2", "Sum('Test_Vint'[Active_Accounts])/Sum('Test_Vint'[Default_Accounts])");

            builder.Save("dsstest.json");

        }

        [TestMethod]
        public void RelationshipTest()
        {
            var builder = new TOMBuilder("dsstest.json");
            var model = builder.Model;
            Assert.IsTrue(model.AllMeasures.Count() > 5);

            // get from and to tables from relationships
            var rels = model.Relationships;
            var fromTables = rels.Select(r => r.FromTable).Distinct().ToList();
            var toTables = rels.Select(r => r.ToTable).Distinct().ToList();

            // get tables from measure
            var pd2 = model.AllMeasures.Where(m => m.Name == "PD2").FirstOrDefault();
            var pd2table = pd2.Table;
            Assert.AreEqual("Test_Vint", pd2table.Name);
            var pd2ReferencedByList = pd2.ReferencedBy;
            var isSimpleMeasure = pd2.IsSimpleMeasure;
            Assert.IsFalse(isSimpleMeasure);
            var dependsOn = pd2.DependsOn;
            Assert.AreEqual(2, dependsOn.Count);
            Assert.AreEqual(0, dependsOn.Columns.Count());
            var dependsOnKeys = dependsOn.Keys;
            Assert.AreEqual(2, dependsOnKeys.Count());
            var keyNames = dependsOnKeys.Select(k => k.DaxObjectName).ToList();
            Assert.AreEqual(2, keyNames.Count);
            // key1
            IDaxObject k1 = dependsOnKeys.Where(k=>k.DaxObjectName == keyNames[0]).First();
            Assert.IsTrue(k1 is Measure);
            var m1 = k1 as Measure;
            Assert.IsNotNull(m1);
            Assert.AreEqual(1, m1.DependsOn.Columns.Count());
            var m1DepCol = m1.DependsOn.Columns.First();
            Assert.AreEqual("Default_Accounts", m1DepCol.Name);
            Assert.AreEqual("Test_Vint", m1DepCol.Table.Name);

            IDaxObject k2 = dependsOnKeys.Where(k => k.DaxObjectName == keyNames[1]).First();
            Assert.IsTrue(k2 is Measure);
            var m2 = k2 as Measure;
            Assert.IsNotNull(m2);
            Assert.AreEqual(1, m2.DependsOn.Columns.Count());
            var m2DepCol = m2.DependsOn.Columns.First();
            Assert.AreEqual("Booked_Accounts", m2DepCol.Name);
            Assert.AreEqual("Test_Orig", m2DepCol.Table.Name);
        }

        [TestMethod]
        public void ShortestPathTest()
        {
            var model = new TOMBuilder("dsstest.json").Model;

            var fromTable = model.Tables["Test_Vint"];
            var toTable = model.Tables["Test_Date"];
            Assert.IsNotNull(fromTable);
            Assert.IsNotNull(toTable);

            var shortpath = GetShortestPath(fromTable, toTable, model.Relationships.ToList());
            Assert.AreEqual(1, shortpath.Count);
            Assert.AreEqual("Test_Vint", shortpath[0].FromTable.Name);
            Assert.AreEqual("Month", shortpath[0].FromColumn.Name);
            Assert.AreEqual("Test_Date", shortpath[0].ToTable.Name);
            Assert.AreEqual("DateKey", shortpath[0].ToColumn.Name);

            toTable = model.Tables["Test_GroupControl"];
            shortpath = GetShortestPath(fromTable, toTable, model.Relationships.ToList());
            Assert.AreEqual(2, shortpath.Count);
            Assert.AreEqual("Test_Vint", shortpath[1].FromTable.Name);
            Assert.AreEqual("Origination_ID", shortpath[1].FromColumn.Name);
            Assert.AreEqual("Test_GroupControl", shortpath[0].ToTable.Name);
            Assert.AreEqual("ID", shortpath[0].ToColumn.Name);
        }

        private List<SingleColumnRelationship> GetShortestPath(Table from, Table to, List<SingleColumnRelationship> relationships, List<SingleColumnRelationship> path = null)
        {
            path = path ?? new List<SingleColumnRelationship>();
            var rel = relationships.Where(r => r.FromTable.DaxObjectFullName == from.DaxObjectFullName
            && r.ToTable.DaxObjectFullName == to.DaxObjectFullName).FirstOrDefault();
            if (rel != null) // found
            {
                path.Add(rel);
            }
            else
            {
                // breadth-first
                // relationships that exists with "the from" table
                var rels = relationships.Where(r => r.FromTable.DaxObjectFullName != from.DaxObjectFullName).ToList();
                foreach(var r in rels)
                {
                    var shorttestPath = GetShortestPath(r.FromTable, to, rels, path);
                    if(shorttestPath.Count > 0) // found
                    {
                        var fromRel = relationships.Where(x => x.FromTable.DaxObjectFullName == from.DaxObjectFullName
                        && x.ToTable.DaxObjectFullName == r.FromTable.DaxObjectFullName).First();
                        path.Add(fromRel);
                        break;
                    } 
                }
            }

            return path;
        }
    }
}
