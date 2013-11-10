using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MySql.Data.MySqlClient;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Data.SQLite;

public class tools
{
    public tools()
	{
	}

    public static String webPath = "";

    public static String dbType = null;
    public static MySqlConnection getConn(){
        return getMySqlConn();
    }

    public static MySqlConnection getMySqlConn(){
        String DB_HOST = tools.getConfigItem("DB_HOST");
        String DB_UNM = tools.getConfigItem("DB_UNM");
        String DB_PWD = tools.getConfigItem("DB_PWD");
        String DB_NAME = tools.getConfigItem("DB_NAME");
        MySqlConnection conn = new MySqlConnection("Data Source=" + DB_HOST + ";Database=" + DB_NAME + ";User ID=" + DB_UNM + ";Password=" + DB_PWD + ";Charset=utf8");
        conn.Open();
        MySqlCommand comd = new MySqlCommand("SET NAMES UTF8");
        comd.Connection = conn;
        comd.ExecuteNonQuery();

        comd.CommandText = "set time_zone='+8:00';";
        comd.ExecuteNonQuery();
        return conn;
    }

    public static SQLiteConnection getSqliteConn() {
        SQLiteConnection conn = new SQLiteConnection();
        //conn = new SQLiteConnection();
        System.Data.SQLite.SQLiteConnectionStringBuilder connstr = new System.Data.SQLite.SQLiteConnectionStringBuilder();
        connstr.DataSource = tools.getConfigItem("APPPATH") + "sql\\sqlite.db";
        conn.ConnectionString = connstr.ToString();
        conn.Open();
        return conn;
    }   

    public static Hashtable il8n = null;
    public static Hashtable readIl8n()
    {
        if (tools.il8n == null) 
        {
            tools.il8n = new Hashtable();
            String path = tools.webPath + "\\language\\"+tools.getConfigItem("IL8N")+"\\";
            String[] files = Directory.GetFiles(@path, "*.ini");
            for (int i = 0; i < files.Count();i++ )
            {
                String inipath = files[i];
                TextReader iniFile = new StreamReader(inipath);
                String strLine = iniFile.ReadLine();
                String currentRoot = "";
                Hashtable t_file = new Hashtable();
                while (strLine != null)
                {
                    if (strLine != "")
                    {
                        if (strLine.StartsWith("[") && strLine.EndsWith("]"))
                        {
                            currentRoot = strLine.Substring(1, strLine.Length - 2);
                        }
                        else
                        {
                            String[] key_value = strLine.Split(new char[] { '=' }, 2);
                            if (!t_file.Contains(key_value[0]))
                            {
                                t_file.Add(key_value[0], key_value[1].Replace("\"", ""));
                            }
                        }
                    }
                    strLine = iniFile.ReadLine();
                }
                tools.il8n.Add(currentRoot,t_file);
            }
        }

        return tools.il8n;
    }

    public static Hashtable importIl8n2DB()
    {
        Hashtable t_return = new Hashtable();
        SQLiteConnection conn = tools.getSqliteConn();
        SQLiteCommand comd = new SQLiteCommand();
        comd.Connection = conn;
        SQLiteDataReader rd = null;
        String sql = "";

        String tablenames = "";
        if (tools.il8n == null) tools.readIl8n();
        foreach (System.Collections.DictionaryEntry objDE in tools.il8n)
        {
            String tablename = objDE.Key.ToString();
            tablenames += "," + tablename;
            Hashtable t = (Hashtable)objDE.Value;
            foreach (System.Collections.DictionaryEntry objDE2 in t)
            {
                sql = "insert into basic_memory (type,code,extend4,extend5,extend6) values ('0','"
                            + objDE2.Key.ToString() + "','" + objDE2.Value.ToString() + "','" + tablename + "','il8n');";

                comd.CommandText = sql;
                comd.ExecuteNonQuery();
            }
        }
        comd = null;
        conn.Close();
        conn = null;
        t_return.Add("tablenames", tablenames);
        return t_return;
    }

