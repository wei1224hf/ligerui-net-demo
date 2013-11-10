using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MySql.Data.MySqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Data;
using System.Web.Script.Serialization;
using System.Data.SQLite;

public class basic_user
{
    public basic_user()
	{
	}

    public static Hashtable thefunction(HttpRequest request) {
        Hashtable t_return = new Hashtable();
        String functionName = request.QueryString.Get("function");
        if (functionName.Equals("grid"))
        {
            String sortname = "id";
            String sortorder = "asc";
            if (request.Params["sortname"] != null)
            {
                sortname = (String)request.Params["sortname"];
            }
            if (request.Params["sortorder"] != null)
            {
                sortorder = (String)request.Params["sortorder"];
            }
            t_return = grid(
                  (String)request.Params["search"]
                , (String)request.Params["pagesize"]
                , (String)request.Params["page"]
                , (String)request.Params["executor"]
                , sortname
                , sortorder
                );	
        }
        if (functionName.Equals("login"))
        {
            String username = request.Params["username"];
            String md5PasswordTime = request.Params["password"];
            String ip = request.UserHostAddress.ToString();
            String client = request.Browser.MajorVersion.ToString();
            String gis_lat= request.Params["username"];
            String gis_lot= request.Params["username"];
            t_return = basic_user.login(username, md5PasswordTime, ip, client, gis_lat, gis_lot);
        }
        if (functionName.Equals("loadConfig"))
        {
            t_return = loadConfig();
        }
        if (functionName.Equals("add"))
        {
            if (basic_user.checkPermission(request.Params["executor"], "120221", request.Params["session"]))
            {
                t_return = add(request.Params["data"],request.Params["executor"]);
            }
        }
        if (functionName.Equals("modify"))
        {
            if (basic_user.checkPermission(request.Params["executor"], "120222", request.Params["session"]))
            {
                t_return = modify(request.Params["data"], request.Params["executor"]);
            }
        }
        if (functionName.Equals("remove"))
        {
            if (basic_user.checkPermission(request.Params["executor"], "120223", request.Params["session"]))
            {
                t_return = remove(request.Params["usernames"], request.Params["executor"]);
            }
        }
        if (functionName.Equals("view"))
        {
            if (basic_user.checkPermission(request.Params["executor"], "120202", request.Params["session"]))
            {
                t_return = view(request.Params["id"]);
            }
        }
        if (functionName.Equals("loadConfig"))
        {
            t_return = loadConfig();
        }
        if (functionName.Equals("updateSession"))
        {
            t_return = updateSession(request.Params["executor"], request.Params["session"]);
        }
        if (functionName.Equals("group_get"))
        {
            if (basic_user.checkPermission(request.Params["executor"], "120241", request.Params["session"]))
            {
                t_return = group_get(request.Params["username"]);
            }
        }
        if (functionName.Equals("group_set"))
        {
            if (basic_user.checkPermission(request.Params["executor"], "120241", request.Params["session"]))
            {
                t_return = group_set(request.Params["username"], request.Params["group_codes"]);
            }
        }
        return t_return;
    }

    public static Hashtable loadConfig() 
    {
        Hashtable t_return = new Hashtable();
        SQLiteConnection conn = tools.getSqliteConn();
        SQLiteCommand comd = new SQLiteCommand();
        comd.Connection = conn;
        SQLiteDataReader rd = null;
        String sql = "";
        ArrayList a = null;

        String[] configs = { "basic_user__type", "basic_user__status" };

        for (int i = 0; i < configs.Count();i++ )
        {
            sql = "select code,value from basic_parameter where reference = '"+configs[i]+"' order by code;";
            comd.CommandText = sql;
            rd = comd.ExecuteReader();
            a = new ArrayList();
            while (rd.Read())
            {
                Hashtable t_item = new Hashtable();
                t_item.Add("code", rd.GetString(0));
                t_item.Add("value", rd.GetString(1));
                a.Add(t_item);
            }
            rd.Close();
            t_return.Add(configs[i],a);
        }

        sql = "select code,name as value from basic_group order by code;";
        comd.CommandText = sql;
        rd = comd.ExecuteReader();
        a = new ArrayList();
        while (rd.Read())
        {
            Hashtable t_item = new Hashtable();
            t_item.Add("code", rd.GetString(0));
            t_item.Add("value", rd.GetString(1));
            a.Add(t_item);
        }
        t_return.Add("group", a);
        rd.Close();

        conn.Close();
        return t_return;
    }

