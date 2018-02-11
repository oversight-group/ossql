#define SHOW_QUERIES
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OsSql;
using static OsSql.OsSqlTypes;
using MySql.Data.MySqlClient;
namespace OsSqlTest
{
    class Program
    {
        public static SQL sql;
        public static Structure Structure_Main;
        public static Table Table_Users, Table_Groups, Table_GroupMembers, Table_Levels, Table_Messages, Table_Settings, Table_TestAtt;
        static void Main(string[] args)
        {
            // Structure Build
            Structure_Main = new Structure();
            Build(ref Structure_Main);
            // Commands
            Console.WriteLine("Welcome.");
            string r;
            while ((r = Console.ReadLine()) != "exit")
                Command(r);
        }
        private static void OsSqlDebugger_OnOsSqlDebugMessage(OsSqlDebugger.OsSqlDebugType type, string msg)
        {
            Console.WriteLine("SQL Debugger (" + type + "): " + msg);
        }
        private static void OsSqlDebugger_OnOsSqlQuery(OsSqlDebugger.OsSqlDebugType type, string message)
        {
            Console.WriteLine("[query] " + message);
        }
        private static void Command(string r)
        {
            string[] cmd = r.Contains(";") ? r.Split(';') : new string[] { r };
            try
            {
                switch (cmd[0])
                {
                    case "initialize":
                        sql = new SQL("127.0.0.1", "ossqltest", "root", "1234");
                        OsSqlDebugger.OnOsSqlDebugMessage += OsSqlDebugger_OnOsSqlDebugMessage;
#if SHOW_QUERIES
                        OsSqlDebugger.OnOsSqlQuery += OsSqlDebugger_OnOsSqlQuery;
#endif
                        Console.WriteLine("Initialized.");
                        break;
                    case "build":
                        Console.WriteLine(Builder.GetCreationQuery(Structure_Main));
                        System.IO.File.WriteAllText("C:/Users/Amit/Desktop/texts.txt", Builder.GetCreationQuery(Structure_Main));
                        break;
                    case "connect":
                        Console.WriteLine("Connecting...");
                        sql.Connect();
                        Console.WriteLine("Connection status - " + sql.Connection.State);
                        break;
                    case "disconnect":
                        sql.Disconnect();
                        Console.WriteLine("Disconnected.");
                        break;
                    case "create":
                        sql.Query(Builder.GetCreationQuery(Structure_Main));
                        break;
                    case "status":
                        Console.WriteLine("Connection status - " + sql.Connection.State);
                        break;
                    case "details":
                        Console.WriteLine("Connection details - " + sql.ConnectionDetails.ToString());
                        break;
                    case "select":
                        {
                            var results = cmd.Length == 5 ? sql.Select(cmd[1], cmd.Length == 2 ? "" : cmd[2], conditionparams: new Parameter(cmd[3], cmd[4])) : sql.Select(cmd[1], cmd.Length == 2 ? "" : cmd[2]);
                            Console.WriteLine(results.Count + " results:");
                            for (int i = 0; i < results.Count; i++)
                            {
                                Console.WriteLine("Result #" + i + ":");
                                foreach (var j in results[i])
                                    Console.WriteLine(j.Key + "=" + j.Value);
                            }
                            break;
                        }
                    case "read":
                        {
                            var results = cmd.Length == 5 ? sql.Read(cmd[1], cmd.Length == 2 ? "" : cmd[2], conditionparams: new Parameter(cmd[3], cmd[4])) : sql.Read(cmd[1], cmd.Length == 2 ? "" : cmd[2]);
                            Console.WriteLine(results.Rows.Count + " rows:");
                            for (int i = 0; i < results.Rows.Count; i++)
                            {
                                Console.WriteLine("Row #" + i);
                                for (int j = 0; j < results.Columns.Count; j++)
                                    Console.WriteLine(results.Columns[j].ColumnName + "=" + results.Rows[i][j]);
                            }
                            break;
                        }
                    case "update":
                        Parameter[] toUpdate = new Parameter[(cmd.Length - 3) / 2];
                        for (int i = 3; i < cmd.Length; i += 2)
                            toUpdate[(i - 3) / 2] = new Parameter(cmd[i], cmd[i + 1]);
                        sql.Update(cmd[1], cmd[2], toUpdate);
                        Console.WriteLine("Done.");
                        break;
                    case "insert":
                        Parameter[] toInsert = new Parameter[(cmd.Length - 2) / 2];
                        for (int i = 2; i < cmd.Length; i += 2)
                            toInsert[(i - 2) / 2] = new Parameter(cmd[i], cmd[i + 1]);
                        int id = sql.Insert(cmd[1], toInsert);
                        Console.WriteLine("Done: " + id);
                        break;
                    case "count":
                        int c = cmd.Length == 5 ? sql.Count(cmd[1], cmd.Length == 2 ? "" : cmd[2], new Parameter(cmd[3], cmd[4])) : sql.Count(cmd[1], cmd.Length == 2 ? "" : cmd[2]);
                        Console.WriteLine("Count = " + c);
                        break;
                    case "updatestructure":
                        sql.UpdateStructure(Structure_Main, updateTables: true);
                        Console.WriteLine("Done.");
                        break;
                    case "setuserdate":
                        sql.Update(Table_Users.Name, "user_id = " + 1, new Parameter("Registered", new DateTime(2012, 12, 21, 12, 12, 12)));
                        Console.WriteLine("Set.");
                        break;
                    case "autoinsert":
                        var registerNewUser = new User()
                        {
                            UserName = "NewUser_AutoInsert",
                            email = "test@test.test",
                            Level = new Level() { ID = 3 },
                            PersonalDetails = new Person()
                            {
                                FirstName = "Amit",
                                LastName = "Barami",
                                Age = 23,
                                Grades = new List<int>() { 100, 100, 150, 666, 1337 }
                            }
                        };
                        int idx = sql.AutoInsert(registerNewUser, Table_Users, skipnull: true);
                        Console.WriteLine("Added: " + idx);
                        break;
                    case "autoupdate":
                        var updateUser = new User() { ID = 1, Level = new Level() { ID = 987 } };
                        sql.AutoUpdate(updateUser, Table_Users, "user_id = 1");
                        break;
                    case "autoupdateskip":
                        var updateUser2 = new User() { ID = 1, Level = new Level() { ID = 555 } };
                        sql.AutoUpdate(updateUser2, Table_Users, "user_id = 1", skipnull: true);
                        break;
                    case "autoselect":
                        {
                            var results = sql.AutoSelect(Table_Users, "user_id = " + cmd[1]);
                            Console.WriteLine(results.Count + " results:");
                            for (int i = 0; i < results.Count; i++)
                            {
                                Console.WriteLine("Result #" + i + ":");
                                foreach (var j in results[i])
                                    Console.WriteLine(j.Key + "=" + j.Value);
                            }
                            break;
                        }
                    case "autoload":
                        Console.WriteLine("Auto loading...");
                        User loadedUser = sql.AutoLoad<User>(Table_Users, "user_id = " + cmd[1], out var data);
                        Console.WriteLine("ID: " + loadedUser.ID);
                        Console.WriteLine("UserName: " + loadedUser.UserName);
                        Console.WriteLine("XP: " + loadedUser.XP);
                        Console.WriteLine("Registered: " + loadedUser.Registered);
                        Console.WriteLine("Level: " + (loadedUser.Level == null ? "null" : loadedUser.Level.ID.ToString())); // should be null, as Level is a class type
                        Console.WriteLine("Personal details: " + loadedUser.PersonalDetails.FirstName + " " + loadedUser.PersonalDetails.LastName + " (" + loadedUser.PersonalDetails.Grades.Count + ")"); // should be null, as Level is a class type
                        break;
                    case "setusernull":
                        sql.Update(Table_Users.Name, "user_id = " + 1, new Parameter("Registered", null));
                        Console.WriteLine("Set.");
                        break;
                    default:
                        Console.WriteLine("Unknown command.");
                        break;
                }
            }
#pragma warning disable CS0168 // Variable is declared but never used
            catch (OsSqlException ose)
#pragma warning restore CS0168 // Variable is declared but never used
            {
                //Console.WriteLine("OsSqlException: " + ose.Message); will be handled by OsSqlDebugger
            }
        }
        public class User : IOsSqlElement
        {
            public int ID { get; set; }
            public string UserName { get; set; }
            public string email { get; set; }
            public float XP { get; set; }
            public Level Level { get; set; }
            public List<Group> Groups { get; set; }
            public enum UserStatus { Active, Offline }
            public UserStatus Status { get; set; }
            public DateTime Registered { get; set; }
            public Person PersonalDetails { get; set; }
            public int GetID()
            {
                return ID;
            }
        }
        public struct Person
        {
            public string FirstName;
            public string LastName;
            public int Age;
            public List<int> Grades;
            private int AnotherUnusedProperty;
        }
        public class Group : IOsSqlElement
        {
            public int ID { get; set; }
            public string Name { get; set; }
            public int GetID()
            {
                return ID;
            }
        }
        public class GroupMembers : IOsSqlElement
        {
            public int ID { get; set; }
            public User User { get; set; }
            public Group Group { get; set; }
            public int GetID()
            {
                return ID;
            }
        }
        public class Level : IOsSqlElement
        {
            public int ID { get; set; }
            public string Header { get; set; }
            public int GetID()
            {
                return ID;
            }
        }
        public class Message : IOsSqlElement
        {
            public int ID { get; set; }
            public string Text { get; set; }
            public int GetID()
            {
                return ID;
            }
        }
        public class Setting : IOsSqlElement
        {
            public int ID { get; set; }
            public string Key { get; set; }
            public string Value { get; set; }
            public int GetID()
            {
                return ID;
            }
        }
        public class Att : IOsSqlElement
        {
            [OsSqlSave(ColumnType.Int, "id", true)]
            public int ID { get; set; }
            [OsSqlSave()]
            public string InSQL { get; set; }
            public string OutSQL { get; set; }
            public int GetID()
            {
                return ID;
            }
        }
        private static void Build(ref Structure s)
        {
            // Users - Manual build
            Table_Users = Structure_Main.AddTable("users");
            Table_Users.AddColumn(ColumnType.Int, "user_id", "ID", true);
            Table_Users.AddColumn(ColumnType.Text, "user_name", "UserName");
            Table_Users.AddColumn(ColumnType.Text, "email"); // same name for both code & database
            Table_Users.AddColumn(ColumnType.Float, "xp", "XP");
            Table_Users.AddColumn(ColumnType.Element, "level", "Level");
            Table_Users.AddColumn(ColumnType.Enum, "status", "Status");
            Table_Users.AddColumn(ColumnType.DateTime, "Registered"); // same name for both code & database
            Table_Users.AddColumn(ColumnType.Object, "PersonalDetails");
            // Groups - Manual build
            Table_Groups = Structure_Main.AddTable("groups");
            Table_Groups.AddColumn(ColumnType.Int, "id", "ID", true);
            Table_Groups.AddColumn(ColumnType.Text, "group_name", "Name");
            // GroupMembers - Manual build
            Table_GroupMembers = Structure_Main.AddTable("groupmembers");
            Table_GroupMembers.AddColumn(ColumnType.Int, "id", "ID", true);
            Table_GroupMembers.AddColumn(ColumnType.Int, "user_id", "User");
            Table_GroupMembers.AddColumn(ColumnType.Int, "group_id", "Group");
            // Levels - Build exactly as a class then setting the ID as AI
            Table_Levels = Structure_Main.AddTable("levels");
            Table_Levels.AddColumnsByClassProperties(typeof(Level));
            var id = Table_Levels.FindC("ID");
            if (id != null)
                id.AutoInc = true;
            // Messages - Build exactly as a class then setting the ID as AI
            Table_Messages = Structure_Main.AddTable("messages");
            Table_Messages.AddColumnsByClassProperties(typeof(Message));
            id = Table_Messages.FindC("ID");
            if (id != null)
                id.AutoInc = true;
            // Settings - Build exactly as a class then setting the ID as AI + adding default values to it
            Table_Settings = Structure_Main.AddTable("settings");
            Table_Settings.AddColumnsByClassProperties(typeof(Setting));
            id = Table_Settings.FindC("ID");
            if (id != null)
                id.AutoInc = true;
            // TestAtt - Build by attributes
            Table_TestAtt = Structure_Main.AddTable("testatt");
            Table_TestAtt.AddColumnsByAttributes(typeof(Att));
        }
    }
}