using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Xml;

namespace AppConnectorTestToolSource
{
    public enum EConnectorTypes
    {
        GetConnector = 1,
        UpdateConnector = 2,
        XSDSchema = 3
    }

    public class Functions
    {
        public resultObject ExecuteGetconnector(string _url, string _token, string _connector)
        {
            // Maak een result-object aan
            resultObject _resultObject = new resultObject();
            // Roep de connector aan
            _resultObject = CallWebService(_url, _token, _connector, EConnectorTypes.GetConnector);

            if (_resultObject.success == false)
            {
                return _resultObject;
            }

            // Maak een nieuw XMLDocument en laad het resultaat hierin
            XmlDocument _XMLresult = new XmlDocument();
            _XMLresult.LoadXml(_resultObject.resultXML);

            // Zet de inhoud van de 'GetDataResult' tag in _resultSoapBody
            var _resultSoapBody = _XMLresult.GetElementsByTagName("GetDataResult")[0];
            // Haal de XML-inhoud op uit de _resultSoapBody
            string _innerObject = _resultSoapBody.InnerXml;
            // Hernoem de XML tags mbv regex  
            // TODO: is hier een betere manier voor?
            string _parsed = _innerObject.Replace("&lt;", "<")
                                               .Replace("&amp;", "&")
                                               .Replace("&gt;", ">")
                                               .Replace("&quot;", "\"")
                                               .Replace("&apos;", "'");
            // Zet de resultaten in het resultaten-object
            _resultObject.resultText = "De connector aanroep is succesvol uitgevoerd. Zie hieronder voor het resultaat:\r\n<br />Connector call succesfull executed. See below for the result:\r\n";
            // Converteer de xml naar html incl tags etc
            _resultObject.resultXML = xmlToHTML(_parsed, _connector);
            _resultObject.success = true;
            // Geef het resultaten-object terug
            return _resultObject;
        }

        public resultObject ExecuteUpdateConnector(string _url, string _token, string _connector, string _xmlUpdate)
        {
            // Maak een result-object aan
            resultObject _resultObject = new resultObject();
            // Roep de connector aan
            _resultObject = CallWebService(_url, _token, _connector, EConnectorTypes.UpdateConnector, _xmlUpdate);
            // als de aanroep goed gegaan is wordt er een tekst in het resultaat gezet
            if (_resultObject.success == true)
            {
                _resultObject.resultText = "De updateconnector is succesvol uitgevoerd\r\n<br />The updateconnector is succesfully executed";
            }

            return _resultObject;
        }

        // haalt het XSD schema op van de gekozen updateconnector
        public resultObject ExecuteXSDScheme(string _url, string _token, string _connector)
        {
            // maak een nieuw resultaten-object
            resultObject _resultObject = new resultObject();

            // roept de webservice aan
            _resultObject = CallWebService(_url, _token, _connector, EConnectorTypes.XSDSchema);
            // haalt het resultaat uit de SOAP header
            string _base64XSDraw = getResultFromSoap(_resultObject.resultXML);
            // zet de Base64 string om naar xml-string en zet deze in het resultaten object
            _resultObject.resultXML = XSDgetXmlFromBase64(_base64XSDraw, _connector);
            // geeft de success flag aan en een resultaten-tekst
            _resultObject.success = true;
            _resultObject.resultText = "Het XSD-schema is succesvol opgehaald en wordt nu gedownload\r\n<br />The XSD-scheme has been successfully retrieved, and will be downloaded now\r\n";

            return _resultObject;
        }

        // haalt het xml-resultaat uit de soap envelope
        protected string getResultFromSoap(string _xmlString)
        {
            XmlDocument _xml = new XmlDocument();
            _xml.LoadXml(_xmlString);

            return (_xml.GetElementsByTagName("ExecuteResult")[0]).InnerXml;
        }