    public static Hashtable login(
              String username
            , String md5PasswordTime
            , String ip
            , String client
            , String gis_lat
            , String gis_lot)
    {
        Hashtable t_return = new Hashtable();
        SQLiteConnection conn = tools.getSqliteConn();
        SQLiteCommand comd = new SQLiteCommand();
        comd.Connection = conn;
        SQLiteDataReader rd = null;
        String sql = "";

        Boolean failed = false;
        sql = tools.getSQL("basic_user__login_check").Replace("__username__", username);
        comd.CommandText = sql;
        rd = comd.ExecuteReader(CommandBehavior.KeyInfo);        
        if (!rd.Read()){
            t_return.Add("msg", "username wrong");
            t_return.Add("status", "2");
            failed = true;
        }
        else{
            if(!username.Equals("guest")){
                String password_ = rd.GetString(1);
                String password__md5 = tools.MD5_(password_+ DateTime.Now.Hour.ToString() );
				String password__md52 = tools.MD5_(password_+ (DateTime.Now.Hour-1).ToString() );
                if(!(password__md5.Equals(md5PasswordTime)||password__md52.Equals(md5PasswordTime))){
				    t_return.Add("status", "2");
				    t_return.Add("msg", "password wrong");
				    failed = true;
			    }
            }
        }

        if(!failed){
            if (!(rd.GetString(10).Equals(ip) && rd.GetString(11).Equals(client))){
                t_return.Add("msg", ((Hashtable)tools.il8n["basic_user"])["kickOff"]);
                t_return.Add("status", "3");
            }
            else {
                t_return.Add("msg", "ok");
                t_return.Add("status", "1");
            }         
                
            double session_ = new Random().NextDouble()*1000000;
            String session__ = (int)session_ + "";
            String session = tools.MD5_(session__);
                
            DataTable schemaTable = rd.GetSchemaTable();
            Hashtable t_data = new Hashtable();
            for (int i = 0; i < schemaTable.Rows.Count;i++ )
            {
                String key = schemaTable.Rows[i].ItemArray[0].ToString();
                String value = "-";
                if (!rd.IsDBNull(i))
                {
                    String type = rd.GetDataTypeName(i);
                    if (type.Equals("decimal") || type.Equals("int"))
                    {
                        value = rd.GetInt32(i).ToString();
                    }
                    else 
                    {
                        value = rd.GetString(i);
                    }                    
                }
                t_data.Add(key, value);
            }
            rd.Close();   
            t_return["il8n"] = tools.readIl8n();
            t_return["zone"] = tools.getConfigItem("ZONE");

            sql = tools.getSQL("basic_user__login_logout").Replace("__user_code__", "'" + username + "'");
            comd.CommandText = sql;
            comd.ExecuteNonQuery();

            sql = tools.getSQL("basic_user__login_session");
            sql = sql.Replace("__username__", "'" + username + "'");
            sql = sql.Replace("__permissions__", "'" + basic_user.getPermission(username,conn) + "'");
            sql = sql.Replace("__session__", "'" + session + "'");
            sql = sql.Replace("__ip__", "'" + ip + "'");
            sql = sql.Replace("__client__", "'" + client + "'");
            sql = sql.Replace("__gis_lat__", "'" + gis_lat + "'");
            sql = sql.Replace("__gis_lot__", "'" + gis_lot + "'");
            comd.CommandText = sql;
            comd.ExecuteNonQuery();

            String session_return = tools.MD5_(session + DateTime.Now.Hour.ToString() );
            t_data["session"] = session_return ;
            t_return["hour"] = DateTime.Now.Hour.ToString();
            t_return.Add("logindata", t_data);            
        }
        conn.Close();

        t_return["permissions"] = basic_user.getPermissionTree(username);

        return t_return;
    }