    public static Hashtable initMemory()
    {
        Hashtable t_return = new Hashtable();
        SQLiteConnection conn = tools.getSqliteConn();
        SQLiteCommand comd = new SQLiteCommand();
        comd.Connection = conn;
        SQLiteDataReader rd = null;

        tools.getConfigItem("reLoad");
        
        if (tools.dbType.Equals("sqlite"))
        {
            tools.readIl8n();
            conn.Close();
            conn = null;
            return t_return;
        }
         
        comd.CommandText = "delete from basic_memory";
        comd.ExecuteNonQuery();
        String s_sql = tools.getSQL("basic_memory__init");
        String[] sql = s_sql.Split(new char[1] { ';' });
        for (int i = 0; i < sql.Count();i++ )
        {
            String sql_ = sql[i];
            comd.CommandText = sql_;
            comd.ExecuteNonQuery();
        }

        if (!tools.dbType.Equals("sqlite"))
        {
            String sql_copy = "insert into basic_memory (code,type,extend4,extend5) (select code,'1' as type,value,reference from basic_parameter where reference like '%\\_%\\_\\_%' )";
            comd.CommandText = sql_copy;
            comd.ExecuteNonQuery();
        }
        else {
            String sql_copy = "select code,'1' as type,value,reference from basic_parameter where reference like '%\\_%\\_\\_%' ESCAPE '\\' ";
            comd.CommandText = sql_copy;
            rd = comd.ExecuteReader();
            String[] sqls = new String[100];
			int count = 0;
			while(rd.Read()){
				sqls[count] = "insert into basic_memory (code,type,extend4,extend5) values ('"+rd.GetString(0)+"','"+rd.GetString(1)+"','"+rd.GetString(2)+"','"+rd.GetString(3)+"')";
				count++;
			}
            rd.Close();
				
			for(int i=0;i<count;i++){
                comd.CommandText = sqls[i];
                comd.ExecuteNonQuery();
			}
        }

        tools.importIl8n2DB();
        conn.Close();
        conn = null;
        return t_return;
    }

    public static int getTableId(String tablename) 
    {
        int id = 0;
        String sql = "select extend1 as id from basic_memory where type = 2 and code = '"+tablename+"' ";
        MySqlConnection conn = tools.getConn();
        MySqlCommand comd = new MySqlCommand(sql);
        comd.Connection = conn;
        MySqlDataReader rd = comd.ExecuteReader();
        rd.Read();
        id = rd.GetInt32("id");
        conn.Close();
        return id;
    }

    public static XmlDocument configXML = null;
    public static String getConfigItem(String id)
    {
        String s_return = "";
        if (tools.configXML == null || id.Equals("reLoad"))
        {
            tools.dbType = null;
            tools.configXML = null;
            String path = tools.webPath + "\\config.xml";
            TextReader fr = new StreamReader(path);
            String strLine = fr.ReadLine();
            String xml = "";
            while (strLine != null)
            {
                xml += strLine;
                strLine = fr.ReadLine();
            }

            configXML = new XmlDocument();
            configXML.LoadXml(xml);
            tools.dbType = tools.getConfigItem("DB_TYPE");
        }
        XmlElement e = configXML.GetElementById(id);
        s_return = e.InnerText;
        return s_return;
    }

    public static XmlDocument sqlXML = null;
    public static String getSQL(String id)
    {
        String s_return = "";
        if (tools.sqlXML == null)
        {
            String sqlxml = "";
            String path = tools.webPath + "\\sql.xml";
            TextReader fr = new StreamReader(path);
            String strLine = fr.ReadLine();
            while (strLine != null)
            {
                sqlxml += strLine;
                strLine = fr.ReadLine();
            }

            sqlXML = new XmlDocument();
            sqlXML.LoadXml(sqlxml);
        }
        XmlElement e = sqlXML.GetElementById(id);
        s_return = e.InnerText;
        return s_return;
    }

