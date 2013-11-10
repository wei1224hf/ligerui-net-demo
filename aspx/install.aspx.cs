using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class WebContent_aspx_Default : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        tools.webPath = Request.PhysicalPath + "..\\..\\..\\";
        JavaScriptSerializer ser = new JavaScriptSerializer();
        Hashtable t = new Hashtable();

        String theclass = Request.QueryString.Get("class");

        if (theclass.Equals("basic_group")) t = basic_group.thefunction(Request);
        if (theclass.Equals("basic_user")) t = basic_user.thefunction(Request);

        if (theclass.Equals("test_il8n")) t = tools.readIl8n();
        if (theclass.Equals("test_importil8n")) t = tools.importIl8n2DB();
        if (theclass.Equals("test_xml")) t.Add("A", tools.getConfigItem("DB_UNM"));
        if (theclass.Equals("test_memory")) t = tools.initMemory();
        if (theclass.Equals("test_tableid")) t.Add("A", tools.getTableId("basic_user"));

        String jsonStr = ser.Serialize(t);
        Response.Write(jsonStr);
    }
}