    public static ArrayList getPermissionTree(String username)
    {
        Hashtable t_return = new Hashtable();
        SQLiteConnection conn = tools.getSqliteConn();
        SQLiteCommand comd = new SQLiteCommand();
        comd.Connection = conn;
        SQLiteDataReader rd = null;
        String sql = "";

        ArrayList a_return = new ArrayList();
        sql = tools.getSQL("basic_user__getPermission");
        sql = sql.Replace("__username__",  username );
        comd.CommandText = sql;
        rd = comd.ExecuteReader();
       
        while (rd.Read())
        {
            DataTable schemaTable = rd.GetSchemaTable();
            Hashtable t_data = new Hashtable();
            for (int i = 0; i < schemaTable.Rows.Count; i++)
            {
                String key = schemaTable.Rows[i].ItemArray[0].ToString();
                String value = "-";
                if (!rd.IsDBNull(i))
                {
                    String type = rd.GetDataTypeName(i);
                    if (type.Equals("decimal") || type.Equals("int"))
                    {
                        value = rd.GetInt32(i).ToString();
                    }
                    else
                    {
                        value = rd.GetString(i);
                    }
                }
                t_data.Add(key, value);
            }
            a_return.Add(t_data);
        }
        a_return = tools.list2Tree(a_return);	

        conn.Close();
        conn = null;
        return a_return;
    }

    private static String getPermission(String username,SQLiteConnection conn)
    {
        String s_return = "";
        SQLiteCommand comd = new SQLiteCommand();
        comd.Connection = conn;
        SQLiteDataReader rd = null;
        String sql = "";

        sql = tools.getSQL("basic_user__getPermission").Replace("__username__", username);
        comd.CommandText = sql;
        rd = comd.ExecuteReader();
        
        while (rd.Read()) 
        {
            s_return += rd.GetString(2) + ",";
        }
        s_return = s_return.Substring(0, s_return.Length - 1);
        rd.Close();
        return s_return;
    }

    private static String search(String search,String executor) 
    {
        String where = " where 1=1 ";
        Hashtable hshTable = new JavaScriptSerializer().Deserialize<Hashtable>(search);
        IDictionaryEnumerator en = hshTable.GetEnumerator();
        while (en.MoveNext())
        {
            String value = en.Value.ToString();
            String key = en.Key.ToString();

            if (key.Equals("username"))
            {
                where += " and username like '%" + value + "%'";
            }
            if (key.Equals("group_code"))
            {
                where += " and group_code = '" + value + "'";
            }
            if (key.Equals("type"))
            {
                where += " and type = '" + value + "'";
            }
            if (key.Equals("status"))
            {
                where += " and status = '" + value + "'";
            }
        }
        return where;
    }

    public static Hashtable getSession(String executor)
    {
        Hashtable t_return = new Hashtable();
        SQLiteConnection conn = tools.getSqliteConn();
        SQLiteCommand comd = new SQLiteCommand();
        comd.Connection = conn;
        SQLiteDataReader rd = null;
        String sql = "";

        sql = "select user_type,group_code,groups from basic_user_session where user_code = '" + executor + "'";
        comd.CommandText = sql;
        rd = comd.ExecuteReader();
        if (rd.Read())
        {
            DataTable schemaTable = rd.GetSchemaTable();
            Hashtable t_data = new Hashtable();
            for (int i = 0; i < schemaTable.Rows.Count; i++)
            {
                String key = schemaTable.Rows[i].ItemArray[0].ToString();
                String value = "-";
                if (!rd.IsDBNull(i)) value = rd.GetString(i);
                t_data.Add(key, value);
            }
            rd.Close();
            t_return = t_data;
        }
        
        conn.Close();
        return t_return;
    }

