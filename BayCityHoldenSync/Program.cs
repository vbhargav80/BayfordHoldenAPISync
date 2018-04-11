using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using HtmlAgilityPack;
using Newtonsoft.Json;
using NLog;
using ScrapySharp.Extensions;
using ScrapySharp.Network;
using RestSharp;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.IO;
using Microsoft.WindowsAzure.Storage.Table;

namespace BayCityHoldenSync
{
    class Program
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
        private static CloudStorageAccount csa;
        private static CloudBlobClient blobClient;
        private static CloudBlobContainer blobContainer;
        private static Dictionary<string, string> _categoryMap;
        private static int _versionNumber;

        static void Main(string[] args)
        {
            csa = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=mcv;AccountKey=aDLSHvke6wa1RY3vWmz20LooQvRCO17OnHdZbtZx2UAg78H1t8nVBcVUKxP2G78y5VN9SeoXYpWB+lEZugsC4w==;EndpointSuffix=core.windows.net");
            blobClient = csa.CreateCloudBlobClient();
            blobContainer = blobClient.GetContainerReference("stock-images");
            blobContainer.CreateIfNotExists();

            _categoryMap = new Dictionary<string, string>();
            _categoryMap.Add("UR3AT", "As Traded");
            _categoryMap.Add("URAA", "As Traded");
            _categoryMap.Add("UR3B", "Bus/People Mover");
            _categoryMap.Add("URMV", "Bus/People Mover");
            _categoryMap.Add("UWM", "Bus/People Mover");
            _categoryMap.Add("UR3PG", "Bus/People Mover");
            _categoryMap.Add("UR3D", "Light Truck");
            _categoryMap.Add("UR3R", "Refrigerated");
            _categoryMap.Add("UR3V", "Van");

            _versionNumber = GetVersionNumber();
            Logger.Debug("Starting Scraper");
            if (args.Length == 0)
                RunScraper();
            Logger.Debug("Finished Scraper");
            
        }

        private static void RunScraper()
        {
            var browser = new ScrapingBrowser();

            ServicePointManager.ServerCertificateValidationCallback +=
                (sender, cert, chain, sslPolicyErrors) => true;

            var url = "http://stock.melbournescheapestcars.com.au/cars?query=(Or.BodyStyle.Van._.BodyStyle.People%20Mover._.BodyStyle.Bus.)&sort=topdeal&limit=10&skip=0";

            var webPage = browser.NavigateToPage(new Uri(url));
            var countContainer = webPage.Html.CssSelect("div.paging__count").FirstOrDefault();
            var countText = countContainer.InnerText.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).ToList();

            var numberOfPages = int.Parse(countText[countText.Count - 1]);
            List<string> detailsPageLinks = new List<string>();
            Dictionary<string, string> imageMap = new Dictionary<string, string>();

            for (int i = 0; i < numberOfPages; i++)
            {
                var pagedUrl = string.Format("http://stock.melbournescheapestcars.com.au/cars?query=(Or.BodyStyle.Van._.BodyStyle.People%20Mover._.BodyStyle.Bus.)&sort=topdeal&limit=10&skip={0}", i * 10);
                var listingsPage = browser.NavigateToPage(new Uri(pagedUrl));
                var items = listingsPage.Html.CssSelect("ul.search-results li .search-results--vehicle h3.vehicle-title a").ToList();

                foreach (var item in items)
                {
                    try
                    {
                        string hrefValue = item.GetAttributeValue("href", string.Empty);
                        if (!string.IsNullOrEmpty(hrefValue) && !detailsPageLinks.Contains(hrefValue))
                        {
                            detailsPageLinks.Add(string.Format("http://stock.melbournescheapestcars.com.au/{0}", hrefValue));
                        }
                    }
                    catch (Exception ex)
                    {

                    }

                }
            }

            var repository = new MongoRepository();
            int j = 0;

