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
        if (tools.dbType == null)
        {
            tools.initMemory();
        }

        String theclass = Request.QueryString.Get("class");

        if (theclass.Equals("basic_group")) t = basic_group.thefunction(Request);
        if (theclass.Equals("basic_user")) t = basic_user.thefunction(Request);

        String jsonStr = ser.Serialize(t);
        Response.Write(jsonStr);
    }
}