        // haalt het XSD-schema uit de xml
        protected string XSDgetSchemaFromXML(string _xml)
        {
            XmlDocument _xdoc = new XmlDocument();
            _xdoc.LoadXml(_xml);
            return (_xdoc.SelectSingleNode("AfasDataConnector/ConnectorData/Schema")).InnerXml;
        }

        // converteert een base64 string van het XSD-schema naar een xml-string
        protected string XSDgetXmlFromBase64(string _base64XSDraw, string _connector)
        {
            byte[] _initFile = Convert.FromBase64String(_base64XSDraw);

            string _xsdString = System.Text.Encoding.UTF8.GetString(_initFile);
            string _xsdStringHtmlDecode = HttpUtility.HtmlDecode(_xsdString);
            return XSDgetSchemaFromXML(_xsdStringHtmlDecode);
        }

        // roept de webservice aan op basis van type actie (get/update/XSD) en vangt fouten in de request af
        protected static resultObject CallWebService(string _url, string _token, string _connector, EConnectorTypes _action, string _xmlUpdate = "")
        {
            // Maak niet resultaten-object
            resultObject _results = new resultObject();
            // Haal de actionstring op, afhankelijk van type action
            string _actionString = GetActionString(_action);
            string _fullUrl = GetFullUrl(_action, _url);
            string _fullToken = fullToken(_token);
            try
            {

                // Maak de soapenvelop aan, afhankelijk van de actie
                XmlDocument soapEnvelopeXml = CreateSoapEnvelope(_fullToken, _connector, _action, _xmlUpdate);
                // Maak de webrequest aan
                HttpWebRequest webRequest = CreateWebRequest(_fullUrl, _actionString);
                // Laadt de soapEnvelop in de webrequest
                InsertSoapEnvelopeIntoWebRequest(soapEnvelopeXml, webRequest);

                // Begin async call naar de webrequest.
                IAsyncResult asyncResult = webRequest.BeginGetResponse(null, null);

                // Suspend de thread totdat de call klaar is
                asyncResult.AsyncWaitHandle.WaitOne();

                // Haal de response op van de request, en zet het resultaat in het resultaten-object
                string soapResult;
                using (WebResponse webResponse = webRequest.EndGetResponse(asyncResult))
                {
                    using (StreamReader rd = new StreamReader(webResponse.GetResponseStream()))
                    {
                        soapResult = rd.ReadToEnd();
                    }
                }
                _results.resultXML = soapResult;
                _results.success = true;
                return _results;
            }
            catch (WebException wex) // afvangen van de webException
            {
                resultObject _resultObject = new resultObject();

                WebResponse errResp = wex.Response; // lees de error response
                string error = "";
                using (Stream respStream = errResp.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(respStream);
                    string text = reader.ReadToEnd();
                    // vangt verschillende foutmeldingen af, en geeft een duidelijke foutmelding terug in het resultObject
                    if (text.Contains("WARN............: Token could not be parsed")) { error = "Token kon niet gelezen worden\r\n<br />Token could not be parsed"; }
                    else if (text.Contains("Ongeldige gebruikersnaam") || text.Contains("invalid username") || text.Contains("inloggegevens niet gevonden")) { error = "Ongeldig token\r\n<br />Invalid token"; }
                    else if (text.Contains("Connector") && text.Contains("does not exist")) { error = "Connector bestaat niet\r\n<br />Connector does not exist"; }
                    else if (text.Contains("404.0 - Not Found")) { error = "De URL is niet juist\r\n<br />The URL is not valid"; }
                    else if (text.Contains("404")) { error = "De URL is niet juist\r\n<br />The URL is not valid"; }
                    else if (text.Contains("Inloggegevens")) { error = "Ongeldig token\r\n<br />Invalid token"; }
                    else { error = "Unhandled WebException:\r\n<br />" + text; }
                }
                _resultObject.success = false;
                _resultObject.resultText = error;
                return _resultObject;
            }
            catch (UriFormatException) // afvangen van een onjuiste url en geeft een duidelijke foutmelding terug in het resultObject
            {
                resultObject _resultObject = new resultObject();
                _resultObject.success = false;
                _resultObject.resultText = "De URL is niet juist\r\n<br />The URL is not valid";
                return _resultObject;
            }
            catch (Exception ex) // afvangen van overige exceptions, en geeft de inhoud van de exception terug in het resultObject
            {
                resultObject _resultObject = new resultObject();
                _resultObject.success = false;
                _resultObject.resultText = "Unhandled Exception:\r\n<br />" + ex;
                return _resultObject;
            }
        }

