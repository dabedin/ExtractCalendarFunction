using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using HtmlAgilityPack;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace Company.Function
{
    public class TrashCalendar
    {
        public DateTime Date { get; set; }
        public bool Umido { get; set; }
        public bool Vegetale { get; set; }
        public bool Carta { get; set; }
        public bool VPL { get; set; }
        public bool Secco { get; set; }
    }
    public static class ExtractCalendar
    {
        [FunctionName("ExtractCalendar")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            // with LINQ from https://www.scrapingbee.com/blog/html-agility-pack/
            // with XPATH from https://stackoverflow.com/a/36711885
            var url = @"https://contarina.it/cittadino/raccolta-differenziata/eco-calendario";
            HtmlWeb web = new HtmlWeb();
            var doc = web.Load(url);
            
            var results = new List<TrashCalendar>();

            // from https://zetcode.com/csharp/datetime-parse/
            Regex rgxDate = new Regex(@"\d{2}-\d{2}-\d{4}");
            // from https://stackoverflow.com/a/14918404
            Regex rgxUmido = new Regex(nameof(TrashCalendar.Umido), RegexOptions.IgnoreCase);
            Regex rgxVegetale = new Regex(nameof(TrashCalendar.Vegetale), RegexOptions.IgnoreCase);
            Regex rgxSecco = new Regex(nameof(TrashCalendar.Secco), RegexOptions.IgnoreCase);
            Regex rgxVPL = new Regex(nameof(TrashCalendar.VPL), RegexOptions.IgnoreCase);
            Regex rgxCarta = new Regex(nameof(TrashCalendar.Carta), RegexOptions.IgnoreCase);

            // all nodes with class comune_zona_38
            //var nodes = doc.DocumentNode.SelectNodes("//table[contains(@class, 'comune_zona_38')]//tr");
            // all tr of table with id 
            var nodes = doc.DocumentNode.SelectNodes("//table[@id='svuotamenti_38']//tr");      
            foreach (var node in nodes)
            {
                var innerText = node.InnerText;
                                
                // from https://stackoverflow.com/a/14918404
                
                Match mat = rgxDate.Match(innerText);
                //if it has a date is a calendar row
                if (mat.Success)
                {
                    var calendar = new TrashCalendar();
                    
                    var date = DateTime.ParseExact(mat.Value, "dd-MM-yyyy", null);
                    calendar.Date = date;
                    Console.WriteLine(date.ToLongDateString());

                    if (rgxUmido.IsMatch(innerText))
                    {
                        calendar.Umido = true;
                    }
                    if (rgxVegetale.IsMatch(innerText))
                    {
                        calendar.Vegetale = true;
                    }
                    if (rgxSecco.IsMatch(innerText))
                    {
                        calendar.Secco = true;
                    }
                    if (rgxVPL.IsMatch(innerText))
                    {
                        calendar.VPL = true;
                    }  
                    if (rgxCarta.IsMatch(innerText))
                    {
                        calendar.Carta = true;
                    }
                    results.Add(calendar);
                }
            }
            
            string responseMessage = string.IsNullOrEmpty(name)
                ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                : $"Hello, {name}. This HTTP triggered function executed successfully.";

            return new OkObjectResult(results);
        }
    }
}
