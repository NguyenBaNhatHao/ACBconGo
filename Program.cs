﻿using System.Collections.Specialized;
using System.Net.Http.Headers;
using System.Text;
using ACBconGo.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data.SqlClient;

DotNetEnv.Env.Load();
HttpClient client = new HttpClient();
USDTTransactions uSDTTransactions = new USDTTransactions();
var server = Environment.GetEnvironmentVariable("server");
var Database = Environment.GetEnvironmentVariable("Database");
var UserId = Environment.GetEnvironmentVariable("UserId");
var password = Environment.GetEnvironmentVariable("password");

var connectionString="Data Source="+server+";Initial Catalog="+Database+";User Id="+UserId+";Password="+password+";";
var ApiKey = Environment.GetEnvironmentVariable("apiKey");
var walletId = Environment.GetEnvironmentVariable("walletId");
var transactionsID = String.Empty;
client.DefaultRequestHeaders.Accept.Clear();
client.DefaultRequestHeaders.Accept.Add(
    new MediaTypeWithQualityHeaderValue("application/json"));
client.DefaultRequestHeaders.Add("X-API-Key",ApiKey);
await GetAccessTransaction(client,walletId,connectionString,uSDTTransactions);

static async Task GetAccessTransaction(HttpClient client,string? walletId,string? connectionString,USDTTransactions uSDTTransactions){
    HttpResponseMessage httpResponse = client.GetAsync("https://rest.cryptoapis.io/wallet-as-a-service/wallets/"+walletId+"/tron/nile/transactions?context=yourExampleString&limit=50&offset=0").GetAwaiter().GetResult();
    httpResponse.EnsureSuccessStatusCode(); 
    var responseString = await httpResponse.Content.ReadAsStringAsync();
    JObject jObject = JObject.Parse(responseString);
    var direction = jObject["data"]["items"];
    foreach(var item in direction){
        if(item["direction"].Value<string>().Equals("outgoing") && item["fungibleTokens"].HasValues == true){
            uSDTTransactions.Address = item["fungibleTokens"][0]["recipient"].Value<string>();
            uSDTTransactions.Account = uSDTTransactions.Address;
            uSDTTransactions.Amount = item["fungibleTokens"][0]["amount"].Value<double>().ToString();
            uSDTTransactions.TransactionId = item["transactionId"].Value<string>();
            var TransactionHash = await GetHashTransaction(client,uSDTTransactions);
            uSDTTransactions.TransactionHash = TransactionHash;
            Console.WriteLine("Address: "+uSDTTransactions.Address);
            Console.WriteLine("Account: "+uSDTTransactions.Account);
            Console.WriteLine("Amount: "+uSDTTransactions.Amount);
            Console.WriteLine("TransactionHash: "+uSDTTransactions.TransactionHash);
            Console.WriteLine();
            var pAddress = new SqlParameter("@pAddress", uSDTTransactions.Address);
            var pAccount = new SqlParameter("@pAccount", uSDTTransactions.Account);
            var pTransactionHash = new SqlParameter("@pTransactionHash", uSDTTransactions.TransactionHash);
            var pAmount = new SqlParameter("@pAmount", uSDTTransactions.Amount);

            using (var conn = new SqlConnection(connectionString))
            using (var command = new SqlCommand("usp_USDTTransactions_Insert", conn) { 
                                    CommandType = System.Data.CommandType.StoredProcedure }) {
                                    command.Parameters.Add(pAddress);
                                    command.Parameters.Add(pAccount);
                                    command.Parameters.Add(pTransactionHash);
                                    command.Parameters.Add(pAmount);
            conn.Open();
            command.ExecuteNonQuery();
            conn.Close();
            }

            
        }
        
    }
}
static async Task<string> GetHashTransaction(HttpClient client, USDTTransactions uSDTTransactions){
    HttpResponseMessage httpResponse = client.GetAsync("https://rest.cryptoapis.io/wallet-as-a-service/wallets/tron/nile/transactions/"+uSDTTransactions.TransactionId).GetAwaiter().GetResult();
    httpResponse.EnsureSuccessStatusCode(); 
    var responseString = await httpResponse.Content.ReadAsStringAsync();
    JObject jObject = JObject.Parse(responseString);
    var direction = jObject["data"]["item"];
    var TransactionHash = direction["transactionHash"].Value<string>();
    return TransactionHash;
}

 