    public static Hashtable grid(
              String search_
            , String pagesize
            , String pagenum
            , String executor
            , String sortname
            , String sortorder)
    {
        Hashtable t_return = new Hashtable();
        SQLiteConnection conn = tools.getSqliteConn();
        SQLiteCommand comd = new SQLiteCommand();
        comd.Connection = conn;
        SQLiteDataReader rd = null;

        ArrayList a_data = new ArrayList();
        String where = search(search_, executor);
        String sql = tools.getSQL("basic_user__grid");
        sql += where + " order by " + sortname + " " + sortorder + "  limit " + (int.Parse(pagesize) * (int.Parse(pagenum) - 1)) + "," + pagesize + ";";
        comd.CommandText = sql;
        rd = comd.ExecuteReader();
        while (rd.Read())
        {
            DataTable schemaTable = rd.GetSchemaTable();
            Hashtable t_data = new Hashtable();
            for (int i = 0; i < schemaTable.Rows.Count; i++)
            {
                String key = schemaTable.Rows[i].ItemArray[0].ToString();
                String value = "-";
                if (!rd.IsDBNull(i))
                {
                    String type = rd.GetDataTypeName(i);
                    if (type.Equals("decimal") || type.Equals("int"))
                    {
                        value = rd.GetInt32(i).ToString();
                    }
                    else
                    {
                        value = rd.GetString(i);
                    }
                }
                t_data.Add(key, value);
            }
            a_data.Add(t_data);
        }
        t_return.Add("Rows",a_data);
        rd.Close();

        sql = "select count(*) as count_ from basic_user " + where;
        comd.CommandText = sql;
        rd = comd.ExecuteReader();
        rd.Read();
        int total = rd.GetInt32(0);
        t_return.Add("Total",total);
        rd.Close();

        conn.Close();
        return t_return;
    }

    public static Hashtable add(String data,String executor)
    {
        Hashtable t_return = new Hashtable();
        SQLiteConnection conn = tools.getSqliteConn();
        SQLiteCommand comd = new SQLiteCommand();
        comd.Connection = conn;
        SQLiteDataReader rd = null;
        String sql = "";

        Hashtable t_data = new JavaScriptSerializer().Deserialize<Hashtable>(data);

        sql = "select * from basic_user where username = '"+t_data["username"]+"'";
        comd.CommandText = sql;
        rd = comd.ExecuteReader();
        if(rd.Read())
        {
            t_return.Add("status","2");
            t_return.Add("msg","username used already");
            rd.Close();
            conn.Close();
            return t_return; 
        }
        rd.Close();

        IDictionaryEnumerator en = t_data.GetEnumerator();
        Hashtable t_data2 = new Hashtable();
        while (en.MoveNext())
        {
            String value = en.Value.ToString();
            String key = en.Key.ToString();

            t_data2.Add(key , "'" + value + "'");
        }
        t_data = t_data2;
        int id = tools.getTableId("basic_user")+1;
        t_data.Add("id", id);
        t_data.Add("creater_code", "'" + executor + "'");
        t_data.Add("creater_group_code", "(select group_code from basic_group_2_user where user_code = '" + executor + "' order by group_code limit 1 )");

        en = t_data.GetEnumerator();
        sql = "insert into basic_user ";
        String keys = "";
        String values = "";
        while (en.MoveNext())
        {
            String value = en.Value.ToString();
            String key = en.Key.ToString();

            keys += key + ",";
            values += value + ",";
        }

        sql = sql + "(" + keys.Substring(0, keys.Length - 1) + ") values (" + values.Substring(0,values.Length-1) + ");";
        comd.CommandText = sql;
        comd.ExecuteNonQuery();

        String user_code = (String) t_data["username"];
		String group_code = (String) t_data["group_code"];
			
		sql = "insert into basic_group_2_user (user_code,group_code) values ("+user_code+","+group_code+");";
        comd.CommandText = sql;
        comd.ExecuteNonQuery();

        conn.Close();
        t_return.Add("status","1");
        t_return.Add("id",id);
        return t_return;
    }

