using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MySql.Data.MySqlClient;
using System.Web.Script.Serialization;
using System.Data;
using NPOI.HSSF.UserModel;
using System.IO;
using NPOI.SS.UserModel;
using System.Data.SQLite;

public class basic_parameter
{
	public basic_parameter()
	{
	}

    public static Hashtable thefunction(HttpRequest request)
    {
        Hashtable t_return = new Hashtable();
        String functionName = request.QueryString.Get("function");
        if (functionName.Equals("grid"))
        {
            if (basic_user.checkPermission(request.Params["executor"], "120301", request.Params["session"]))
            {
                String sortname = "code";
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
        }
        if (functionName.Equals("loadConfig"))
        {
            t_return.Add("nothing",new String[]{"nothing","nothing"});
        }
        if (functionName.Equals("add"))
        {
            if (basic_user.checkPermission(request.Params["executor"], "120321", request.Params["session"]))
            {
                t_return = add(request.Params["data"], request.Params["executor"]);
            }
        }
        if (functionName.Equals("remove"))
        {
            if (basic_user.checkPermission(request.Params["executor"], "120323", request.Params["session"]))
            {
                t_return = remove(request.Params["usernames"], request.Params["executor"]);
            }
        }

        return t_return;
    }

    private static String search(String search, String executor)
    {
        String where = " where 1=1 ";
        Hashtable hshTable = new JavaScriptSerializer().Deserialize<Hashtable>(search);
        IDictionaryEnumerator en = hshTable.GetEnumerator();
        while (en.MoveNext())
        {
            String value = en.Value.ToString();
            String key = en.Key.ToString();

            if (key.Equals("name"))
            {
                where += " and reference like '%" + value + "%'";
            }
        }
        return where;
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
        String sql = "select * from basic_parameter";
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
        t_return.Add("Rows", a_data);
        rd.Close();

        sql = "select count(*) as count_ from basic_parameter " + where;
        comd.CommandText = sql;
        rd = comd.ExecuteReader();
        rd.Read();
        int total = rd.GetInt32(0);
        t_return.Add("Total", total);
        rd.Close();

        conn.Close();
        return t_return;
    }

    public static Hashtable add(String data, String executor)
    {
        Hashtable t_return = new Hashtable();
        SQLiteConnection conn = tools.getSqliteConn();
        SQLiteCommand comd = new SQLiteCommand();
        comd.Connection = conn;
        SQLiteDataReader rd = null;
        String sql = "";

        Hashtable t_data = new JavaScriptSerializer().Deserialize<Hashtable>(data);

        sql = "select * from basic_parameter where code = '" + t_data["code"] + "' and reference = '" + t_data["reference"] + "' ";
        comd.CommandText = sql;
        rd = comd.ExecuteReader();
        if (rd.Read())
        {
            t_return.Add("status", "2");
            t_return.Add("msg", "used already");
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

            t_data2.Add(key, "'" + value + "'");
        }
        t_data = t_data2;

        en = t_data.GetEnumerator();
        sql = "insert into basic_parameter ";
        String keys = "";
        String values = "";
        while (en.MoveNext())
        {
            String value = en.Value.ToString();
            String key = en.Key.ToString();

            keys += key + ",";
            values += value + ",";
        }

        sql = sql + "(" + keys.Substring(0, keys.Length - 1) + ") values (" + values.Substring(0, values.Length - 1) + ");";
        comd.CommandText = sql;
        comd.ExecuteNonQuery();

        conn.Close();
        t_return.Add("status", "1");
        return t_return;
    }

    public static Hashtable remove(String items, String executor)
    {
        Hashtable t_return = new Hashtable();
        SQLiteConnection conn = tools.getSqliteConn();
        SQLiteCommand comd = new SQLiteCommand();
        comd.Connection = conn;
        SQLiteDataReader rd = null;
        String sql = "";

        String[] item = items.Split(new Char[] { ',' });
        for (int i = 0; i < items.Length; i++)
        {
            sql = "delete from basic_parameter where id = '" + item[i] + "';";
            comd.CommandText = sql;
            comd.ExecuteNonQuery();
        }

        conn.Close();
        t_return.Add("status", "1");
        t_return.Add("count", items.Length);
        return t_return;
    }

    public static Hashtable resetMemory()
    {
        Hashtable t_return = new Hashtable();
        tools.initMemory();
        t_return.Add("status","1");
        return t_return;
    }
}