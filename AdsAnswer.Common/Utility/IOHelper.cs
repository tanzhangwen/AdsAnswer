//---------------------------------------------------------------------
// <copyright company="Microsoft Corporation" file="IOHelpder.cs">
//      Created by: tomtan at 7/14/2015 3:25:51 PM
//      Copyright (c) Microsoft Corporation.  All rights reserved.
//      Information Contained Herein is Proprietary and Confidential.
// </copyright>
//---------------------------------------------------------------------

namespace AdsAnswer.Utility
{
    using System;
    using System.IO;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Data;
    using System.Data.SqlClient;

    public class IOHelper
    {
        #region text file

        public static IEnumerable<string> ExtractLine(string file)
        {
            using (StreamReader reader = new StreamReader(file))
            {
                string line = null;
                while ((line = reader.ReadLine()) != null)
                    yield return line;
            }
        }

        public static IEnumerable<string[]> ExtractTsv(string file)
        {
            using (StreamReader reader = new StreamReader(file))
            {
                string line = null;
                while ((line = reader.ReadLine()) != null)
                    yield return line.Split('\t');
            }
        }

        public static IEnumerable<string[]> ParseTsv(string content, char[] splitter)
        {
            if (string.IsNullOrEmpty(content))
                yield break;
            using (StringReader reader = new StringReader(content))
            {
                string line = null;
                while ((line = reader.ReadLine()) != null)
                    yield return line.Split(splitter);
            }
        }

        #endregion

        #region file & directory

        public static void DeleteFolder(string folderPath)
        {
            if (Directory.Exists(folderPath))
                Directory.Delete(folderPath, true);
        }

        #endregion

        #region DB

        public static List<T> ExtractRecords<T>(SqlConnection connection, string sqlstmt, Func<IDataRecord, T> func)
        {
            int n = 0;
            while (true)
            {
                n++;
                try
                {
                    List<T> list = ExtractRecords(connection, sqlstmt)
                        .Select(func)
                        .ToList();
                    return list;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    Console.WriteLine("Retry");
                    if (n >= 3)
                        throw;
                    System.Threading.Thread.Sleep(1000 * 30);
                }
            }
        }

        public static IEnumerable<IDataRecord> ExtractRecords(SqlConnection connection, string sqlstmt)
        {
            using (SqlCommand cmd = connection.CreateCommand())
            {
                cmd.CommandText = sqlstmt;
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                        yield return dr;
                }
            }
        }

        public static List<T> ExtractRecords<T>(string conString, string sqlstmt, Func<IDataRecord, T> func)
        {
            int n = 0;
            while (true)
            {
                n++;
                try
                {
                    List<T> list = ExtractRecords(conString, sqlstmt)
                        .Select(func)
                        .ToList();
                    return list;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    Console.WriteLine("Retry");
                    if (n >= 3)
                        throw;
                    System.Threading.Thread.Sleep(1000 * 30);
                }
            }
        }

        public static IEnumerable<IDataRecord> ExtractRecords(string conString, string sqlstmt)
        {
            using (SqlConnection connection = new SqlConnection(conString))
            {
                connection.Open();
                foreach (var t in ExtractRecords(connection, sqlstmt))
                    yield return t;
                connection.Close();
            }
        }

        public static int ExecuteSqlStmt(string conString, string sqlstmt)
        {
            int ret = 0;
            using (SqlConnection connection = new SqlConnection(conString))
            {
                connection.Open();
                using (var sqlcmd = connection.CreateCommand())
                {
                    sqlcmd.CommandText = sqlstmt;
                    ret = sqlcmd.ExecuteNonQuery();
                }
                connection.Close();
            }
            return ret;
        }

        #endregion

        public static T ParseEnum<T>(string value)
        {
            return (T)Enum.Parse(typeof(T), value, true);
        }

        public static string UrlEncode(string str)
        {
            StringBuilder sb = new StringBuilder();
            byte[] byStr = System.Text.Encoding.UTF8.GetBytes(str);
            for (int i = 0; i < byStr.Length; i++)
            {
                sb.Append(@"%" + Convert.ToString(byStr[i], 16));
            }

            return (sb.ToString());
        }

        public static IEnumerable<List<TSource>> Pages<TSource>(IEnumerable<TSource> source, int pageSize)
        {
            List<TSource> list = new List<TSource>();
            foreach (var t in source)
            {
                list.Add(t);
                if (list.Count >= pageSize)
                {
                    yield return list;
                    list = new List<TSource>();
                }
            }
            if (list.Count > 0)
                yield return list;
        }
    }
}