    public static ArrayList list2Tree(ArrayList a_list)
    {
        ArrayList a_return = new ArrayList();

        for (int i = 0; i < a_list.Count; i++)
        {
            Hashtable t = (Hashtable)a_list[i];
            int len = ((String)t["code"]).Length;

            int pos_1, pos_2, pos_3, pos_4, pos_5, pos_6 = 0;

            if (len == 2)
            {
                a_return.Add(t);
            }
            else if (len == 4)
            {
                pos_1 = a_return.Count - 1;

                Hashtable t_ = (Hashtable)a_return[pos_1];

                ArrayList a_ = new ArrayList();
                if (t_.ContainsKey("children"))
                {
                    a_ = (ArrayList)t_["children"];
                    a_.Add(t);
                    t_["children"] = a_;
                }
                else
                {
                    a_.Add(t);
                    t_.Add("children", a_);
                }      
               

                a_return[pos_1] = t_;
            }
            else if (len == 6)
            {
                pos_1 = a_return.Count - 1;
                pos_2 = ((ArrayList)( (Hashtable)a_return[pos_1])["children"]).Count - 1;

                Hashtable t_ = (Hashtable)(((ArrayList)((Hashtable)a_return[pos_1])["children"])[pos_2]);

                ArrayList a_ = new ArrayList();
                if (t_.ContainsKey("children"))
                {
                    a_ = (ArrayList)t_["children"];
                    a_.Add(t);
                    t_["children"] = a_;
                }
                else
                {
                    a_.Add(t);
                    t_.Add("children", a_);
                } 

                ((ArrayList)((Hashtable)a_return[pos_1])["children"])[pos_2] = t_;
                        
            }
            else if (len == 8)
            {
                pos_1 = a_return.Count - 1;
                pos_2 = ((ArrayList)((Hashtable)a_return[pos_1])
                        ["children"]).Count - 1;
                pos_3 = ((ArrayList)((Hashtable)((ArrayList)((Hashtable)a_return[pos_1])["children"])[pos_2])
                        ["children"]).Count - 1;

                Hashtable t_ = (Hashtable)((ArrayList)((Hashtable)((ArrayList)((Hashtable)a_return[pos_1])["children"])[pos_2])["children"])[pos_3];

                ArrayList a_ = new ArrayList();
                if (t_.ContainsKey("children"))
                {
                    a_ = (ArrayList)t_["children"];
                    a_.Add(t);
                    t_["children"] = a_;
                }
                else
                {
                    a_.Add(t);
                    t_.Add("children", a_);
                } 

                ((ArrayList)((Hashtable)((ArrayList)((Hashtable)a_return[pos_1])["children"])[pos_2])
                        ["children"])[pos_3]= t_;
            }
            else if (len == 10)
            {
                pos_1 = a_return.Count - 1;
                pos_2 = ((ArrayList)((Hashtable)a_return[pos_1])["children"]).Count - 1;
                pos_3 = ((ArrayList)((Hashtable)((ArrayList)((Hashtable)a_return[pos_1])["children"])[pos_2])["children"]).Count - 1;
                pos_4 = ((ArrayList)((Hashtable)((ArrayList)((Hashtable)((ArrayList)((Hashtable)a_return[pos_1])["children"])[pos_2])
                        ["children"])[pos_3])["children"]).Count - 1;

                Hashtable t_ = (Hashtable)((ArrayList)((Hashtable)((ArrayList)((Hashtable)((ArrayList)((Hashtable)a_return
                        [pos_1])["children"])[pos_2])
                        ["children"])[pos_3])["children"])
                        [pos_4];

                ArrayList a_ = new ArrayList();
                if (t_.ContainsKey("children"))
                {
                    a_ = (ArrayList)t_["children"];
                    a_.Add(t);
                    t_["children"] = a_;
                }
                else
                {
                    a_.Add(t);
                    t_.Add("children", a_);
                } 

                ((ArrayList)((Hashtable)((ArrayList)((Hashtable)((ArrayList)((Hashtable)a_return
                        [pos_1])["children"])[pos_2])
                        ["children"])[pos_3])["children"])[pos_4]=t_;
            }
            else if (len == 12)
            {
                pos_1 = a_return.Count - 1;
                pos_2 = ((ArrayList)((Hashtable)a_return[pos_1])
                        ["children"]).Count - 1;
                pos_3 = ((ArrayList)((Hashtable)((ArrayList)((Hashtable)a_return
                        [pos_1])["children"])[pos_2])
                        ["children"]).Count - 1;
                pos_4 = ((ArrayList)((Hashtable)((ArrayList)((Hashtable)((ArrayList)((Hashtable)a_return
                        [pos_1])["children"])[pos_2])
                        ["children"])[pos_3])["children"]).Count - 1;
                pos_5 = ((ArrayList)((Hashtable)((ArrayList)((Hashtable)((ArrayList)((Hashtable)((ArrayList)((Hashtable)a_return
                        [pos_1])["children"])[pos_2])
                        ["children"])[pos_3])["children"])
                        [pos_4])["children"]).Count - 1;

                Hashtable t_ = (Hashtable)((ArrayList)((Hashtable)((ArrayList)((Hashtable)((ArrayList)((Hashtable)((ArrayList)((Hashtable)a_return
                        [pos_1])["children"])[pos_2])
                        ["children"])[pos_3])["children"])
                        [pos_4])["children"])[pos_5];

                ArrayList a_ = new ArrayList();
                if (t_.ContainsKey("children"))
                {
                    a_ = (ArrayList)t_["children"];
                    t_["children"] = a_;
                }
                else
                {
                    t_.Add("children", a_);
                }
                a_.Add(t);

                ((ArrayList)((Hashtable)((ArrayList)((Hashtable)((ArrayList)((Hashtable)((ArrayList)((Hashtable)a_return
                        [pos_1])["children"])[pos_2])
                        ["children"])[pos_3])["children"])
                        [pos_4])["children"])[pos_5]= t_;
            }
            else if (len == 14)
            {
                pos_1 = a_return.Count - 1;
                pos_2 = ((ArrayList)((Hashtable)a_return[pos_1])
                        ["children"]).Count - 1;
                pos_3 = ((ArrayList)((Hashtable)((ArrayList)((Hashtable)a_return
                        [pos_1])["children"])[pos_2])
                        ["children"]).Count - 1;
                pos_4 = ((ArrayList)((Hashtable)((ArrayList)((Hashtable)((ArrayList)((Hashtable)a_return
                        [pos_1])["children"])[pos_2])
                        ["children"])[pos_3])["children"]).Count - 1;
                pos_5 = ((ArrayList)((Hashtable)((ArrayList)((Hashtable)((ArrayList)((Hashtable)((ArrayList)((Hashtable)a_return
                        [pos_1])["children"])[pos_2])
                        ["children"])[pos_3])["children"])
                        [pos_4])["children"]).Count - 1;
                pos_6 = ((ArrayList)((Hashtable)((ArrayList)((Hashtable)((ArrayList)((Hashtable)((ArrayList)((Hashtable)((ArrayList)((Hashtable)a_return
                        [pos_1])["children"])[pos_2])
                        ["children"])[pos_3])["children"])
                        [pos_4])["children"])[pos_5])
                        ["children"]).Count - 1;

                Hashtable t_ = (Hashtable)((ArrayList)((Hashtable)((ArrayList)((Hashtable)((ArrayList)((Hashtable)((ArrayList)((Hashtable)((ArrayList)((Hashtable)a_return
                        [pos_1])["children"])[pos_2])
                        ["children"])[pos_3])["children"])
                        [pos_4])["children"])[pos_5])
                        ["children"])[pos_6];

                ArrayList a_ = new ArrayList();
                if (t_.ContainsKey("children"))
                {
                    a_ = (ArrayList)t_["children"];
                    t_["children"] = a_;
                }
                else
                {
                    t_.Add("children", a_);
                }
                a_.Add(t);

                ((ArrayList)((Hashtable)((ArrayList)((Hashtable)((ArrayList)((Hashtable)((ArrayList)((Hashtable)((ArrayList)((Hashtable)a_return
                        [pos_1])["children"])[pos_2])
                        ["children"])[pos_3])["children"])
                        [pos_4])["children"])[pos_5])
                        ["children"])[pos_6]=t_;
            }
        }

        return a_return;
    }