    public static Hashtable add_register(String data)
    {
        data = data.Substring(0, data.Length - 1) + ",\"type\":\"99\",\"status\":\"10\"}";
        return add(data, "admin");
    }

    public static Hashtable remove(String usernames, String executor) 
    {
        Hashtable t_return = new Hashtable();
        SQLiteConnection conn = tools.getSqliteConn();
        SQLiteCommand comd = new SQLiteCommand();
        comd.Connection = conn;
        SQLiteDataReader rd = null;
        String sql = "";

        String[] username = usernames.Split(new Char[]{','});
        for (int i = 0; i < username.Length; i++)
        {
            sql = "delete from basic_user where username = '" + username[i] + "';";
            comd.CommandText = sql;
            comd.ExecuteNonQuery();
            sql = "delete from basic_group_2_user where user_code = '" + username[i] + "';";
            comd.CommandText = sql;
            comd.ExecuteNonQuery();
        }

        conn.Close();
        t_return.Add("status","1");
        t_return.Add("count", username.Length);
        return t_return;
    }

    public static Hashtable modify(String data, String executor)
    {
        Hashtable t_return = new Hashtable();
        SQLiteConnection conn = tools.getSqliteConn();
        SQLiteCommand comd = new SQLiteCommand();
        comd.Connection = conn;
        SQLiteDataReader rd = null;
        String sql = "";

        Hashtable t_data = new JavaScriptSerializer().Deserialize<Hashtable>(data);
        String username = (String)t_data["username"];
        t_data.Remove("username");
        String group_code = (String)t_data["group_code"];

        sql = "delete from basic_group_2_user where user_code = '" + username + "' ;";
        comd.CommandText = sql;
        comd.ExecuteNonQuery();
        sql = "insert into basic_group_2_user (user_code,group_code) values ('" + username + "','" + group_code + "');";
        comd.CommandText = sql;
        comd.ExecuteNonQuery();

        IDictionaryEnumerator en = t_data.GetEnumerator();
        Hashtable t_data2 = new Hashtable();
        while (en.MoveNext())
        {
            String value = en.Value.ToString();
            String key = en.Key.ToString();

            t_data2.Add(key, "'" + value + "'");
        }
        t_data = t_data2;
        t_data.Add("time_lastupdated", "now()");
        t_data.Add("count_updated", "count_updated+1");	

        en = t_data.GetEnumerator();
        sql = "update basic_user set ";
        int columns = 0;
        while (en.MoveNext())
        {
            columns++;
            String value = en.Value.ToString();
            String key = en.Key.ToString();

            sql += key+ " = "+value+" ,";
        }
        sql = sql.Substring(0, sql.Length - 1);
        sql += " where username = '"+username+"'";
        comd.CommandText = sql;
        comd.ExecuteNonQuery();

        conn.Close();
        t_return.Add("status","1");
        t_return.Add("columns",columns);
        return t_return;
    }

