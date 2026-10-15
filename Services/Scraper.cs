using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace SimpleJobTracker.Models
{
    public class Scraper
    {
        private readonly HttpClient _httpClient;

        public Scraper()
        {
            //setup http client as a user agent
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Checking for jobs");
        }

        //Get the html document from the url
        public async Task<HtmlDocument> GetHtmlAsync(string url)
        {
            try
            {
                string htmlString = await _httpClient.GetStringAsync(url); //Try to get html text
                var htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(htmlString); // load the html text into a html document
                return htmlDocument;
            }
            catch (Exception ex) 
            { 
                Console.WriteLine($"Error: Could not fetch html data with url: { ex.Message }");
                return null;
            }
        }

        //check if the web page has any scripts that dynamically load content to the page
        public bool DoesContainDynamicContent(HtmlDocument document)
        {
            var scripts = document.DocumentNode.SelectNodes("//script"); // get all script nodes
            if(scripts == null) return false;

            //check if script nodes contain frequently used types to dynamically load data
            return scripts.Any(script => script.InnerHtml.Contains("ajax") || script.InnerHtml.Contains("XMLHttpRequest") || script.InnerHtml.Contains("fetch("));
        }
    }
}
