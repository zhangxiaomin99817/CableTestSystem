/************************************************************************************
 *      Copyright (C) 2019 FigKey,All Rights Reserved
 *      File:
 *				SQLiteHelper.cs
 *      Description:
 *				 SQL���ݷ��ʸ�����
 *      Author:
 *				��С��
 *				1297953037@qq.com
 *				http://www.figkey.com
 *      Finish DateTime:
 *				2020��03��18��
 *      History:
 *      
 ***********************************************************************************/
using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration;
using System.Data;
using System.Data.SQLite;
using CommonUtils.Logger;

namespace CableTestManager.Data
{
    /// <summary>
    /// SQLite���ݷ��ʸ�����
    /// </summary>
    public class SQLiteHelper
    {
        public static readonly string CONSTR = ConfigurationManager.ConnectionStrings["SQLiteString"].ConnectionString;

        /// <summary>
        /// ��ȡ���Ӷ���
        /// </summary>
        /// <param name="connectionString">�����ַ���</param>
        /// <returns>�������Ӷ���</returns>
        private static SQLiteConnection CreateConnection(string connectionString)
        {
            SQLiteConnection con = new SQLiteConnection(connectionString);
            con.Open();
            return con;
        }
        /// <summary>
        /// ִ����ɾ�ĵĸ�������
        /// </summary>
        /// <param name="connectionString">�����ַ���</param>
        /// <param name="cmdType">��������</param>
        /// <param name="cmdText">Ҫִ�е�SQL����洢������</param>
        /// <param name="values">SQL����еĲ���</param>
        /// <returns>������Ӱ�������</returns>
        public static int ExecuteCommand(string connectionString, CommandType cmdType, string cmdText, SQLiteParameter[] values)
        {
            using (SQLiteConnection con = CreateConnection(connectionString))
            {
                SQLiteCommand cmd = new SQLiteCommand();
                cmd.Connection = con;
                cmd.CommandType = cmdType;
                cmd.CommandText = cmdText;
                if (values != null && values.Length > 0) cmd.Parameters.AddRange(values);
                int result = cmd.ExecuteNonQuery();
                con.Close();
                return result;
            }
        }
        /// <summary>
        /// ִ�в�ѯ�ķ���
        /// </summary>
        /// <param name="connectionString">�����ַ���</param>
        /// <param name="cmdType">��������</param>
        /// <param name="cmdText">Ҫִ�е�SQL����洢������</param>
        /// <param name="values">SQL����еĲ���</param>
        /// <returns>�������ݶ�ȡ������</returns>
        public static SQLiteDataReader GetReader(string connectionString, CommandType cmdType, string cmdText, SQLiteParameter[] values)
        {
            SQLiteConnection con = CreateConnection(connectionString);
            SQLiteCommand cmd = new SQLiteCommand();
            cmd.Connection = con;
            cmd.CommandType = cmdType;
            cmd.CommandText = cmdText;
            if (values != null && values.Length > 0) cmd.Parameters.AddRange(values);
            SQLiteDataReader reader = cmd.ExecuteReader(CommandBehavior.CloseConnection);
            return reader;
        }
        /// <summary>
        /// ִ�д��ۺϺ����Ĳ�ѯ����
        /// </summary>
        /// <param name="connectionString">�����ַ���</param>
        /// <param name="cmdType">��������</param>
        /// <param name="cmdText">Ҫִ�е�SQL����洢������</param>
        /// <param name="values">SQL����еĲ���</param>
        /// <returns>����ִ�н���е�һ�е�һ�е�ֵ</returns>
        public static object GetScalar(string connectionString, CommandType cmdType, string cmdText, SQLiteParameter[] values)
        {
            using (SQLiteConnection con = CreateConnection(connectionString))
            {
                SQLiteCommand cmd = new SQLiteCommand();
                cmd.Connection = con;
                cmd.CommandType = cmdType;
                cmd.CommandText = cmdText;
                if (values != null && values.Length > 0) cmd.Parameters.AddRange(values);
                object result = cmd.ExecuteScalar();
                con.Close();
                return result;
            }
        }
        /// <summary>
        /// ִ�в�ѯ�ķ������������ݼ�
        /// </summary>
        /// <param name="connectionString">���ݿ������ַ���</param>
        /// <param name="cmdType">��������</param>
        /// <param name="cmdText">Ҫִ�е�SQL����洢������</param>
        /// <param name="values">SQL����洢���̵Ĳ����б�</param>
        /// <returns>������Ӧ�����ݼ�</returns>
        public static DataSet GetDataSet(string connectionString, CommandType cmdType, string cmdText, SQLiteParameter[] values)
        {
            DataSet ds = new DataSet();
            try
            {
                using (SQLiteConnection con = new SQLiteConnection(connectionString))
                {
                    SQLiteDataAdapter adapter = new SQLiteDataAdapter();
                    adapter.SelectCommand = new SQLiteCommand();
                    adapter.SelectCommand.Connection = con;
                    adapter.SelectCommand.CommandType = cmdType;
                    adapter.SelectCommand.CommandText = cmdText;
                    if (values != null && values.Length > 0) adapter.SelectCommand.Parameters.AddRange(values);
                    adapter.Fill(ds);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Log.Error($"{cmdText}"+ex.Message + ex.StackTrace);
            }
            return ds;
        }
    }
}