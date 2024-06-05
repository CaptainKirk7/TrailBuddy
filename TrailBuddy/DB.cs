using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using SQLite;
using System.Reflection;

namespace TrailBuddy;
public class DB
{
    private static string DBName = "trailbuddy.db";
    public static SQLiteConnection conn;

    public static void OpenConnection()
    {
        string libFolder = FileSystem.AppDataDirectory;
        string fname = System.IO.Path.Combine(libFolder, DBName);
        conn = new SQLiteConnection(fname);

        conn.CreateTable<Favorites>();
        conn.CreateTable<LocalTrails>();
        conn.CreateTable<CoordData>();
    }
}