    public static String MD5_(String input)
    {
        // Create a new instance of the MD5CryptoServiceProvider object.
        MD5 md5Hasher = MD5.Create();

        // Convert the input string to a byte array and compute the hash.
        byte[] data = md5Hasher.ComputeHash(Encoding.Default.GetBytes(input));

        // Create a new Stringbuilder to collect the bytes
        // and create a string.
        StringBuilder sBuilder = new StringBuilder();

        // Loop through each byte of the hashed data 
        // and format each one as a hexadecimal string.
        for (int i = 0; i < data.Length; i++)
        {
            sBuilder.Append(data[i].ToString("x2"));
        }

        // Return the hexadecimal string.
        return sBuilder.ToString();
    }

    public static String randomName()
    {
        Random rand = new Random();
        String name = "";
        String name_1 = "赵钱孙李周吴郑王冯陈楮卫蒋沈韩杨朱秦尤许何吕施张孔曹严华金魏陶姜戚谢邹喻柏水窦章云苏潘葛奚范彭郎鲁韦昌马苗凤花方俞任袁柳酆鲍史唐费廉岑薛雷贺倪汤";
        String name_2 = "安邦安福安歌安国安和安康安澜安民安宁安平安然安顺"
            + "宾白宾鸿宾实彬彬彬炳彬郁斌斌斌蔚滨海波光波鸿波峻"
            + "才捷才良才艺才英才哲才俊成和成弘成化成济成礼成龙"
            + "德本德海德厚德华德辉德惠德容德润德寿德水德馨德曜"
            + "飞昂飞白飞飙飞掣飞尘飞沉飞驰飞光飞翰飞航飞翮飞鸿"
            + "刚豪刚洁刚捷刚毅高昂高岑高畅高超高驰高达高澹高飞"
            + "晗昱晗日涵畅涵涤涵亮涵忍涵容涵润涵涵涵煦涵蓄涵衍"
            + "嘉赐嘉德嘉福嘉良嘉茂嘉木嘉慕嘉纳嘉年嘉平嘉庆嘉荣"
            + "开畅开诚开宇开济开霁开朗凯安凯唱凯定凯风凯复凯歌"
            + "乐安乐邦乐成乐池乐和乐家乐康乐人乐容乐山乐生乐圣"
            + "茂才茂材茂德茂典茂实茂学茂勋茂彦敏博敏才敏达敏叡"
            + "朋兴朋义彭勃彭薄彭湃彭彭彭魄彭越彭泽彭祖鹏程鹏池";

        int name_1_ = (int)(name_1.Length * rand.NextDouble());
        int name_2_ = (int)((name_2.Length - 2) * rand.NextDouble());
        name = name_1.Substring(name_1_, 1) + name_2.Substring(name_2_,  2);

        return name;
    }

    static void Main(string[] args)
    {
        //To Do
    }
}