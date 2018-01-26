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

namespace BayCityHoldenSync
{
    class Program
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

        static void Main(string[] args)
        {
            Logger.Debug("Starting Scraper");
            RunScraper();
            Logger.Debug("Finished Scraper");
            Logger.Debug("Starting API sync");
            UploadToApi();
            Logger.Debug("Finished API sync");
        }

        private static void UploadToApi()
        {
            var repo = new MongoRepository();
            var allCars = repo.GetAllItems();

            var apiCarAds = new List<ApiCarAd>();
            foreach (var ad in allCars)
            {
                apiCarAds.Add(new ApiCarAd()
                {
                    Colour = ad.Colour,
                    Make = ad.Make,
                    Model = ad.Model,
                    Odometer = ad.Odometer,
                    Price = (int)decimal.Parse(ad.Price),
                    Rego = string.IsNullOrEmpty(ad.Rego) ? "NA" : ad.Rego,
                    Title = ad.Title,
                    Vin = ad.Vin,
                    Year = ad.Year,
                    Condition = string.IsNullOrEmpty(ad.Condition) ? "NA" : ad.Condition,
                    Images = new List<CarImage> { new CarImage() { Url = ad.ImageUrl } }
                });
            }

            var client = new RestClient("http://snap247-qa.ap-southeast-2.elasticbeanstalk.com/");
            var request = new RestRequest("api/data-feed/vehicles/", Method.POST);

            request.AddHeader("authorization", "token 073cf860e6ea60734fad061e1588749ec66c2b2e");
            request.AddHeader("Accept", "application/json");
            request.AddParameter("application/json", JsonConvert.SerializeObject(apiCarAds), ParameterType.RequestBody);
            Logger.Debug(JsonConvert.SerializeObject(apiCarAds));

            var response = client.Execute(request);
            Logger.Info("API status code is {0}", response.StatusDescription);
        }

        private static void RunScraper()
        {
            var browser = new ScrapingBrowser();

            ServicePointManager.ServerCertificateValidationCallback +=
                (sender, cert, chain, sslPolicyErrors) => true;

            var url = "http://www.baycityholden.com.au/VehicleSearchResults";

            var webPage = browser.NavigateToPage(new Uri(url));
            var countContainer = webPage.Html.CssSelect("span#inv_search_count_container").FirstOrDefault();
            var numberOfItems = int.Parse(countContainer.InnerText);
            var numberOfPages = (numberOfItems / 25) + 1;

            List<string> detailsPageLinks = new List<string>();
            Dictionary<string, string> imageMap = new Dictionary<string, string>();

            for (int i = 1; i <= numberOfPages; i++)
            {
                var pagedUrl = string.Format("http://www.baycityholden.com.au/VehicleSearchResults?pageNumber={0}", i);
                var listingsPage = browser.NavigateToPage(new Uri(pagedUrl));
                var items = listingsPage.Html.CssSelect(".vehicleListWrapper article").ToList();

                foreach (var item in listingsPage.Html.CssSelect(".vehicleListWrapper article"))
                {
                    try
                    {
                        var link = item.CssSelect(".imageContainer figure a").FirstOrDefault();
                        var image = item.CssSelect(".imageContainer figure a img").FirstOrDefault();
                        string hrefValue = link.GetAttributeValue("href", string.Empty);
                        string imageUri = image.GetAttributeValue("data-original", string.Empty);

                        if (!string.IsNullOrEmpty(hrefValue) && !detailsPageLinks.Contains(hrefValue))
                        {
                            detailsPageLinks.Add(string.Format("http://www.baycityholden.com.au/{0}", hrefValue));
                        }

                        if (!string.IsNullOrEmpty(hrefValue) && !string.IsNullOrEmpty(imageUri) && !imageMap.ContainsKey(string.Format("http://www.baycityholden.com.au/{0}", hrefValue)))
                        {
                            imageMap.Add(string.Format("http://www.baycityholden.com.au/{0}", hrefValue), imageUri);
                        }
                    }
                    catch (Exception ex)
                    {

                    }

                }
            }

            var repository = new MongoRepository();
            foreach (var item in imageMap)
            {
                ProcessItem(item.Key, item.Value, repository);
            }
        }

        private static void ProcessItem(string detailsUrl, string imageUri, MongoRepository repository)
        {

            Logger.Info("Scraping Page {0}", detailsUrl);
            var browser = new ScrapingBrowser();
            var detailsPage = browser.NavigateToPage(new Uri(detailsUrl));

            try
            {
                var carAd = new CarAd();
                var year = detailsPage.Html.SelectSingleNode("//header/div[@class='h1']/span[@itemprop='releaseDate']/text()[1]").InnerText.CleanInnerText();
                var make = detailsPage.Html.SelectSingleNode("//header/div[@class='h1']/span[@itemprop='manufacturer']/text()[1]").InnerText.CleanInnerText();
                var model = detailsPage.Html.SelectSingleNode("//header/div[@class='h1']/span[@itemprop='model']/text()[1]").InnerText.CleanInnerText();
                var trim = detailsPage.Html.SelectSingleNode("//header/div[@class='h1']/span[@itemprop='trim']/text()[1]").InnerText.CleanInnerText();

                carAd.Title = string.Format("{0} {1} {2} {3}", year, make, model, trim);
                carAd.StockNumber = detailsPage.Html.SelectSingleNode("//input[@name='vehicle:buy:stock']").GetAttributeValue("value", "");
                carAd.Id = carAd.StockNumber;
                carAd.Vin = detailsPage.Html.SelectSingleNode("//input[@name='vehicle:buy:vin']").GetAttributeValue("value", "");
                carAd.Make = detailsPage.Html.SelectSingleNode("//input[@name='vehicle:buy:make']").GetAttributeValue("value", "");
                carAd.Model = detailsPage.Html.SelectSingleNode("//input[@name='vehicle:buy:model']").GetAttributeValue("value", "");
                carAd.Year = detailsPage.Html.SelectSingleNode("//input[@name='vehicle:buy:year']").GetAttributeValue("value", "");
                carAd.Price = detailsPage.Html.SelectSingleNode("//input[@name='vehicle:buy:price:asking']").GetAttributeValue("value", "");
                carAd.Odometer = detailsPage.Html.SelectSingleNode("//input[@name='vehicle:buy:odometer:mi']").GetAttributeValue("value", "");
                carAd.Colour = detailsPage.Html.SelectSingleNode("//input[@name='vehicle:buy:colorcombination:exteriorcolor_1']").GetAttributeValue("value", "");
                carAd.Rego = detailsPage.Html.SelectSingleNode("//input[@name='vehicle:buy:reg_plate']")?.GetAttributeValue("value", "");
                carAd.Condition = detailsPage.Html.SelectSingleNode("//input[@name='vehicle:buy:reg_plate']")?.GetAttributeValue("value", "");
                carAd.ImageUrl = imageUri.Replace("x200", "x650");
                carAd.FinalUrl = detailsUrl;
                carAd.LastModified = DateTime.UtcNow;
                carAd.Condition = detailsPage.Html.SelectSingleNode("//span[@itemprop='itemCondition']/text()[1]").InnerText;

                repository.Upsert(carAd);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error for item {0}", detailsUrl);
            }
        }
    }
}
