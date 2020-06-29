﻿using PreisVergleich.Helpers;
using PreisVergleich.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Reflection;
using static PreisVergleich.Helpers.Logger;

namespace PreisVergleich.Helper
{
    public class SQLiteHelper
    {
        public SQLiteConnection connection;

        public Logger log;

        public SQLiteHelper(string ConnectionString)
        {
            log = new Logger();

            if (!string.IsNullOrEmpty(ConnectionString))
            {
                //Datenbank erstellen
                if (!File.Exists(ConnectionString))
                {
                    //Datenbank & Tabelle erstellen
                    SQLiteConnection.CreateFile(ConnectionString);

                    connection = new SQLiteConnection($"Data Source={ConnectionString};Version=3;");
                    connection.Open();

                    string sql = "create table PRODUKTE (produktID INTEGER PRIMARY KEY NOT " +
                        " NULL, hardwareRatURL varchar(200), compareSiteURL varchar(200), hardwareRatPrice REAL(10), compareSitePrice REAL(10), state varchar(50), differencePrice REAL(10), compareSiteType varchar(100), articleName varchar(300), articleURL varchar(300))";

                    SQLiteCommand command = new SQLiteCommand(sql, connection);
                    command.ExecuteNonQuery();

                }
                else
                {
                    connection = new SQLiteConnection($"Data Source={ConnectionString};Version=3;");
                    connection.Open();
                }
            }

        }

        public List<ProduktModell> getGridData(string sql)
        {
            List<ProduktModell> retVal = new List<ProduktModell>();
            if (string.IsNullOrEmpty(sql))
            {
                return null;
            }
            if (connection.State == ConnectionState.Open)
            {
                try
                {
                    using (SQLiteCommand cmd = new SQLiteCommand(sql, connection))
                    {
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            if (!reader.HasRows)
                            {
                                return null;
                            }
                            while (reader.Read())
                            {
                                ProduktModell dataRow = new ProduktModell()
                                {
                                    hardwareRatURL = reader[0].ToString(),
                                    compareURL = reader[1].ToString(),
                                    hardwareRatPrice = double.Parse(reader[2].ToString()),
                                    comparePrice = double.Parse(reader[3].ToString()),
                                    State = reader[4].ToString(),
                                    priceDifference = double.Parse(reader[5].ToString()),
                                    compareSiteType = reader[6].ToString(),
                                    produktID = int.Parse(reader[7].ToString()),
                                    articleName = reader[8].ToString(),
                                    articlePicture = reader[9].ToString(),
                                };
                                retVal.Add(dataRow);
                            }
                        }
                    }
                    return retVal;
                }
                catch (Exception ex)
                {
                    log.writeLog(LogType.ERROR, MethodBase.GetCurrentMethod().Name + ": " + "Fehler beim Ausführen des Read-SQls", ex);
                    log.writeLog(LogType.ERROR, MethodBase.GetCurrentMethod().Name + ": " + sql);
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        public void InsertItem(string urlHWRat, string urlCompareSite)
        {
            string sql = $"INSERT INTO PRODUKTE (hardwareRatURL, compareSiteURL, hardwareRatPrice, compareSitePrice, state, differencePrice, compareSiteType) VALUES ('{urlHWRat}', '{urlCompareSite}', '0', '0', 'keiner', '0', 'Geizhals')";
            if (connection.State == ConnectionState.Open)
            {
                try
                {
                    using (SQLiteCommand cmd = new SQLiteCommand(sql, connection))
                    {
                        cmd.ExecuteNonQuery();                    
                    }
                }
                catch (Exception ex)
                {
                    log.writeLog(LogType.ERROR, MethodBase.GetCurrentMethod().Name + ": " + "Fehler beim Ausführen des Insert-SQls", ex);
                    log.writeLog(LogType.ERROR, MethodBase.GetCurrentMethod().Name + ": " + sql);
                    return;
                }
            }
            else
            {
                return;
            }
        }

        public void UpdateItem(ProduktModell item)
        {
            string sql = $"UPDATE PRODUKTE set articleURL = '{item.articlePicture}', articleName = '{item.articleName}', hardwareRatURL = '{item.hardwareRatURL}', compareSiteURL = '{item.compareURL}', hardwareRatPrice = '{item.hardwareRatPrice}', compareSitePrice = '{item.comparePrice}', state = '{item.State}', differencePrice = '{item.priceDifference}' where produktID = '{item.produktID}'";
            if (connection.State == ConnectionState.Open)
            {
                try
                {
                    using (SQLiteCommand cmd = new SQLiteCommand(sql, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    log.writeLog(LogType.ERROR, MethodBase.GetCurrentMethod().Name + ": " + "Fehler beim Ausführen des Update-SQls", ex);
                    log.writeLog(LogType.ERROR, MethodBase.GetCurrentMethod().Name + ": " + sql);
                    return;
                }
            }
            else
            {
                return;
            }
        }


        public void DeleteItem(ProduktModell item)
        {
            string sql = $"DELETE FROM PRODUKTE where produktID = '{item.produktID}'";
            if (connection.State == ConnectionState.Open)
            {
                try
                {
                    using (SQLiteCommand cmd = new SQLiteCommand(sql, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    log.writeLog(LogType.ERROR, MethodBase.GetCurrentMethod().Name + ": " + "Fehler beim Ausführen des Delete-SQls", ex);
                    log.writeLog(LogType.ERROR, MethodBase.GetCurrentMethod().Name + ": " + sql);
                    return;
                }
            }
            else
            {
                return;
            }
        }
    }
}