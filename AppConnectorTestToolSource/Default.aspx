<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="AppConnectorTestToolSource.Default" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>App connector test-tool</title>
    <link href="Content/bootstrap.css" rel="stylesheet" type="text/css" />
    <style type="text/css">
        .auto-style1 {
            width: 21px;
            height: 19px;
        }
    </style>
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <script src="Scripts/jquery-3.2.1.min.js"></script>

    <script type="text/javascript">

        function copyAOLnumber() {
            var src = document.getElementById("tbAOLnumber").value;
            var dest = document.getElementById("tbUrl");
            url = "https://" + src + ".afasonlineconnector.nl";
            dest.value = url;
        }
    </script>

    <script type="text/javascript">
        $(function smoothScrollResult() {
            var result = $('#resultHeader')
            if (result.length) {
                $('html, body').animate({ scrollTop: $('#resultHeader').offset().top }, 'slow');
            }
        })
    </script>



</head>
<body>

    <div class="container" align="center">
        <div class="jumbotron">
            <h1>Welcome to the
                <br />
                AppConnector test-tool</h1>
        </div>
        <div class="alert alert-info" role="alert">Disclaimer: dit is <strong>niet</strong> een officiële tool van AFAS</div>
        <div>
            <form id="appConnectorTest" runat="server" method="post" enctype="multipart/form-data">
                <asp:HiddenField runat="server" ID="_repostcheckcode" />

                <div id="row">
                    <div class="panel panel-primary">
                        <div class="panel-heading">
                            <h3 class="panel-title">Settings</h3>
                        </div>
                        <div class="panel-body">
                            <p>
                                <asp:CheckBox ID="cbAFASOnline" runat="server" Text="AFAS Online" OnCheckedChanged="cbAFASOnline_CheckedChanged" AutoPostBack="true" Checked="True" />
                            </p>
                            <p>
                                <asp:Label ID="lbAOL" runat="server" Text="AFAS Online abonnementsnummer"></asp:Label>
                                <br />
                                <asp:TextBox ID="tbAOLnumber" placeholder="12345" runat="server" ClientIDMode="Static" onKeyUp="copyAOLnumber()" TabIndex="1"></asp:TextBox>
                            </p>
                            <p>
                                <asp:RegularExpressionValidator ID="vlAOLnumber" runat="server" ControlToValidate="tbAOLnumber" Display="Dynamic" ErrorMessage="Abonneenumber has 5 digits - Abonnementsnummer bestaat uit 5 cijfers" ForeColor="Red" ValidationExpression="\d{5}">Abonneenumber has 5 digits - Abonnementsnummer bestaat uit 5 cijfers</asp:RegularExpressionValidator>
                            </p>
                            <asp:Label ID="lbURL" runat="server" Text="Url"></asp:Label>
                            <p>
                                <asp:TextBox ID="tbUrl" runat="server" Width="600px" ClientIDMode="Static" Placeholder="https://12345.afasonlineconnector.nl" TabIndex="2"></asp:TextBox>
                                &nbsp;<a href="https://kb.afas.nl/index.php#URL van WebServices Profit Connectoren" target="_blank" tabindex="8"><img alt="[info]" class="auto-style1" src="questionmark.png" /></a>
                            </p>
                            <p>
                                <asp:RequiredFieldValidator ID="vlAOL" runat="server" ControlToValidate="tbUrl" Display="Dynamic" ErrorMessage="URL is required - URL is verplicht" ForeColor="#FF3300">URL is required - URL is verplicht</asp:RequiredFieldValidator>
                            </p>
                            <p>
                                <asp:Label ID="lbToken" runat="server" Text="Token"></asp:Label>
                            </p>
                            <p>
                                <asp:TextBox ID="tbToken" runat="server" Width="600px" placeholder="8077F20066EJ48CF67F62E53C493455FGB06856444FE480A4C174F9271B8F262" TabIndex="3"></asp:TextBox>
                                &nbsp;<a href="https://kb.afas.nl/index.php#Handmatig gebruikerstoken aan eigen app connector toevoegen" target="_blank" tabindex="9"><img alt="[info]" class="auto-style1" src="questionmark.png" /></a>
                            </p>
                            <p>
                                <asp:RequiredFieldValidator ID="vlToken" runat="server" ControlToValidate="tbToken" Display="Dynamic" ErrorMessage="Token is required - Token is verplicht" ForeColor="#FF3300">Token is required - Token is verplicht</asp:RequiredFieldValidator>
                                <asp:RegularExpressionValidator ID="vlTokenTags" runat="server" ControlToValidate="tbToken" Display="Dynamic" ErrorMessage="Fill only the token, without the tags - Vul alleen het token in, zonder de tags" ForeColor="#FF3300" ValidationExpression="^[a-zA-Z0-9]+$"></asp:RegularExpressionValidator>
                            </p>
                            <p>
                                <asp:Label ID="lbConnector" runat="server" Text="Connector"></asp:Label>
                            </p>
                            <p>
                                <asp:TextBox ID="tbConnector" runat="server" Width="600px" placeholder="ProfitCountries" TabIndex="4"></asp:TextBox>
                                <asp:Label ID="infoConnector" runat="server"><a href="https://kb.afas.nl/index.php#Eigen app connector toevoegen" target="_blank" tabindex="10"><img alt="[info]" class="auto-style1" src="questionmark.png" /></a></asp:Label>
                            </p>
                            <p>
                                <asp:RequiredFieldValidator ID="vlConnector" runat="server" ControlToValidate="tbConnector" Display="Dynamic" ErrorMessage="Connectorname is required - Connectornaam is verplicht" ForeColor="#FF3300">Connectorname is required - Connectornaam is verplicht</asp:RequiredFieldValidator>
                            </p>
                            <p>
                                <asp:Label ID="lbUploadXMLFile" runat="server" Text="Upload hier je XML-bestand"></asp:Label></p>
                            <div id="divUploadXML" runat="server" class="btn btn-default">
                                <input id="ulUploadXML" type="file" runat="server" />
                            </div>
                            <p>
                                <asp:RequiredFieldValidator ID="vlUploadXML" runat="server" ControlToValidate="ulUploadXML" Display="Dynamic" ErrorMessage="Kies een bestand - Choose a file" ForeColor="#FF3300">Kies een bestand - Choose a file</asp:RequiredFieldValidator>
                            </p>
                            <p>
                                <asp:Label ID="lbActie" runat="server" Text="Actie"></asp:Label>
                            </p>
                            <p>
                                <asp:DropDownList ID="DropDownAction" runat="server" OnSelectedIndexChanged="DropDownAction_SelectedIndexChanged" AutoPostBack="true" TabIndex="5">
                                </asp:DropDownList>
                            </p>
                        </div>
                    </div>
                </div>
                <p>
                    <asp:Button ID="btCheck" runat="server" Text="Test" class="btn btn-lg btn-success" OnClick="btCheck_Click" TabIndex="6" />
                    &nbsp;<asp:Button ID="btClear" runat="server" Text="Clear" class="btn btn-lg btn-default" OnClick="btClear_Click" TabIndex="7" />
                </p>
            </form>
        </div>

        <div class="page-header" runat="server" id="resultHeader">
            <h1>Results</h1>
        </div>
        <div class="well" runat="server" id="results">
            <h4><span class="label label-warning" id="resultGetWarning" runat="server">Let op! Omdat dit een testtool is worden alleen de eerste 20 resultaten getoond!<br />
                Warning! Because this is a testtool, only the first 20 result are showed!</span></h4>
            <p>
                <h4><span class="label label-success" runat="server" id="lbResultContainer">
                    <asp:Label ID="lbResult" runat="server" Text="[Results]"></asp:Label></span>
            </p>
        </div>

        <div class="well" runat="server" id="resultXML">
            <asp:Label ID="lbXML" runat="server" Text="[XML]"></asp:Label>
        </div>

        <div class="container" id="resultTableContainer" runat="server" style="width: inherit; overflow-x: scroll">
            <div class="col-md-6" id="resultTable" runat="server" style="text-align: left">
                <table class="table table-striped">
                    <asp:Literal ID="lbResultTable" runat="server" Text="[ResultTable]"></asp:Literal>
                </table>
            </div>
        </div>

        <div class="alert alert-danger" id="resultAlert" runat="server">
            <asp:Label ID="lbAlert" runat="server" Text="[Alert]"></asp:Label>
        </div>

    </div>



    <p align="center">
        ©<a href="http://www.johananvm.nl/">JvM</a> 2017
    </p>

</body>
</html>
