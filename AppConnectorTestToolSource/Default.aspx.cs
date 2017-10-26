using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Net;
using System.Xml;
using System.IO;
using System.Text;

namespace AppConnectorTestToolSource
{
    public partial class Default : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            //verberg velden die nog niet zichtbaar hoeven zijn
            resultGetWarning.Visible = false; // waarschuwing dat de getconnector test maar 20 resultaten ophaalt
            resultXML.Visible = false; // mogelijkheid om bron-xml te laten zien, niet geïmplementeerd, container van lbXML
            lbXML.Visible = false; // mogelijkheid om bron-xml te laten zien
            lbAOL.Visible = true; // laat het veld AFAS Online abonnementsnummer zien, zodat je die makkelijk in kunt vullen
            vlUploadXML.Enabled = false;
            ShowLabels("Results", false);
            ShowLabels("Alert", false);
            ShowLabels("UploadXML", false);

            // voorkom dubbele post bij F5 of refresh
            CancelUnexpectedRepost();

            // vul de actie-dropdown vanuit de enum voor type actie
            EnumToDropDown(typeof(EConnectorTypes), DropDownAction);
            if ((EConnectorTypes)Enum.Parse(typeof(EConnectorTypes), DropDownAction.SelectedValue) == EConnectorTypes.UpdateConnector)
            {
                ShowLabels("UploadXML", true);
                vlUploadXML.Enabled = true;
            }
            else
            {
                ShowLabels("UploadXML", false);
                vlUploadXML.Enabled = false;
            }
        }

        // haalt de enum-waardes op van een bepaald type, en zet deze in een dropdownlist
        static private void EnumToDropDown(Type _enumType, DropDownList _dropDown)
        {
            // controle: als er al waardes in de dropdown staan worden deze niet nog een keer toegevoegd
            if (_dropDown.Items.Count != 0)
            {
                return;
            }
            else
            {
                // maak een array van de values van de enum
                Array _values = System.Enum.GetValues(_enumType);

                // loop door de values heen en voeg deze toe aan de dropdownlist
                foreach (int _value in _values)
                {
                    string _name = Enum.GetName(_enumType, _value);
                    ListItem _item = new ListItem(_name, _value.ToString());
                    _dropDown.Items.Add(_item);
                }
            }
        }

        // Voorkom dat een actie 2x wordt uitgevoerd door een F5 of refresh van de pagina
        private void CancelUnexpectedRepost()
        {
            string clientCode = _repostcheckcode.Value;
            // Get server code van de sessie
            string serverCode = Session["_repostcheckcode"] as string ?? "";
            if (!IsPostBack || clientCode.Equals(serverCode))
            {
                // Codes zijn hetzelfde, actie is geïnitieerd door de gebruiker
                // Sla nieuwe code op
                string code = Guid.NewGuid().ToString();
                _repostcheckcode.Value = code;
                Session["_repostcheckcode"] = code;
            }
            else
            {
                // Onverwachte acties door F5 of refresh
                Response.Redirect(Request.Url.AbsoluteUri);
            }
        }

        // Maak alle velden leeg
        protected void btClear_Click(object sender, EventArgs e)
        {
            // maak velden leeg
            tbUrl.Text = "";
            tbToken.Text = "";
            tbConnector.Text = "";
            tbAOLnumber.Text = "";
            // maakt de resultaten/velden onzichtbaar
            ShowLabels("Results", false);
            ShowLabels("Alert", false);
            resultGetWarning.Visible = false;
            if ((EConnectorTypes)Enum.Parse(typeof(EConnectorTypes), DropDownAction.SelectedValue) == EConnectorTypes.UpdateConnector)
            {
                ShowLabels("UploadXML", true);
                vlUploadXML.Enabled = true;
            }
            else
            {
                ShowLabels("UploadXML", false);
                vlUploadXML.Enabled = false;
            }
        }

        // Wijzigt waardes op basis van het wel/niet aanvinken van AFAS Online
        protected void cbAFASOnline_CheckedChanged(object sender, EventArgs e)
        {
            if (cbAFASOnline.Checked == true) // Als AFAS Online aangevinkt wordt, zet dan de onderstaande waarde in het url-veld en maak het veld AOL zichtbaar
            {
                string _url = "https://" + tbAOLnumber.Text + ".afasonlineconnector.nl";
                tbUrl.Text = _url;
                ShowLabels("AOL", true);
            }
            if (cbAFASOnline.Checked == false) // Als AFAS Online uitgevinkt wordt, zet dan onderstaande waarde in het url-veld, en maak het veld AOL onzichtbaar
            {
                tbUrl.Text = @"https://[servername]";
                ShowLabels("AOL", false);
            }
        }

        // Bij het klikken op de testknop
        protected void btCheck_Click(object sender, EventArgs e)
        {
            // haal de variabelen op uit de user-input boxen (validatie zit op de invoervelden zelf)
            string _url = tbUrl.Text;
            string _token = tbToken.Text;
            string _connector = tbConnector.Text;
            EConnectorTypes _actionType = (EConnectorTypes)Enum.Parse(typeof(EConnectorTypes), DropDownAction.SelectedValue);
            //int _action = listBoxAction.SelectedIndex; // haal de gekozen actie op (get/update/XSD)

            Functions _functions = new Functions();
            resultObject _results = new resultObject();

            switch (_actionType)
            {
                case EConnectorTypes.GetConnector: // getconnector
                    _results = _functions.ExecuteGetconnector(_url, _token, _connector);
                    break;
                case EConnectorTypes.UpdateConnector: // updateconnector
                    _results = GetUploadedXMLString();
                    if (_results.success == false) { break; }
                    else
                    {
                        _results = _functions.ExecuteUpdateConnector(_url, _token, _connector, _results.resultXML);
                    }
                    break;
                case EConnectorTypes.XSDSchema: // XSD-schema
                    _results = _functions.ExecuteXSDScheme(_url, _token, _connector);
                    break;
                default:
                    _results.success = false;
                    _results.resultText = "Er ging iets mis, probeer het later opnieuw\r\n<br />Something went wrong, try again later";
                    break;
            }
            if (_results.success == true)
            {
                // laat de juiste labels en onderdelen zien
                ShowLabels("Results", true);
                lbResult.Text = _results.resultText;
                switch (_actionType)
                {
                    case EConnectorTypes.GetConnector: // getconnector
                        resultGetWarning.Visible = true;
                        lbResultTable.Text = _results.resultXML;
                        break;
                    case EConnectorTypes.UpdateConnector: // updateconnector
                        lbResultTable.Text = _results.resultXML;
                        break;
                    case EConnectorTypes.XSDSchema: // XSD-schema
                        downloadXSD(_results.resultXML, _connector); // bied de XSD als download aan in de browser
                        break;
                    default: // fallback
                        break;
                }
                //ScriptManager.RegisterStartupScript(Page, typeof(Page), "smoothScrollResult", "smoothScrollResult();", true);
                ScriptManager.RegisterClientScriptBlock(Page, typeof(Page), "smoothScrollToResult", ";$(function() { $('html, body').animate({ scrollTop: $('#resultHeader').offset().top }, 'slow')}", false);
            }
            else if (_results.success == false) // als er iets fout gegaan is
            {
                ShowLabels("Alert", true);
                lbAlert.Text = _results.resultText + _results.resultXML;
            }
            else // extra fallback
            {
                ShowLabels("Alert", true);
                lbAlert.Text = "Er ging iets mis, probeer het later opnieuw\r\n<br />Something went wrong, try again later";
            }
        }

        // reads the uploaded XML and returns a resultobject with de xml-string in de xmlresult
        private resultObject GetUploadedXMLString()
        {
            resultObject _result = new resultObject();
            try
            {
                if (ulUploadXML.PostedFile != null) // controleer of er een bestand is
                {
                    // zet de file in een variabele
                    HttpPostedFile _uploadedXML = ulUploadXML.PostedFile;
                    // Haal de lengte op van de file
                    int _fileLen = _uploadedXML.ContentLength;
                    // ZEt de file in een byte-array
                    byte[] _byteXML = new byte[_fileLen];
                    // lees de file naar bytes
                    _uploadedXML.InputStream.Read(_byteXML, 0, _fileLen);
                    // maak een string van het geuploadde bestand
                    string _xml = Encoding.UTF8.GetString(_byteXML);
                    // controleer of deze juist is, deze wordt afgevangen door een catch
                    XmlDocument _xmlTest = new XmlDocument();
                    _xmlTest.LoadXml(_xml);
                    // Zet de xml string in het resultaten-object en geen deze terug
                    _result.success = true;
                    _result.resultXML = _xml;
                    return _result;
                }
                else
                {
                    throw new Exception("no file selected");
                }
            }
            catch (XmlException xmlEx) // XML-exception -> xml kon niet goed geparsed worden
            {
                _result.success = false;
                _result.resultText = "Kon het XML-bestand niet valideren, controleer of deze juist is en UTF-8 gecodeerd\r\n<br />Could not verify the XML-file, check if the file is correct and UTF-8 formatted";
                return _result;
            }
            catch (Exception ex) // overige fouten bij uploaden XML
            {
                _result.resultText = "Er ging iets fout bij het uploaden van de XML, probeer het later opnieuw\r\n<br />Something went wrong with uploading de XML, try again later";
                _result.success = false;
                return _result;
            }

        }

        protected void downloadXSD(string _XSDSchema, string _connector)
        {
            // Maak een byte array van de xsd-string
            byte[] _file = System.Text.Encoding.UTF8.GetBytes(_XSDSchema);

            // zet response headers
            Response.Clear();
            Response.ClearHeaders();
            Response.ClearContent();
            Response.AddHeader("Content-Disposition", "attachment; filename=" + _connector + ".xsd");
            Response.AddHeader("Content-Length", _file.Length.ToString());
            Response.ContentType = "text/xml";

            const int bufferLength = 10000;
            byte[] buffer = new Byte[bufferLength];
            int length = 0;
            Stream download = null;

            try
            {
                // maak een nieuwe memorystream van de byte-array
                download = new MemoryStream(_file);
                do
                {
                    if (Response.IsClientConnected)
                    {
                        length = download.Read(buffer, 0, bufferLength);
                        Response.OutputStream.Write(buffer, 0, length);
                        buffer = new Byte[bufferLength];
                    }
                    else
                    {
                        length = -1;
                    }
                }
                while (length > 0);
                Response.Flush();
                Response.End();
            }
            finally
            {
                // sluit de download als de buffer leeg is
                if (download != null) { download.Close(); }
            }
        }

        // voer uit als de index van de dropdownbox wijzigt, is nodig voor het tonen van de file-upload box
        protected void DropDownAction_SelectedIndexChanged(object sender, EventArgs e)
        {
            // haal type actie op uit de dropdown
            EConnectorTypes _actionType = (EConnectorTypes)Enum.Parse(typeof(EConnectorTypes), DropDownAction.SelectedValue);
            // als updateconnector, dan file uploadbox laten zien
            if (_actionType == EConnectorTypes.UpdateConnector)
            {
                ShowLabels("UploadXML", true);
                vlUploadXML.Enabled = true;
            }
            // anders, file upload box niet laten zien
            else
            {
                ShowLabels("UploadXML", false);
                vlUploadXML.Enabled = false;
            }
        }

        // functie om makkelijk labels/divs aan en uit te zetten
        protected void ShowLabels(string _type, bool _show)
        {
            switch (_type)
            {
                case "Results":
                    resultHeader.Visible = _show;
                    resultTableContainer.Visible = _show;
                    resultTable.Visible = _show;
                    lbResultTable.Visible = _show;
                    lbResult.Visible = _show;
                    lbResultContainer.Visible = _show;
                    results.Visible = _show;
                    break;
                case "AOL":
                    tbAOLnumber.Visible = _show;
                    lbAOL.Visible = _show;
                    vlAOLnumber.Visible = _show;
                    break;
                case "Alert":
                    resultAlert.Visible = _show;
                    lbAlert.Visible = _show;
                    break;
                case "UploadXML":
                    lbUploadXMLFile.Visible = _show;
                    ulUploadXML.Visible = _show;
                    divUploadXML.Visible = _show;
                    break;
            }
        }
    }
}