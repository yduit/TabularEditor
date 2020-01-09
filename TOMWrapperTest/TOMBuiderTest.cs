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
            var table = db.Tables.Where(t=>t.Name == "Test_Vint").First();
            var builder = new TOMBuilder();
            builder.AddTable(table);

            //var model = new StarModel("select * from [Alphabetical list of products]");
            //model.Init(dbConnection);
        }
    }
}