        // geeft de juiste actiestring voor in de soap-request terug
        private static string GetActionString(EConnectorTypes _action)
        {
            string _returnActionString = "";
            switch (_action)
            {
                case EConnectorTypes.GetConnector: // GetConnector
                    _returnActionString = "urn:Afas.Profit.Services/GetData";
                    break;
                case EConnectorTypes.UpdateConnector: // Updateconnector
                    _returnActionString = "urn:Afas.Profit.Services/Execute";
                    break;
                case EConnectorTypes.XSDSchema: // XSD-schema ophalen
                    _returnActionString = "urn:Afas.Profit.Services/Execute";
                    break;
            }
            return _returnActionString;
        }

        // haal de volledige url op op basis van de base-url en afhankelijk van het type aanroep (get/update/XSD)
        private static string GetFullUrl(EConnectorTypes _action, string _url)
        {
            string _relative = "";
            // Haal de base url op vanuit de aangeleverde url
            var _uri = new Uri(_url);
            string _baseuri = _uri.GetLeftPart(UriPartial.Authority);
            //bepaal de asmx pagina
            switch (_action)
            {
                case EConnectorTypes.GetConnector: //GetConnector
                    _relative = "appconnectorget.asmx";
                    break;
                case EConnectorTypes.UpdateConnector: //Updateconnector
                    _relative = "appconnectorupdate.asmx";
                    break;
                case EConnectorTypes.XSDSchema: //XSD-schema ophalen
                    _relative = "appconnectordata.asmx";
                    break;
            }
            string _fullUrl = _baseuri + "/profitservices/" + _relative;
            return _fullUrl;
        }

        // maakt het token compleet inclusief tags
        private static string fullToken(string _token)
        {
            if (_token.Contains("data")) // controleer of er al een tag aanwezig is, kan eigenlijk niet, maar voor de zekerheid
            {
                return _token;
            }
            else // vul het token aan met de tags
            {
                return "<token><version>1</version><data>" + _token + "</data></token>";
            }
        }

        // maak de webrequest aan inclusief headers
        private static HttpWebRequest CreateWebRequest(string _fullUrl, string _actionString)
        {
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(_fullUrl); // maak een nieuw webrequest met de volledige url
            webRequest.Headers.Add("SOAPAction", _actionString); // add headers voor soap
            webRequest.ContentType = "text/xml;charset=utf-8";
            webRequest.Accept = "text/xml";
            webRequest.Method = "POST";
            return webRequest;
        }