    public static Hashtable view(String id)
    {
        Hashtable t_return = new Hashtable();
        SQLiteConnection conn = tools.getSqliteConn();
        SQLiteCommand comd = new SQLiteCommand();
        comd.Connection = conn;
        SQLiteDataReader rd = null;
        String sql = "";

        sql = tools.getSQL("basic_user__view").Replace("__id__", id );
        comd.CommandText = sql;
        rd = comd.ExecuteReader();
        if (rd.Read())
        {
            DataTable schemaTable = rd.GetSchemaTable();
            Hashtable t_data = new Hashtable();
            for (int i = 0; i < schemaTable.Rows.Count; i++)
            {
                String key = schemaTable.Rows[i].ItemArray[0].ToString();
                String value = "-";
                if (!rd.IsDBNull(i))
                {
                    String type = rd.GetDataTypeName(i);
                    if (type.Equals("decimal") || type.Equals("int"))
                    {
                        value = rd.GetInt32(i).ToString();
                    }
                    else
                    {
                        value = rd.GetString(i);
                    }
                }
                t_data.Add(key, value);
            }
            t_return.Add("status","1");
            t_return.Add("data", t_data);
        }
        else 
        {
            t_return.Add("status", "2");
            t_return.Add("msg", "No such item");
        }

        rd.Close();
        conn.Close();
        return t_return;
    }

    public static Hashtable logout(String username, String session)
    {
        Hashtable t_return = new Hashtable();
        SQLiteConnection conn = tools.getSqliteConn();
        SQLiteCommand comd = new SQLiteCommand();
        comd.Connection = conn;
        SQLiteDataReader rd = null;
        String sql = "";

        sql = tools.getSQL("basic_user__logout").Replace("__user_code__", "'" + username + "'").Replace("__session__", "'" + session + "'");
        comd.CommandText = sql;
        comd.ExecuteNonQuery();

        if(rd!=null)rd.Close();
        conn.Close();
        return t_return;
    }

    public static Hashtable updateSession(String user_code, String session)
    {
        Hashtable t_return = new Hashtable();
        SQLiteConnection conn = tools.getSqliteConn();
        SQLiteCommand comd = new SQLiteCommand();
        comd.Connection = conn;
        SQLiteDataReader rd = null;
        String sql = "";

        double session_ = new Random().NextDouble() * 1000000;
        String session__ = (int)session_ + "";
        String r_session = tools.MD5_(session__);

        sql = tools.getConfigItem("basic_user__session_update")
                .Replace("__user_code__", "'" + user_code + "'")
                .Replace("__r_session__", "'" + r_session + "'")
                .Replace("__session__", "'" + session + "'");

        comd.CommandText = sql;
        comd.ExecuteNonQuery();

        t_return.Add("session",r_session);

        if (rd != null) rd.Close();
        conn.Close();
        return t_return;
    }

    public static Hashtable group_set(String username, String group_codes)
    {
        Hashtable t_return = new Hashtable();
        SQLiteConnection conn = tools.getSqliteConn();
        SQLiteCommand comd = new SQLiteCommand();
        comd.Connection = conn;
        SQLiteDataReader rd = null;
        String sql = "";

        sql = "delete from basic_group_2_user where user_code = '" + username + "' ";
        comd.CommandText = sql;
        comd.ExecuteNonQuery();

        String[] codes = group_codes.Split(new Char[]{','});
        for (int i = 0; i < codes.Length; i++)
        {
            sql = "insert into basic_group_2_user (user_code,group_code) values ( '" + username + "','" + codes[i] + "'); ";
            comd.CommandText = sql;
            comd.ExecuteNonQuery();
        }

        t_return.Add("status", "1");
        t_return.Add("msg", "ok");

        if (rd != null) rd.Close();
        conn.Close();
        return t_return;
    }

    public static Hashtable group_get(String username)
    {
        Hashtable t_return = new Hashtable();
        SQLiteConnection conn = tools.getSqliteConn();
        SQLiteCommand comd = new SQLiteCommand();
        comd.Connection = conn;
        SQLiteDataReader rd = null;
        String sql = "";

        sql = tools.getSQL("basic_user__group_get").Replace("__username__", username);
        comd.CommandText = sql;
        rd = comd.ExecuteReader();
        ArrayList array = new ArrayList();
		while (rd.Read()) 
        {			
			Hashtable t = new Hashtable();
			t.Add("name", rd.GetString(0));	
			t.Add("code", rd.GetString(1).Replace("-",""));
            t.Add("code_", rd.GetString(1));
				
			if(!rd.IsDBNull(2))
            {
				t.Add("ischecked", 1);
			}
				
			array.Add(t);
		}
		array = tools.list2Tree(array);	
			
		t_return.Add("groups", array);
		t_return.Add("status", "1");
        t_return.Add("msg", "ok");

        if (rd != null) rd.Close();
        conn.Close();
        return t_return;
    }

