using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MySql.Data.MySqlClient;
using System.Web.Script.Serialization;
using System.Data;
using System.IO;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using System.Data.SQLite;

public class basic_group
{
    public basic_group()
	{
	}

    public static Hashtable thefunction(HttpRequest request) {
        Hashtable t_return = new Hashtable();
        String functionName = request.QueryString.Get("function");
        if (functionName.Equals("grid"))
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
        if (functionName.Equals("loadConfig"))
        {
            t_return = loadConfig();
        }
        if (functionName.Equals("add"))
        {
            if (basic_user.checkPermission(request.Params["executor"], "120121", request.Params["session"]))
            {
                t_return = add(request.Params["data"], request.Params["executor"]);
            }
        }
        if (functionName.Equals("modify"))
        {
            if (basic_user.checkPermission(request.Params["executor"], "120122", request.Params["session"]))
            {
                t_return = modify(request.Params["data"], request.Params["executor"]);
            }
        }
        if (functionName.Equals("remove"))
        {
            if (basic_user.checkPermission(request.Params["executor"], "120123", request.Params["session"]))
            {
                t_return = remove(request.Params["usernames"], request.Params["executor"]);
            }
        }
        if (functionName.Equals("view"))
        {
            if (basic_user.checkPermission(request.Params["executor"], "120102", request.Params["session"]))
            {
                t_return = view(request.Params["id"]);
            }
        }
        if (functionName.Equals("permission_get"))
        {
            if (basic_user.checkPermission(request.Params["executor"], "120140", request.Params["session"]))
            {
                t_return = permission_get(request.Params["code"]);
            }
        }
        if (functionName.Equals("permission_set"))
        {
            if (basic_user.checkPermission(request.Params["executor"], "120140", request.Params["session"]))
            {
                t_return = permission_set(request.Params["code"], request.Params["codes"]);
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

        String[] configs = { "basic_group__status", "basic_group__type" };

        for (int i = 0; i < configs.Count(); i++)
        {
            sql = "select code,value from basic_parameter where reference = '" + configs[i] + "' order by code;";
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
            t_return.Add(configs[i], a);
        }

        conn.Close();
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
                where += " and name like '%" + value + "%'";
            }
            if (key.Equals("type"))
            {
                where += " and type = '" + value + "'";
            }
            if (key.Equals("code"))
            {
                where += " and ( ( code like '" + value + "__' ) or (code = '" + value + "') )";
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
        String sql = tools.getSQL("basic_group__grid");
        sql += where + " order by "+sortname+" "+sortorder+"  limit " + (int.Parse(pagesize) * (int.Parse(pagenum) - 1)) + "," + pagesize + ";";
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

        sql = "select count(*) as count_ from basic_group " + where;
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

        sql = "select * from basic_group where code = '" + t_data["code"] + "'";
        comd.CommandText = sql;
        rd = comd.ExecuteReader();
        if (rd.Read())
        {
            t_return.Add("status", "2");
            t_return.Add("msg", "code used already");
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
        sql = "insert into basic_group ";
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

    public static Hashtable modify(String data, String executor)
    {
        Hashtable t_return = new Hashtable();
        SQLiteConnection conn = tools.getSqliteConn();
        SQLiteCommand comd = new SQLiteCommand();
        comd.Connection = conn;
        SQLiteDataReader rd = null;

        String sql = "";
        Hashtable t_data = new JavaScriptSerializer().Deserialize<Hashtable>(data);
        String code = (String)t_data["code"];
        t_data.Remove("code");

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
        sql = "update basic_group set ";
        int columns = 0;
        while (en.MoveNext())
        {
            columns++;
            String value = en.Value.ToString();
            String key = en.Key.ToString();

            sql += key + " = " + value + " ,";
        }
        sql = sql.Substring(0, sql.Length - 1);
        sql += " where code = '" + code + "'";
        comd.CommandText = sql;
        comd.ExecuteNonQuery();

        conn.Close();
        t_return.Add("status", "1");
        t_return.Add("columns", columns);
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
            sql = "delete from basic_group where code = '" + item[i] + "';";
            comd.CommandText = sql;
            comd.ExecuteNonQuery();
            sql = "delete from basic_group_2_user where group_code = '" + item[i] + "';";
            comd.CommandText = sql;
            comd.ExecuteNonQuery();

            sql = "delete from basic_group_2_permission where group_code = '" + item[i] + "';";
            comd.CommandText = sql;
            comd.ExecuteNonQuery();
        }

        conn.Close();
        t_return.Add("status", "1");
        t_return.Add("count", items.Length);
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

        sql = tools.getSQL("basic_group__view").Replace("__id__",  id );
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
            t_return.Add("status", "1");
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

    public static Hashtable permission_get(String code)
    {
        Hashtable t_return = new Hashtable();
        SQLiteConnection conn = tools.getSqliteConn();
        SQLiteCommand comd = new SQLiteCommand();
        comd.Connection = conn;
        SQLiteDataReader rd = null;
        String sql = "";

        sql = tools.getConfigItem("basic_group__permission_get").Replace("__group_code__", "'" + code + "'");
        comd.CommandText = sql;
        rd = comd.ExecuteReader();
        ArrayList array = new ArrayList();
		while (rd.Read()) 
        {			
			Hashtable t = new Hashtable();
			t.Add("name", rd.GetString(0));	
			t.Add("code", rd.GetString(1));
            t.Add("icon", rd.GetString(2));
				
			if(!rd.IsDBNull(4))
            {
				t.Add("ischecked", 1);
                t.Add("cost", rd.GetString(3));
                t.Add("credits", rd.GetString(4));
			}
				
			array.Add(t);
		}
		array = tools.list2Tree(array);	

        t_return.Add("permissions", array);
		t_return.Add("status", "1");
        t_return.Add("msg", "ok");

        if (rd != null) rd.Close();
        conn.Close();
        return t_return;
    }

    public static Hashtable permission_set(String code, String items)
    {
        Hashtable t_return = new Hashtable();
        SQLiteConnection conn = tools.getSqliteConn();
        SQLiteCommand comd = new SQLiteCommand();
        comd.Connection = conn;
        SQLiteDataReader rd = null;
        String sql = "";

        sql = "delete from basic_group_2_permission where group_code = '" + code + "' ";
        comd.CommandText = sql;
        comd.ExecuteNonQuery();

        String[] codes = items.Split(new Char[] { ',' });
        for (int i = 0; i < codes.Length; i++)
        {
            sql = "insert into basic_group_2_permission (group_code,permission_code) values ( '" + code + "','" + codes[i] + "'); ";
            comd.CommandText = sql;
            comd.ExecuteNonQuery();
        }

        t_return.Add("status", "1");
        t_return.Add("msg", "ok");

        if (rd != null) rd.Close();
        conn.Close();
        return t_return;
    }

    public static Hashtable upload(String path, String executor)
    {
        Hashtable t_return = new Hashtable();
        SQLiteConnection conn = tools.getSqliteConn();
        SQLiteCommand comd = new SQLiteCommand();
        comd.Connection = conn;
        SQLiteDataReader rd = null;
        String sql = "";

        String[] sql_user = new String[9];
        sql_user[0] = "delete from basic_user;";
        sql_user[1] = "insert into basic_user(username,password,group_code,group_all,id,type,status) values ('admin',md5('admin'),'10','10',1,'10','10');";
        sql_user[2] = "insert into basic_user(username,password,group_code,group_all,id,type,status) values ('guest',md5('guest'),'99','99',2,'10','10');";
        sql_user[3] = "delete from basic_group_2_user;";
        sql_user[4] = "insert into basic_group_2_user(user_code,group_code) values ('admin','10');";
        sql_user[5] = "insert into basic_group_2_user(user_code,group_code) values ('guest','99');";

        sql_user[6] = "delete from basic_group_2_permission;";
        sql_user[7] = "delete from basic_permission;";
        sql_user[8] = "delete from basic_group;";

        for (int i = 0; i < 9; i++)
        {
            comd.CommandText = sql_user[i];
            comd.ExecuteNonQuery();
        }	

        HSSFWorkbook excel;
        using (FileStream file = new FileStream(@path, FileMode.Open, FileAccess.Read))
        {
            excel = new HSSFWorkbook(file);
        }

        Sheet sheet = excel.GetSheet("data_basic_group");
        int c_group = 0;
        for (int row = 1; row <= sheet.LastRowNum; row++)
        {
            c_group++;
            sheet.GetRow(row).GetCell(0).SetCellType(NPOI.SS.UserModel.CellType.STRING);
            sheet.GetRow(row).GetCell(1).SetCellType(NPOI.SS.UserModel.CellType.STRING);
            sheet.GetRow(row).GetCell(2).SetCellType(NPOI.SS.UserModel.CellType.STRING);
            sheet.GetRow(row).GetCell(3).SetCellType(NPOI.SS.UserModel.CellType.STRING);
            sql = "insert into basic_group(name,code,type,status) values ('"+
                sheet.GetRow(row).GetCell(0).StringCellValue+"','"+
                sheet.GetRow(row).GetCell(1).StringCellValue+"','"+
                sheet.GetRow(row).GetCell(2).StringCellValue+"','"+
                sheet.GetRow(row).GetCell(3).StringCellValue+"');";
            comd.CommandText = sql;
            comd.ExecuteNonQuery();            
        }

        sheet = excel.GetSheet("data_basic_permission");
        int c_permission = 0;
        for (int row = 1; row <= sheet.LastRowNum; row++)
        {
            c_permission++;
            sheet.GetRow(row).GetCell(0).SetCellType(NPOI.SS.UserModel.CellType.STRING);
            sheet.GetRow(row).GetCell(1).SetCellType(NPOI.SS.UserModel.CellType.STRING);
            sheet.GetRow(row).GetCell(2).SetCellType(NPOI.SS.UserModel.CellType.STRING);
            sheet.GetRow(row).GetCell(3).SetCellType(NPOI.SS.UserModel.CellType.STRING);
            String path__ = "";
            if (sheet.GetRow(row).GetCell(4) != null)
            {
                sheet.GetRow(row).GetCell(4).SetCellType(NPOI.SS.UserModel.CellType.STRING);
                path__ = sheet.GetRow(row).GetCell(4).StringCellValue;
            }
            sql = "insert into basic_permission (name,type,code,icon,path) values('" +
                sheet.GetRow(row).GetCell(0).StringCellValue + "','" +
                sheet.GetRow(row).GetCell(1).StringCellValue + "','" +
                sheet.GetRow(row).GetCell(2).StringCellValue + "','" +
                sheet.GetRow(row).GetCell(3).StringCellValue + "','" +
                path__ + "');";
            comd.CommandText = sql;
            comd.ExecuteNonQuery();
        }

        sheet = excel.GetSheet("data_basic_group_2_permission");
        int rowCnt = sheet.LastRowNum;
        int colCnt = 0;
        int c_p2g = 0;
        for (int row = 2; row <= rowCnt; row++)
        {
            sheet.GetRow(row).GetCell(1).SetCellType(NPOI.SS.UserModel.CellType.STRING);
            String permission = sheet.GetRow(row).GetCell(1).StringCellValue;
            if (colCnt == 0) colCnt = sheet.GetRow(row).LastCellNum;
            for (int column = 2; column < colCnt; column++)
            {
                sheet.GetRow(1).GetCell(column).SetCellType(NPOI.SS.UserModel.CellType.STRING);
                String group = sheet.GetRow(1).GetCell(column).StringCellValue;
                if (sheet.GetRow(row).GetCell(column) != null)
                {
                    sheet.GetRow(row).GetCell(column).SetCellType(NPOI.SS.UserModel.CellType.STRING);
                    c_p2g++;
                    sql = "insert into basic_group_2_permission (permission_code,group_code) values('" + permission + "','" + group + "');";
                    comd.CommandText = sql;
                    comd.ExecuteNonQuery();
                }
            }
        }

        sql = "DELETE from basic_group_2_permission where basic_group_2_permission.group_code not in('10','99');";
        comd.CommandText = sql;
        comd.ExecuteNonQuery();
        sql = "insert into basic_group_2_permission (permission_code,group_code) SELECT basic_permission.`code` as permission_code ,basic_group.`code` as group_code FROM basic_permission , basic_group WHERE (basic_permission.`code` like '50%' or basic_permission.`code` like '11%' or basic_permission.`code` like '52%' )  AND basic_group.`code` >= '30' and basic_group.`code` <> '99' and basic_permission.`code` not like '%9_'; ";
        comd.CommandText = sql;
        comd.ExecuteNonQuery();
        sql = "insert into basic_group_2_permission (permission_code,group_code) SELECT basic_permission.`code` as permission_code ,'X1' as group_code FROM basic_permission  WHERE (basic_permission.`code` like '50%' or basic_permission.`code` like '11%' or basic_permission.`code` like '52%' )  AND basic_permission.`code` like '%9_'; ";
        comd.CommandText = sql;
        comd.ExecuteNonQuery();

        t_return.Add("c_p2g", c_p2g);
        t_return.Add("c_group", c_group);
        t_return.Add("c_permission", c_permission); 

        if (rd != null) rd.Close();
        conn.Close();
        return t_return;
    }

    public static Hashtable data4test()
    {
        Hashtable t_return = new Hashtable();
        MySqlConnection conn = tools.getConn();
        MySqlCommand comd = new MySqlCommand();
        MySqlDataReader rd = null;
        comd.Connection = conn;
        String sql = "";

        String code__zone_6 = tools.getConfigItem("ZONE");
        String ZONE_NAME = tools.getConfigItem("ZONE_NAME");
        ZONE_NAME = (ZONE_NAME.Split(new Char[1]{'.'}))[2];
        Random rand = new Random();
		String name = "";

        String[] departments__zone_6 = {"行政中心","公安","法院","工商局","城市管理","大医院A","大医院B","社保","国土规划局","房产管理局"};
        String[] departments__zone_6__type = { "3010-9421-10", "3010-9423-10", "3010-9431-10", "3010-9425-10", "3010-8021-10", "3010-8511-10", "3010-8511-10", "3010-9424-10", "3010-9424-11", "3010-9424-12" };

        String sql_delete = "delete from basic_group where code like '" + code__zone_6 + "%' and code <> '" + code__zone_6 + "';";
        comd.CommandText = sql_delete;
        comd.ExecuteNonQuery();

        sql = "START TRANSACTION; ";
        comd.CommandText = sql;
        comd.ExecuteNonQuery();
        int c_total = 0;
        for (int i = 0; i < departments__zone_6.Length; i++)
        {
            String code__z6dp = code__zone_6 + "-" + (10 + i);
            String sql_zone6_dep = "insert into basic_group ("
                + "name,"
                + "code,"
                + "count_users,"
                + "type,"
                + "status,"
                + "remark,"
                + "chief,"
                + "chief_cellphone,"
                + "phone"
                + ")values("
                + "'"+ ZONE_NAME + departments__zone_6[i] + "',"
                + "'" + code__z6dp + "',"
                + "0,"
                + "'" + departments__zone_6__type[i] + "',"
                + "10,"
                + "'说明描述,内容应该很长很长,含有HTML标签,比如回车<br/>还有图片<img src=\"http://img.baidu.com/img/iknow/docshare/icon_s_vip.png\"/>',"
                + "'" + tools.randomName() + "',"
                + "'13456" + (int)(rand.NextDouble() * 1000000 + 1000000) + "',"
                + "'111111111'"
                + ")";
            comd.CommandText = sql_zone6_dep;
            comd.ExecuteNonQuery();
            c_total++;
        }

        //镇或街道,6个到10个
        String chinese_number = "一二三四五六七八九十";
		int count__zone_8 = (int) Math.Floor(rand.NextDouble()*4+6);

        for (int i = 0; i < count__zone_8; i++)
        {
            String code__z8 = code__zone_6 + (10 + i);
            //先插入行政区划
            name = chinese_number.ElementAt((int)(chinese_number.Length * rand.NextDouble()))
                + "" + chinese_number.ElementAt((int)(chinese_number.Length * rand.NextDouble()))
                + "" + chinese_number.ElementAt((int)(chinese_number.Length * rand.NextDouble())) + (rand.NextDouble() > 0.5 ? "镇" : "街道");
            String sql_insert_zone8 = "insert into basic_group(name,code,type) values ('" + name + "','" + code__z8 + "','2010');";
            comd.CommandText = sql_insert_zone8;
            comd.ExecuteNonQuery();
            c_total++;
            String[] departments__zone_8 = { "行政中心", "派出所", "法院", "工商所", "卫生院" };
            String[] departments__zone_8__type = { "3010-9421-10", "3010-9423-10", "3010-9431-10", "3010-9425-10", "3010-8511-10" };

            for (int i1 = 0; i1 < departments__zone_8.Length; i1++)
            {
                String code__z8dp = code__z8 + "-" + (10 + i1);

                String sql_zone8_dep = "insert into basic_group ("
                    + "name,"
                    + "code,"
                    + "count_users,"
                    + "type,"
                    + "status,"
                    + "remark,"
                    + "chief,"
                    + "chief_cellphone,"
                    + "phone"
                    + ")values("
                    + "'" + name + departments__zone_8[i1] + "',"
                    + "'" + code__z8dp + "',"
                    + "0,"
                    + "'" + departments__zone_8__type[i1] + "',"
                    + "10,"
                    + "'说明描述,内容应该很长很长,含有HTML标签,比如回车<br/>还有图片<img src=\"http://img.baidu.com/img/iknow/docshare/icon_s_vip.png\"/>',"
                    + "'" + tools.randomName() + "',"
                    + "'13456" + (int)(rand.NextDouble() * 1000000 + 1000000) + "',"
                    + "'111111111'"
                    + ")";
                comd.CommandText = sql_zone8_dep;
                comd.ExecuteNonQuery();
                c_total++;
            }

            //社区或村庄,7个到12个
			int count__zone_10 = (int) Math.Floor(rand.NextDouble()*5+7);
				
			for(int i2=0;i2<count__zone_10;i2++){
					
				String code__z10 = code__z8 + (10+i2);
				//先插入行政区划
                name = chinese_number.ElementAt((int)(chinese_number.Length * rand.NextDouble()))
                    + "" + chinese_number.ElementAt((int)(chinese_number.Length * rand.NextDouble()))
                    + "" + chinese_number.ElementAt((int)(chinese_number.Length * rand.NextDouble())) + (rand.NextDouble() > 0.5 ? "村" : "小区");
				String sql_insert_zone10 = "insert into basic_group(name,code,type) values ('"+name+"','"+code__z10+"','2010');";
				comd.CommandText = sql_insert_zone10;
                comd.ExecuteNonQuery();
                c_total++;

				String[] departments__zone_10 = {"行政中心"};
				String[] departments__zone_10__type = {"3010-9421-10"};
					
				for(int i3=0;i3<departments__zone_10.Length;i3++){
					String code__z10dp = code__z10+"-"+(10+i3);
						
					String sql_zone8_dep = "insert into basic_group ("
						+"name,"
						+"code,"
						+"count_users,"
						+"type,"
						+"status,"
						+"remark,"
						+"chief,"
						+"chief_cellphone,"
						+"phone"
						+")values("
						+"'"+name+departments__zone_10[i3]+"',"
						+"'"+code__z10dp+"',"
						+"0,"
						+"'"+departments__zone_10__type[i3]+"',"
						+"10,"
						+"'说明描述,内容应该很长很长,含有HTML标签,比如回车<br/>还有图片<img src=\"http://img.baidu.com/img/iknow/docshare/icon_s_vip.png\"/>',"
						+"'"+tools.randomName()+"',"
						+"'13456"+(int)(rand.NextDouble()*1000000+1000000)+"',"
						+"'111111111'"
						+")";
                    comd.CommandText = sql_zone8_dep;
                    comd.ExecuteNonQuery();
                    c_total++;
				}
			}
        }
        sql = "COMMIT;";
        comd.CommandText = sql;
        comd.ExecuteNonQuery();

        String sql_select = "select * from basic_group where type like '30%'";
        MySqlCommand comd2 = new MySqlCommand();
        MySqlConnection conn2 = tools.getConn();
        comd2.Connection = conn2;
        comd2.CommandText = sql_select;
        rd = comd2.ExecuteReader();
        comd.CommandText = "START TRANSACTION; ";
        comd.ExecuteNonQuery();
        while (rd.Read())
        {
            int count = (int)(rand.NextDouble() * 3 + 5);
            for (int i = 0; i < count; i++)
            {
                String sql_division = "insert into basic_group ("
                        + "name,"
                        + "code,"
                        + "count_users,"
                        + "type,"
                        + "status,"
                        + "remark,"
                        + "chief,"
                        + "chief_cellphone,"
                        + "phone"
                        + ")values("
                        + "'" + rd.GetString("name") + "科室" + (int)(rand.NextDouble() * 10000) + "',"
                        + "'" + rd.GetString("code") + "-" + (10 + i) + "',"
                        + "0,"
                        + "'40',"
                        + "10,"
                        + "'说明描述,内容应该很长很长,含有HTML标签,比如回车<br/>还有图片<img src=\"http://img.baidu.com/img/iknow/docshare/icon_s_vip.png\"/>',"
                        + "'" + tools.randomName() + "',"
                        + "'13456" + (int)(rand.NextDouble() * 1000000 + 1000000) + "',"
                        + "'111111111'"
                        + ")";

                comd.CommandText = sql_division;
                comd.ExecuteNonQuery();
                c_total++;
            }
        }
        comd.CommandText = "COMMIT; ";
        comd.ExecuteNonQuery();

        t_return.Add("c_total", c_total);
        if (rd != null) rd.Close();
        conn.Close();
        conn2.Close();
        return t_return;
    }
}