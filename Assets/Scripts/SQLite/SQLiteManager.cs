using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;
using System.Data;
using Mono.Data.Sqlite;
using System.Collections;

public class SQLiteManager : MonoBehaviour {
    //Класс предоставляющий инструментарий для работы с SQLite

    public SqliteConnection db_con = null;
    public SqliteCommand db_cmd = null;
    public SqliteDataReader db_reader = null;

    public IDbConnection dbconn = null;
    public string dbName = "main.bytes";                    //текущая БД к которой тестируется подключение
    public string table = "";                               //ссылка не текущую таблицу

    public Text log = null;                                 //поле для вывода содержимого из БД

    //Поля для ввода сохраняемых значений:
    public Text textPole = null;
    public Text intPole = null;
    public Text floatPole = null;

    public string filepath = "";                                 //путь до базы данных

    //подключение к базе
    public bool connection()
    {
        bool result = false;
        try
        {
            // если игра запускается не Android
            if (Application.platform != RuntimePlatform.Android)
            {
                //используем такой путь:
                filepath = Application.dataPath + "/StreamingAssets/" + dbName;
            }else{

                filepath = Application.persistentDataPath + "/" + dbName;
                // если базы данных по заданному пути нет, размещаем ее там
                if (!File.Exists(filepath))
                {
                    WWW loadDB = new WWW("jar:file://" + Application.dataPath + "!/assets/" + dbName);
                    while (!loadDB.isDone) { }
                    File.WriteAllBytes(filepath, loadDB.bytes);
                }
            }

            db_con = new SqliteConnection("URI=file:" + filepath);
            db_con.Open();

            //Сообщаем что подключились:
            if (db_con.State == ConnectionState.Open)
            {
                log.text = "Соединение с ДБ установлено!";
                result = true;
            }
        }
        catch (Exception ex){
            log.text = ex.ToString();
        }
        return result;
    }

    //Метод запрашивает все содержимое конкретной таблицы
    public void selectAllContentTable(string tableName) {
        if (connection())
        {
            string sqlQuery = "SELECT iden, testText, testInt, testFloat FROM " + tableName;
            db_cmd = new SqliteCommand(sqlQuery, db_con);
            db_reader = db_cmd.ExecuteReader();

            while (db_reader.Read())
            {
                int primary = db_reader.GetInt32(0);
                string name = db_reader.GetString(1);
                int rand = db_reader.GetInt32(2);
                float value = db_reader.GetFloat(3);
                //выводим пользователю:
                log.text = "Text = " + name + ", Int = " + rand + ", float = " + value;
            }
            //Не забываем закрыть соединение с базой
            db_reader.Close();
            db_reader = null;
            db_cmd.Dispose();
            db_cmd = null;
            db_con.Close();
            db_con = null;
        }
    }

    //Метод обновляет данные в строке:
    public void onSave(string tableName) {
        if (connection())
        {
            string sqlQuery = "UPDATE test SET testText = '" + textPole.text + "', testInt = " + intPole.text + ", testFloat = " + floatPole.text + " WHERE iden = " + 0 + ";";
            db_cmd = new SqliteCommand(sqlQuery, db_con);
            db_cmd.CommandText = sqlQuery;
            db_cmd.Connection.CreateCommand();
            db_reader = db_cmd.ExecuteReader();
            db_reader.Close();
            db_reader = null;
            //Не забываем закрыть соединение с базой
            db_cmd.Dispose();
            db_cmd = null;
            db_con.Close();
            db_con = null;
        }
    }

    /*
     * Пример:
     * string conn = "URI=file:" + Application.dataPath + "/PickAndPlaceDatabase.s3db"; //Path to database. 
     * IDbConnection dbconn; 
     * dbconn = (IDbConnection) new SqliteConnection(conn); 
     * dbconn.Open(); //Open connection to the database. 
     * IDbCommand dbcmd = dbconn.CreateCommand(); 
     * string sqlQuery = "SELECT value,name, randomSequence " + "FROM PlaceSequence"; 
     * dbcmd.CommandText = sqlQuery; IDataReader reader = dbcmd.ExecuteReader(); 
     * while (reader.Read()) { int value = reader.GetInt32(0); 
     * string name = reader.GetString(1); 
     * int rand = reader.GetInt32(2); 
     * Debug.Log( "value= "+value+" name ="+name+" random ="+ rand); 
     * } reader.Close(); 
     * reader = null; 
     * dbcmd.Dispose(); 
     * dbcmd = null; 
     * dbconn.Close(); 
     * dbconn = null; 
     * }
     * */
}