    public static Boolean checkPermission(String user_code, String actioncode, String session)
    {
        Boolean b_return = true;
        SQLiteConnection conn = tools.getSqliteConn();
        SQLiteCommand comd = new SQLiteCommand();
        comd.Connection = conn;
        SQLiteDataReader rd = null;
        String sql = "";

        sql = tools.getSQL("basic_user__getSession").Replace("__user_code__", user_code);
        comd.CommandText = sql;
        rd = comd.ExecuteReader();
        if (rd.Read()) {
            if (!user_code.Equals("guest")){
                String session_ = rd.GetString(4);

                String session_md5 = tools.MD5_(session_ + DateTime.Now.Hour.ToString());
                String session_md52 = tools.MD5_(session_ + (DateTime.Now.Hour - 1).ToString());
                if (!(session_md5.Equals(session) || session_md52.Equals(session)))
                {
                    b_return = false;
                }
            }
            rd.Close();
            sql = "update basic_user_session set lastaction = '" + actioncode + "' , lastactiontime = '" + DateTime.Now.Hour + "' , count_actions = count_actions + 1 where user_code = '" + user_code + "' ;";
            comd.CommandText = sql;
            comd.ExecuteNonQuery();
        }
        else{
            b_return = false;
        }

        if (rd != null) rd.Close();
        conn.Close();
        return b_return;
    }

