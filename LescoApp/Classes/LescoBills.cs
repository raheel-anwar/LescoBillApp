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
        private string lescoURL;
        public string getURL { get { return lescoURL; } }
        public string currentPath { get; set; }

        //Constructor Methods
        public LescoBills()
        {
            lescoURL = "";
        }
        public void customerID(string custID, bool IsCommercial)
        {
            lescoURL = getReferenceNo(custID, IsCommercial);
        }
        public void referenceNo(string batchNo, string subDiv, string refNo, string valueRU, bool IsCommercial)
        {
            if (IsCommercial == true)
            {
                lescoURL = string.Format("http://www.lesco.gov.pk/Modules/CustomerBill/BillPrintMDI.asp?nBatchNo={0}&nSubDiv={1}&nRefNo={2}&strRU={3}", batchNo, subDiv, refNo, valueRU);
            }
            else
            {
                lescoURL = string.Format("http://108.166.183.120/Default.aspx?BatchNo={0}&SubDiv={1}&RefNo={2}&RU={3}", batchNo, subDiv, refNo, valueRU);
            }
        }

        //Search the string to get a specific value between two points.
        private string findURL(string strSource, string strStart, string strEnd)
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
            HttpWebRequest mainPageRequest = (HttpWebRequest)WebRequest.Create(lescoURL);
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
                sb.Append(findURL(webString, @"\/Reserved.ReportViewerWebControl", "OnlyHtmlInline&Format="));
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

        //Get Reference No from CustomerID first
        public string getReferenceNo(string custID, bool IsCommercial)
        {
            string URL = "http://www.lesco.gov.pk/Modules/CustomerBill/CustomerMenu.asp";
            string customerID = string.Format("txtCustID={0}", custID);
            string searchParam = IsCommercial ? "/Modules/CustomerBill/BillPrintMDI.asp?nBatchNo=" : "/Modules/CustomerBill/BillPrint.asp?nBatchNo=";

            string referenceNo;

            using (WebClient web = new WebClient())
            {
                web.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                string resultHTML = web.UploadString(URL, customerID);
                string refNo = findURL(resultHTML, searchParam, "\"");
                

                if (IsCommercial)
                {
                    refNo = refNo.Insert(0, "http://www.lesco.gov.pk/Modules/CustomerBill/BillPrintMDI.asp?nBatchNo=");
                    referenceNo = refNo;
                }
                else
                {
                    refNo = refNo.Replace("n", "").Replace("strR", "R");
                    refNo = refNo.Insert(0, "http://108.166.183.120/Default.aspx?BatchNo=");
                    referenceNo = refNo;
                }

            }
            if (referenceNo.Contains("RefNo="))
            {
                return referenceNo;
            }
            else
            {
                throw new Exception();
            }
        }
    }
}