            foreach (var item in detailsPageLinks)
            {
                ProcessItem(item, repository, ++j);
            }
        }

        private static void ProcessItem(string detailsUrl, MongoRepository repository, int i)
        {

            Logger.Info("Scraping Page {0}", detailsUrl);
            var browser = new ScrapingBrowser();
            var detailsPage = browser.NavigateToPage(new Uri(detailsUrl));

            try
            {
                var carAd = new CarAd();
                
                carAd.Title = detailsPage.Html.CssSelect("h1.details__title")?.FirstOrDefault()?.InnerText;
                carAd.Price = detailsPage.Html.CssSelect(".details__header .details__price")?.FirstOrDefault()?.InnerText
                    ?.Replace(",","");
                carAd.Comments = detailsPage.Html.CssSelect("div#tabComments > div")?.FirstOrDefault()?.InnerText?.Trim();
                carAd.Features = detailsPage.Html.CssSelect("div#standardfeatures > ul > li")
                    ?.Select(a => a.InnerText).ToList();

                var keys = detailsPage.Html.CssSelect("div#tabVehicleDetails > dl.dl-list > dt")
                    ?.Select(a => a.InnerText).ToList();
                var values = detailsPage.Html.CssSelect("div#tabVehicleDetails > dl.dl-list > dd")
                    ?.Select(a => a.InnerText).ToList();

                carAd.Images = detailsPage.Html.CssSelect("div.gallery__item > a")
                    ?.Select(a => a.GetAttributeValue("href", string.Empty)).ToList();

                
                carAd.StockNumber = values[keys.IndexOf("Stock Number")];
                carAd.Id = $"{carAd.StockNumber}";

                if (keys.Contains("Odometer"))
                {
                    var odo = values[keys.IndexOf("Odometer")]?.Replace(",", "")
                    ?.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

                    carAd.Odometer = odo[0];
                }
                    
                if (keys.Contains("Year of manufacture"))
                    carAd.Year = values[keys.IndexOf("Year of manufacture")];

                if (keys.Contains("Class"))
                    carAd.Year = values[keys.IndexOf("Class")];

                // TODO:
                carAd.Category = "";

                if (keys.Contains("Body Type"))
                    carAd.Body = values[keys.IndexOf("Body Type")];

                if (keys.Contains("Seats"))
                    carAd.Seats = values[keys.IndexOf("Seats")];

                if (keys.Contains("Fuel Type"))
                    carAd.FuelType = values[keys.IndexOf("Fuel Type")];

                if (keys.Contains("Year of manufacture"))
                    carAd.Year = values[keys.IndexOf("Year of manufacture")];

                if (keys.Contains("Reg"))
                    carAd.Reg = values[keys.IndexOf("Reg")];

                if (keys.Contains("Colour"))
                    carAd.Colour = values[keys.IndexOf("Colour")];

                if (keys.Contains("VIN"))
                    carAd.Vin = values[keys.IndexOf("VIN")];

                if (keys.Contains("Make"))
                    carAd.Make = values[keys.IndexOf("Make")];

                if (keys.Contains("title"))
                    carAd.Title = values[keys.IndexOf("title")];

                if (keys.Contains("Transmission"))
                    carAd.Transmission = values[keys.IndexOf("Transmission")];

                if (keys.Contains("Drive Type"))
                    carAd.DriveType = values[keys.IndexOf("Drive Type")];

                if (keys.Contains("Doors"))
                    carAd.Doors = values[keys.IndexOf("Doors")];

                if (keys.Contains("Stock Number"))
                    carAd.StockNumber = values[keys.IndexOf("Stock Number")];

                if (keys.Contains("Model"))
                    carAd.Model = values[keys.IndexOf("Model")];

                if (keys.Contains("Engine"))
                    carAd.Engine = values[keys.IndexOf("Engine")];

                if (keys.Contains("Class"))
                    carAd.Class = values[keys.IndexOf("Class")];

                carAd.Category = GetCategory(carAd.StockNumber, carAd.Body);
                carAd.Version = (_versionNumber + 1).ToString();

                UploadImages(carAd.Images);
                carAd.Images = carAd.Images.Select(
                    a => $"https://mcv.blob.core.windows.net/stock-images/{new Uri(a).Segments.Last()}"
                    ).ToList();

                
                repository.Upsert(carAd);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error for item {0}", detailsUrl);
            }
        }

        private static void UploadImages(List<string> sourceUrls)
        {
            foreach (var img in sourceUrls)
            {
                var uri = new Uri(img);
                var newBlockBlob = blobContainer.GetBlockBlobReference(uri.Segments.Last());
                var webClient = new WebClient();
                byte[] imageBytes = webClient.DownloadData(img);
                using (var stream = new MemoryStream(imageBytes, writable: false))
                {
                    newBlockBlob.UploadFromStream(stream);
                }
            }
        }

        private static string GetCategory(string stockNumber, string body)
        {
            CloudTableClient tableClient = csa.CreateCloudTableClient();
            CloudTable table = tableClient.GetTableReference("AdminToolOverride");

            TableQuery<AdminToolOverride> itemStockQuery = new TableQuery<AdminToolOverride>().Where(
                TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, stockNumber),
                    TableOperators.And,
                TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, stockNumber)));

            var adminToolOverrides = table.ExecuteQuery(itemStockQuery);
            if (adminToolOverrides.Any())
            {
                return adminToolOverrides.First().Category;
            }

            CloudTable cds = tableClient.GetTableReference("CDSFeed");
            TableQuery<CDSItem> cdsQuery = new TableQuery<CDSItem>().Where(
                TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "P1"),
                    TableOperators.And,
                TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, stockNumber)));

            var cdsItems = cds.ExecuteQuery(cdsQuery);
            if (cdsItems.Any())
            {
                return _categoryMap[cdsItems.First().Location];
            }

            return body;
        }

        private static int GetVersionNumber()
        {
            CloudTableClient tableClient = csa.CreateCloudTableClient();
            CloudTable table = tableClient.GetTableReference("DocumentVersion");
            TableQuery<DocumentVersion> cdsQuery = new TableQuery<DocumentVersion>().Where(
                TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "P1"),
                    TableOperators.And,
                TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, "versionnumber")));

            var cdsItems = table.ExecuteQuery(cdsQuery);
            return int.Parse(cdsItems.First().VersionNumber);
        }

        private static void UpdateVersionNumber()
        {
            CloudTableClient tableClient = csa.CreateCloudTableClient();
            CloudTable table = tableClient.GetTableReference("DocumentVersion");
            TableOperation retrieveOperation = TableOperation.Retrieve<DocumentVersion>("P1", "versionnumber");

            TableResult retrievedResult = table.Execute(retrieveOperation);

            // Assign the result to a CustomerEntity object.
            DocumentVersion updateEntity = (DocumentVersion)retrievedResult.Result;
            if (updateEntity != null)
            {
                updateEntity.VersionNumber = (int.Parse(updateEntity.VersionNumber) + 1).ToString();
                // Create the Replace TableOperation.
                TableOperation updateOperation = TableOperation.Replace(updateEntity);

                // Execute the operation.
                table.Execute(updateOperation);
            }
        }
    }
}