    //TODO
    public static int data4test_commit = 0;
	//每个部门1个负责人,8个左右的员工
    public static Hashtable data4test()
    {
        int c_total = 0;
        Hashtable t_return = new Hashtable();
        MySqlConnection conn = tools.getConn();
        MySqlCommand comd = new MySqlCommand();
        MySqlDataReader rd = null;
        comd.Connection = conn;
        String sql = "";

        String zone = tools.getConfigItem("ZONE");
        comd.CommandText = "DELETE FROM basic_user where username like '" + zone + "%';";
        comd.ExecuteNonQuery();
        comd.CommandText = "DELETE FROM basic_group_2_user where user_code like '" + zone + "%';";
        comd.ExecuteNonQuery();
        comd.CommandText = "DELETE FROM oa_person where remark = '" + zone + "';";
        comd.ExecuteNonQuery();
        comd.CommandText = "START TRANSACTION;";
        comd.ExecuteNonQuery();

        DateTime dt1 = new DateTime(1949,1,1);
        DateTime dt2 = new DateTime(2010,1,1);
        Random rand = new Random();

        int id__oa_person = tools.getTableId("oa_person");
        int id__basic_user = tools.getTableId("basic_user");

        String sql_select = "select * from basic_group where type like '30%' or type like '40%' ";
        MySqlCommand comd_read = new MySqlCommand();
        MySqlConnection conn_read = tools.getConn();
        comd_read.Connection = conn_read;
        comd_read.CommandText = sql_select;
        rd = comd_read.ExecuteReader();
        while (rd.Read())
        { 
            String group_code = rd.GetString("code");
		    String[] group_code_ = group_code.Split(new Char[]{'-'});
            int len = group_code_[0].Length;

            int count_person = (int)(rand.NextDouble() * 4 + 4);
            for (int i = 0; i < count_person; i++)
            {
                String username = group_code + "-" + i;
                String group_all = group_code;
                String type = "";
                if (len == 10) type = "1";
                if (len == 8) type = "2";
                if (len == 6) type = "3";
                if (group_code_.Length == 2)
                {
                    type = type + "3";
                    i = count_person;
                    sql = "update basic_group set chief_code = '" + username + "' where code = '" + group_code + "'";
                    comd.CommandText = sql;
                    comd.ExecuteNonQuery();
                    group_all = group_code + ",X1";
                    String sql__basic_group_2_user = "insert into basic_group_2_user(user_code,group_code) values ('" + username + "','X1');";
                    comd.CommandText = sql__basic_group_2_user;
                    comd.ExecuteNonQuery();
                }
                else
                {
                    type = (i == 0 ? type + "2" : type + "1");
                }
                if (i == 0)
                {
                    sql = "update basic_group set chief_code = '" + username + "' where code = '" + group_code + "'";
                    comd.CommandText = sql;
                    comd.ExecuteNonQuery();
                    group_all = group_code + ",X1";
                    String sql__basic_group_2_user = "insert into basic_group_2_user(user_code,group_code) values ('" + username + "','X1');";
                    comd.CommandText = sql__basic_group_2_user;
                    comd.ExecuteNonQuery();
                }

                id__oa_person++;
                id__basic_user++;

                DateTime dt = dt1.AddSeconds( (dt2 - dt1).TotalSeconds * rand.NextDouble() );
                String name = tools.randomName();
                String sql__oa_person = "insert into oa_person (name,birthday,photo,gender,nation,marriage,degree,politically,address_birth_code,id,creater_code,creater_group_code,remark) values ("
                        + " '" + name + "'"
                        + ",'" + dt.ToString("yyyy-MM-dd") + "'"
                        + ",'../file/upload/photo/" + (int)(rand.NextDouble() * 2 + 1) + "/" + (int)(rand.NextDouble() * 30 + 1) + ".jpg'"
                        + ",'" + (int)(rand.NextDouble() * 2 + 1) + "'"
                        + ",'" + (rand.NextDouble() > 0.5 ? 1 : (int)(rand.NextDouble() * 56 + 1)) + "'"
                        + ",'" + (rand.NextDouble() > 0.8 ? 1 : 2) + "'"
                        + ",'" + (int)(rand.NextDouble() * 9 + 1) + "0'"
                        + ",'" + (int)(rand.NextDouble() * 13 + 1) + "'"
                        + ",'00'"
                        + ",'" + id__oa_person + "'"
                        + ",'admin'"
                        + ",'10'"
                        + ",'" + zone + "'"
                        + ");";
                //System.out.println(sql__oa_person);
                comd.CommandText = sql__oa_person;
                comd.ExecuteNonQuery();

                String sql__basic_user = "insert into basic_user(username,password,id,group_code,group_all,type,status,id_person) values ('" + username + "',md5('" + username + "'),'" + id__basic_user + "','" + group_code + "','" + group_all + "','" + type + "','10','" + id__oa_person + "');";
                comd.CommandText = sql__basic_user;
                comd.ExecuteNonQuery();                

                String sql__basic_group_2_user_ = "insert into basic_group_2_user(user_code,group_code) values ('" + username + "','" + group_code + "');";
                comd.CommandText = sql__basic_group_2_user_;
                comd.ExecuteNonQuery();
                c_total++;

                basic_user.data4test_commit++;
                if (basic_user.data4test_commit >= 15000)
                {
                    basic_user.data4test_commit = 0;
                    comd.CommandText = "COMMIT;";
                    comd.ExecuteNonQuery();
                    comd.CommandText = "START TRANSACTION;";
                    comd.ExecuteNonQuery();
                }
            }
        }
        comd.CommandText = "COMMIT;";
        comd.ExecuteNonQuery();
        comd.CommandText = "START TRANSACTION;";
        comd.ExecuteNonQuery();

        t_return.Add("c_total", c_total);

        if (rd != null) rd.Close();
        conn_read.Close();
        conn.Close();
        return t_return;
    }
}