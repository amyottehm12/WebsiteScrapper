using System;
using System.IO;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using iTextSharp.text;
using iTextSharp.text.html.simpleparser;
using iTextSharp.text.pdf;
using System.Linq;
using System.Collections.Generic;
using Microsoft.VisualBasic.FileIO;
using System.Text;
using System.Net;
using System.Web;

namespace Website_Scrapper
{
    public partial class Form1 : Form
    {
        static int recipeNumber = 100;
        static string[] linksArray;
        string[] namesArray;

        string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\Recipes";


        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //Send login information
            SendDataToService();

            //grab all the URLs for the recipes
            populateURLs();

            //Gets the data from each URL
            grabData();

            //Exports the list as a PDF
            exportAsPDF();
        }

        private void SendDataToService()
        {
            //StringBuilder sb = new StringBuilder();
            //AppendParameter(sb, "email", "twyms22@gmail.com");

            //byte[] byteArray = Encoding.UTF8.GetBytes(sb.ToString());

            //string url = "https://www.superhealthykids.com/my-account/"; //or: check where the form goes

            //HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            //request.Method = "POST";
            //request.ContentType = "application/x-www-form-urlencoded";
            ////request.Credentials = CredentialCache.DefaultNetworkCredentials; // ??

            //using (Stream requestStream = request.GetRequestStream())
            //{
            //    requestStream.Write(byteArray, 0, byteArray.Length);
            //}

            //HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            CookieCollection Cookies = new CookieCollection();
            var web = new HtmlWeb();
            web.OverrideEncoding = Encoding.Default;
            web.UseCookies = true;
            web.PreRequest += (request) =>
            {
                if (request.Method == "POST")
                {
                    string payload = request.Address.Query;
                    byte[] buff = Encoding.UTF8.GetBytes(payload.ToCharArray());
                    request.ContentLength = buff.Length;
                    request.ContentType = "application/x-www-form-urlencoded";
                    System.IO.Stream reqStream = request.GetRequestStream();
                    reqStream.Write(buff, 0, buff.Length);
                }

                request.CookieContainer.Add(Cookies);

                return true;
            };

            web.PostResponse += (request, response) =>
            {
                if (request.CookieContainer.Count > 0 || response.Cookies.Count > 0)
                {
                    Cookies.Add(response.Cookies);
                }
            };

            string baseUrl = "https://www.superhealthykids.com/";
            string urlToHit = baseUrl + "?QueryString with Login Credentials";
            HtmlAgilityPack.HtmlDocument doc = web.Load(urlToHit, "POST");

        }

        protected void AppendParameter(StringBuilder sb, string name, string value)
        {
            string encodedValue = HttpUtility.UrlEncode(value);
            sb.AppendFormat("{0}={1}&", name, encodedValue);
        }

        /// <summary>
        /// Fills the testURL with urls to parse
        /// </summary>
        private void populateURLs()
        {
            var names = new List<string[]>();
            var links = new List<string[]>();

            using (FileStream reader = File.OpenRead(desktop + "\\fileNames.txt")) // mind the encoding - UTF8
            using (TextFieldParser parser = new TextFieldParser(reader))
            {
                parser.TrimWhiteSpace = true; // if you want
                parser.Delimiters = new[] { "," };
                parser.HasFieldsEnclosedInQuotes = true;
                while (!parser.EndOfData)
                {
                    string[] line = parser.ReadFields();
                    names.Add(line);
                }
            }
            
            using (FileStream reader = File.OpenRead(desktop + "\\links.txt")) // mind the encoding - UTF8
            using (TextFieldParser parser = new TextFieldParser(reader))
            {
                parser.TrimWhiteSpace = true; // if you want
                parser.Delimiters = new[] { "," };
                parser.HasFieldsEnclosedInQuotes = true;
                while (!parser.EndOfData)
                {
                    string[] line = parser.ReadFields();
                    links.Add(line);
                }
            }


            namesArray = names[0].ToArray();
            linksArray = links[0].ToArray();
            
            var test = 0;
        }

        /// <summary>
        /// Fills the data[] with the inner HTML of the div named recipe
        /// </summary>
        /// <param name="urlAddresses"></param>
        private void grabData()
        {

            //for the length of the urlAddress string....
            for (int i = 0; i < linksArray.Length; i++)
            {
                //As long as the urlAddress exists
                if (linksArray[i] != null)
                {
                    //initialise the web page
                    var webget = new HtmlWeb();
                    //load in the first URL
                    var document = webget.Load(linksArray[i]);
                    //Grab all img nodes
                    var imagesToRemove = document.DocumentNode.SelectNodes("//img[@src!='']");
                    //remove all image nodes
                    foreach (var image in imagesToRemove)
                    {
                        image.Remove();
                    }
                    //grab all stupid widget share div nodes
                    var widgetToRemove = document.DocumentNode.SelectNodes("//div[contains(@class,'cf yummly-share-widget')]");
                    //blast those things from orbit
                    if (widgetToRemove != null)
                    {
                        foreach (var widget in widgetToRemove)
                        {
                            widget.Remove();
                        }

                    }
                    

                    //scan through all divs, look for class="recipe"
                    foreach (HtmlNode div in document.DocumentNode.SelectNodes("//div[contains(@class,'recipe')]"))
                    {
                        //dump that into the data[]
                        linksArray[i] = div.InnerHtml.ToString();
                        //regex magic to remove random \r\t\n's
                        linksArray[i] = Regex.Replace(linksArray[i], @"\r\n\t?|\n|\t|\r", "");
                        //turn it back into a compatible website
                        linksArray[i] = "<html><div>" + linksArray[i] + "</div></html>";
                    }

                }

            }


            //stream reads the HTML direct into the data[]
            #region Old
            //for (int i = 0; i < urlAddresses.Length; i++)
            //{
            //    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(urlAddresses[i]);
            //    HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            //    if (response.StatusCode == HttpStatusCode.OK)
            //    {
            //        Stream receiveStream = response.GetResponseStream();
            //        StreamReader readStream = null;

            //        if (response.CharacterSet == null)
            //        {
            //            readStream = new StreamReader(receiveStream);
            //        }
            //        else
            //        {
            //            readStream = new StreamReader(receiveStream, Encoding.GetEncoding(response.CharacterSet));
            //        }

            //        data[i] = readStream.ReadToEnd();

            //        response.Close();
            //        readStream.Close();
            //    }

            //}
            #endregion Old

        }

        /// <summary>
        /// saves all the data[] as PDF's
        /// </summary>
        private void exportAsPDF()
        {
            for (int i = 0; i < linksArray.Length; i++)
            {
                if (linksArray[i] != null)
                {
                    //MemoryStream msOutput = new MemoryStream();
                    TextReader reader = new StringReader(linksArray[i]);

                    // step 1: Creation of a document-object
                    Document document = new Document(PageSize.A4, 30, 30, 30, 30);

                    // step 2:
                    // Create a writer that listens to the document
                    // and directs a XML-stream to a file
                    PdfWriter writer = PdfWriter.GetInstance(document, new FileStream(desktop + "\\" + namesArray[i] + ".pdf", FileMode.CreateNew));

                    // step 3: Create a worker parse the document
                    HTMLWorker worker = new HTMLWorker(document);

                    // step 4: Open document and start the worker on the document
                    document.Open();
                    worker.StartDocument();

                    // step 5: Parse the html into the document
                    worker.Parse(reader);

                    // step 6: Close the document and the worker
                    worker.EndDocument();
                    worker.Close();
                    document.Close();
                }

            }
            
        }
        
    }

}
