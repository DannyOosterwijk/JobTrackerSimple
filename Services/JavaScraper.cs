using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Manager;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleJobTracker.Services
{
    public class JavaScraper
    {
        private readonly IWebDriver _driver;

        public JavaScraper()
        {
            //Setup chrome in headless mode 
            var options = new ChromeOptions();
            options.AddArgument("--headless");
            _driver = new ChromeDriver(options);
        }

        //Get the html document from the url
        public async Task<HtmlDocument> GetHtmlAsync(string url)
        {
            try
            {
                await Task.Run(() => _driver.Navigate().GoToUrl(url)); //Move the chrome driver to the url page

                var htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(_driver.PageSource); //turn the page into a html document
                return htmlDocument;
            }
            catch (Exception ex) 
            {
                Console.WriteLine($"Error: Could not fetch html data with url: {ex.Message}");
                return null;
            }
        }

        //Clean up the driver
        public void Dispose()
        {
            _driver?.Quit();
            _driver?.Dispose();
        }


        //Goes to the website and returns the amount of texts that contain at least one of the tags
        public async Task<int> CheckWeb(string url, string[] tags)
        {
            int amount = 0; //amount text that contain a tag
            try
            {
                if (url != "" && tags.Length != 0 && tags[0] != "") //Skip if url is empty or no tags are selected
                {
                    for (int i = 0; i < tags.Length; i++) //make sure all tags are lower-case
                    {
                        tags[i] = tags[i].ToLower().Trim();
                    }

                    //Get the body node from the web page
                    var htmlDoc = await GetHtmlAsync(url);
                    var body = htmlDoc.QuerySelector("body");

                    string debugString = ""; //stores all text from the web page for debugging
                    CheckNode(body, tags, ref amount, ref debugString);

                    //Get path and filename for debug file
                    string fileName = "DebugPage.txt";
                    var dirInfo = System.IO.Directory.CreateDirectory(Environment.CurrentDirectory + "/Saves");
                    string path = Path.Combine(Environment.CurrentDirectory + "/Saves", fileName);

                    File.WriteAllText(path, debugString);//create a debug file to more easily read html data
                }
                else
                {
                    throw new Exception("Invalid url or no tags selected");
                }
            }
            catch (Exception ex) 
            {
                Console.WriteLine("ERROR: " + ex.Message);
                throw new Exception(ex.Message);
            }

            return amount;
        }

        //Go down the hierarchy of the node adding all text to the debug string and raise amount for every instance of a tag
        private void CheckNode(HtmlNode node, string[] tags, ref int amount, ref string debugString)
        {
            var children = node.ChildNodes;

            //Filter out all unrelevant nodes from the list
            var FilteredChildren = new List<HtmlNode>();
            foreach (var child in children)
            {
                if (child.Name != "#text" && !child.Name.Contains("script") && child.Name != "#comment" && child.Name != "header" && child.Name != "footer")
                {
                    FilteredChildren.Add(child);
                }
            }

            //check if there are still relevant child nodes
            if (FilteredChildren.Count > 0)
            {
                //for every relevant child, check their descendants
                foreach (var filterChild in FilteredChildren)
                {
                    CheckNode(filterChild, tags, ref amount, ref debugString);
                }
            }
            else //if at the end of the hierarchy
            {
                var htmlText = node.InnerText; //get text from current node
                if (htmlText != "")
                {
                    debugString += htmlText + "\n\n"; //add text to the debug text
                    
                    //check if text contains one of the tags
                    htmlText = htmlText.ToLower();
                    foreach (var tag in tags)
                    {
                        if (htmlText.Contains(tag))
                        {
                            amount++;
                            break;
                        }
                    }
                }


            }
        }
    }
}
