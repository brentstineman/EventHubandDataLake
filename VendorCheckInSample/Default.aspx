<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="VendorCheckInSample.Default" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
    
        <h2>Vendor Check-in</h2>
        Select an attendee and vendor from the options below and click &quot;check in&quot; to send a message to the Event Hub<br />
        <br />
        <strong>Attendee:</strong>
        <asp:DropDownList ID="ddlAttendee" runat="server" DataSourceID="sqsAttendees" DataTextField="Name" DataValueField="BusinessEntityID">
        </asp:DropDownList>
        <br />
        <strong>Vendor:</strong>
        <asp:DropDownList ID="ddlVendor" runat="server" DataSourceID="sqsVendors" DataTextField="Name" DataValueField="BusinessEntityID">
        </asp:DropDownList>
        <br />
        <asp:Button ID="btnCheckin" runat="server" Text="Check In" Width="178px" OnClientClick="doCheckin(); return false;" CausesValidation="False" />
<script type="text/javascript">
    function doCheckin() {
        // *****
        // Props to Sandrino Di Mattia of FabricConroller.net for his sample on which is
        // code is heavily taken from
        // see the original at: http://fabriccontroller.net/blog/posts/iot-with-azure-service-bus-event-hubs-authenticating-and-sending-from-any-type-of-device-net-and-js-samples/
        // ****

        // get selected Attendee
        var e = document.getElementById("ddlAttendee");
        var strAttendee = e.options[e.selectedIndex].value;

        // get selected Vendor
        var e = document.getElementById("ddlVendor");
        var strVendor = e.options[e.selectedIndex].value;

        // set up  parameters for EventHub
        var sas = "*shared access key*";

        // set up request 
        var xmlHttpRequest = new XMLHttpRequest();
        xmlHttpRequest.open('POST', 'https://<namespace>.servicebus.windows.net/<hubname>/publishers/vendor/messages', true);
        xmlHttpRequest.setRequestHeader('Content-Type', 'application/atom+xml;type=entry;charset=utf-8');
        xmlHttpRequest.setRequestHeader('Authorization', sas);
        // optional: set property on the message. 
        xmlHttpRequest.setRequestHeader('Type', 'VendorCheckin');
 
        eventMsg = "{ VendorID: " + strVendor + ", BadgeID: " + strAttendee + " }";
        response = xmlHttpRequest.send(eventMsg);

        window.alert("Sent event: " + eventMsg);
    }
</script>
    
    </div>
        <asp:SqlDataSource ID="sqsAttendees" runat="server" ConnectionString="*sql db connection string*" SelectCommand="SELECT TOP (25) FirstName + ' ' + LastName AS Name, BusinessEntityID FROM Person.Person ORDER BY BusinessEntityID"></asp:SqlDataSource>
        <asp:SqlDataSource ID="sqsVendors" runat="server" ConnectionString="*sql db connection string*" SelectCommand="SELECT TOP (25) BusinessEntityID, Name from Purchasing.Vendor order by BusinessEntityID"></asp:SqlDataSource>
    </form>
</body>
</html>