        // maakt de soapenvelop, op basis van type actie (get/update/XSD)
        private static XmlDocument CreateSoapEnvelope(string _token, string _connector, EConnectorTypes _action, string _xmlUpdate = "")
        {
            XmlDocument soapEnvelop = new XmlDocument(); // maak een nieuwe soap envelop
            string createdXml = "";
            switch (_action)
            {
                case EConnectorTypes.GetConnector: // getconnector
                    createdXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
    <soap:Body>
        <GetData xmlns=""urn:Afas.Profit.Services"">
            <token>
                <![CDATA[" + _token + @"]]>
            </token>
            <connectorId>" + _connector + @"</connectorId>
            <skip>0</skip>
            <take>20</take>
        </GetData>
    </soap:Body>
</soap:Envelope>";
                    break;
                case EConnectorTypes.UpdateConnector: //updateconnector
                    createdXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
    <soap:Body>
        <Execute xmlns=""urn:Afas.Profit.Services"">
            <token>
                <![CDATA[" + _token + @"]]>
            </token>
            <connectorType>" + _connector + @"</connectorType>
            <connectorVersion>1</connectorVersion>
            <dataXml>
                <![CDATA[" + _xmlUpdate + @"]]>
            </dataXml>
        </Execute>
    </soap:Body>
</soap:Envelope>";
                    break;
                case EConnectorTypes.XSDSchema: // XSD-schema
                    createdXml = @"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:urn=""urn:Afas.Profit.Services"">
<soapenv:Header/>
 <soapenv:Body>
  <urn:Execute>
   <urn:token><![CDATA[" + _token + @"]]></urn:token>
   <urn:dataID>GetXmlSchema</urn:dataID>
   <urn:parametersXml>
    <![CDATA[
    <DataConnector><UpdateConnectorId>" + _connector + @"</UpdateConnectorId><EncodeBase64>true</EncodeBase64></DataConnector>
     ]]>
   </urn:parametersXml>
  </urn:Execute>
 </soapenv:Body>
</soapenv:Envelope>";
                    break;
            }

            soapEnvelop.LoadXml(createdXml);
            return soapEnvelop;
        }

        // zet de soap envelop in de webrequest
        private static void InsertSoapEnvelopeIntoWebRequest(XmlDocument soapEnvelopeXml, HttpWebRequest webRequest)
        {
            using (Stream stream = webRequest.GetRequestStream())
            {
                soapEnvelopeXml.Save(stream); // insert de soapenvelop in de webrequest
            }
        }
        // Formateer de xml naar HTML-output
        protected string xmlToHTML(string _xmlString, string _connector)
        {
            XmlDocument _xml = new XmlDocument();
            _xml.LoadXml(_xmlString); // laad de xml in een xml-object
            int _maxNodes = 0;
            string _output = "<thead><tr>"; // open de table head
            string _headers = "";
            XmlNode _parent = _xml.SelectSingleNode("//" + _connector); // selecteer de juiste node, afhankelijk van de connectornaam
            XmlNodeList _xmlnodelist = _xml.SelectNodes("//" + _connector); // haal een nodelijst op

            foreach (XmlNode _child in _xmlnodelist) // haal de namen van de velden op en zet deze in de table-head
            {
                if (_child.ChildNodes.Count > _maxNodes) // loop tot het einde van de node bereikt is
                {
                    _headers = "";
                    _maxNodes = _child.ChildNodes.Count;
                    for (int i = 0; i < _child.ChildNodes.Count; i++) // loop door elke node heen, en zet deze in een html tabel als tabel-head
                    {
                        _headers += "<th>" + _child.ChildNodes[i].Name + "</th>";
                    }
                }
            }
            _output += _headers;
            _output += "</tr></thead><tbody>"; // sluit de table-head en open de table-body

            foreach (XmlNode _child in _xmlnodelist) // haal nu alle data uit de xml op en zet deze in de tabel
            {
                _output += "<tr>";
                for (int i = 0; i < _child.ChildNodes.Count; i++) // loop door de data heen
                {
                    _output += "<td>";
                    if (String.IsNullOrEmpty(_child.ChildNodes[i].InnerText)) { _output += "[leeg]"; } // als een node leeg is, zet dan [leeg] in de output
                    else if (String.IsNullOrWhiteSpace(_child.ChildNodes[i].InnerText)) { _output += "[leeg]"; } // als een node leeg is, zet dan [leeg] in de output
                    else { _output += _child.ChildNodes[i].InnerText; } // zet de tekst in de tabel
                    _output += "</td>";
                }
                _output += "</tr>"; // sluit de tabel
            }

            return _output;
        }
    }
}