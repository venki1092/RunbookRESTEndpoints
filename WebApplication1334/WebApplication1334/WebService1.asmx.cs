using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using System.Web.Services;
using System.Web.Script.Services;
using System.Web.Script.Serialization;
using System.Diagnostics;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Text;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using System.Threading.Tasks;

namespace WebApplication1334
{
    /// <summary>
    /// Summary description for WebService1
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    // [System.Web.Script.Services.ScriptService]
    public class WebService1 : System.Web.Services.WebService
    {

        const int deleteToken = 1421;
        const long threshold = Int64.MaxValue / 2;
        [WebMethod]
        public Employee[] GetEmployessXML()
        {
            Employee[] emps = new Employee[] {
            new Employee()
            {
                Id=101,
                Name="Nitin",
                Salary=10000
            },
            new Employee()
            {
                Id=102,
                Name="Dinesh",
                Salary=100000
            }
        };
            return emps;
        }
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json, UseHttpGet = true)]
        public string GetsmartchairJSON()
        {
            MongoCount mc = new MongoCount();
            
            var count = testCount();
            if (count > threshold)
                mc.countThreshold = "true";
            else
                mc.countThreshold = "false";
            
            sendMQTTMessage(count);
            return new JavaScriptSerializer().Serialize(mc);
        }
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json, UseHttpGet = true)]
        public string DeleteExcessData()
        {
            mongoDelete();
            sendMQTTMessage(deleteToken);
            return "{ deleteStatus: true}"; //token to return for get message
        }
        public async void test()
        {
            IMongoClient _client;
            IMongoDatabase _database;
            _client = new MongoClient();
            _database = _client.GetDatabase("irrigate");
            var collection = _database.GetCollection<BsonDocument>("Venkatesh");
            var filter = new BsonDocument();
            collection.Count(filter);
            var count = 0;
            using (var cursor = await collection.FindAsync(filter))
            {
                while (await cursor.MoveNextAsync())
                {
                    var batch = cursor.Current;
                    foreach (var document in batch)
                    {
                        Console.WriteLine(document.ToJson());
                        // process document
                        count++;
                    }
                }
            }
        }
        void mongoDelete()
        {
            var time = DateTime.UtcNow;
            time.AddDays(-7);
            var query = new QueryDocument
            {
               { "date", time}
            };
        
            var conString = "mongodb://54.213.131.219:27018";
            var Client = new MongoClient(conString);
            var DB = Client.GetDatabase("smartchairdb");
            var collection = DB.GetCollection<BsonDocument>("smartchairdataset");

            //deleting multiple record
            var DelMultiple = collection.DeleteMany(
                             Builders<BsonDocument>.Filter.Lt("date", time));
        }
        public long testCount()
        {
            IMongoClient _client;
            IMongoDatabase _database;
            var conString = "mongodb://54.213.131.219:27018";
            _client = new MongoClient(conString);
            _database = _client.GetDatabase("smartchairdb");
            var collection = _database.GetCollection<BsonDocument>("smartchairdataset");
            var filter = new BsonDocument();
            var count = collection.Count(filter);
            return count;
        }

        public void sendMQTTMessage(long count)
        {
            MqttClient client = new MqttClient("54.213.131.219", 2882, false, null, null, MqttSslProtocols.None, null);
            string clientId = Guid.NewGuid().ToString();
            client.Connect(clientId);
            string strValue = "Incoming Request";
            client.Publish("debugPrint", Encoding.UTF8.GetBytes(strValue +" " + count));
        }
    }
}
