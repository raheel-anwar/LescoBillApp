using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace LescoApp.Classes
{
    class LescoBills
    {
        private string baseAddress;
        public string currentPath { get; set; }

        //Constructor Methods
        public LescoBills (string batchNo, string subDiv, string refNo, string valueRU)
        {
            baseAddress = string.Format("http://108.166.183.120/Default.aspx?BatchNo={0}&SubDiv={1}&RefNo={2}&RU={3}", batchNo, subDiv, refNo, valueRU);
        }

        //Search the string to get a specific value between two points.
        private string getAddress(string strSource, string strStart, string strEnd)
        {
            int stringStart;
            int stringEnd;
            stringStart = strSource.IndexOf(strStart, 0) + strStart.Length;
            stringEnd = strSource.IndexOf(strEnd, stringStart);
            return strSource.Substring(stringStart, stringEnd - stringStart);
        }

        //Get Lesco Bill as PDF Main Method
        public void getLescoBillPDF()
        {
            string webString = "";

            //Create a cookie container to establish a proper session
            CookieContainer newCookie = new CookieContainer();

            //Create a request and response for main HTML page
            HttpWebRequest mainPageRequest = (HttpWebRequest)WebRequest.Create(baseAddress);
            mainPageRequest.CookieContainer = newCookie;
            HttpWebResponse mainPageResponse = (HttpWebResponse)mainPageRequest.GetResponse();

            //Creates a stream reader to read HTTP response and store the result in a string
            using (StreamReader myReader = new StreamReader(mainPageResponse.GetResponseStream()))
            {
                webString = myReader.ReadToEnd();
            }

            //Create a string builder to prepare a download string for PDF from webString
            //Always use string because HTML response is a long string and cause performance issues
            StringBuilder sb = new StringBuilder();
            try
            {
                sb.Append(getAddress(webString, @"\/Reserved.ReportViewerWebControl", "OnlyHtmlInline&Format="));
                sb.Append("OnlyHtmlInline&Format=PDF");
                sb.Insert(0, @"http://108.166.183.120/Reserved.ReportViewerWebControl");
            }
            catch
            {
                sb.Clear();
                sb.Append("Internal Error...");
                File.WriteAllText(string.Format(@"{0}\LescoBill.pdf", currentPath), sb.ToString());
                mainPageResponse.Close();
                return;
            }

            //Create a request and response for PDF document but in the same session.
            HttpWebRequest requestPDF = (HttpWebRequest)WebRequest.Create(sb.ToString());
            requestPDF.CookieContainer = newCookie;
            HttpWebResponse responsePDF = (HttpWebResponse)requestPDF.GetResponse();

            //Create two streams to copy web stream into file stream as PDF
            using (Stream streamPDF = responsePDF.GetResponseStream())
            {
                using (Stream outputPDF = File.OpenWrite(string.Format(@"{0}\LescoBill.pdf", currentPath)))
                {
                    streamPDF.CopyTo(outputPDF);
                }
            }

            mainPageResponse.Close();
            responsePDF.Close();
        }
    }
}
