using AibiSoft.Data.Metadata;
using AibiSoft.Data.PivotTable;
using AibiSoft.Data.StarSchema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

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
            builder.AddTable(db.Tables.Where(t => t.Name == "Test_Vint").First());
            builder.AddTable(db.Tables.Where(t => t.Name == "Test_Orig").First());
            builder.AddTable(db.Tables.Where(t => t.Name == "Test_GroupControl").First());
            builder.AddTable(db.Tables.Where(t => t.Name == "Test_Date").First());
            
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
            //var model = new StarModel("select * from [Alphabetical list of products]");
            //model.Init(dbConnection);
        }

        [TestMethod]
        public void RelationshipTest()
        {
            var builder = new TOMBuilder("dsstest.json");
            var model = builder.Model;
            Assert.IsTrue(model.AllMeasures.Count() > 5);
        }
    }
}
