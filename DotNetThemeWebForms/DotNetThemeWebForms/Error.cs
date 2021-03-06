﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Configuration;
using System.Net.Mail;

namespace DotNetTheme
{
    public class Error
    {
        public void handleError(Exception exception, string emailText)
        {
            try
            {
                //Let's not get stuck in a loop, if an excpeption occurs at the email code don't try to email
                if (emailText != "Exception occured attempting to send email.")
                {
                    //Fire off email
                    Email email = new Email();
                    email.SendEmail("_____@wrdsb.on.ca", System.Web.Configuration.WebConfigurationManager.AppSettings["loginTitle"].ToString() + " Exception", emailText);
                    email.SendEmail("_____@googleapps.wrdsb.ca", System.Web.Configuration.WebConfigurationManager.AppSettings["loginTitle"].ToString() + " Exception", emailText);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.EventLog log = new System.Diagnostics.EventLog();
                string errorMessage = "Message: " + ex.Message + "\r\nSource: " + ex.Source + "\r\nStack: " + ex.StackTrace;
                if (ex.TargetSite != null)
                {
                    errorMessage += "\r\nTarget: " + ex.TargetSite.ToString();
                }

                if (ex.InnerException != null)
                {
                    errorMessage += "\r\nInner: " + ex.InnerException.ToString();
                }
                log.Source = System.Web.Configuration.WebConfigurationManager.AppSettings["loginTitle"].ToString();
                log.WriteEntry(errorMessage, System.Diagnostics.EventLogEntryType.Error);
            }

            try
            {
                //Logging to the System Error log requires some configuration
                //1) Add a Registry(Folder) Key on the server/localhost. The last folder name matching the web config loginTitle key value
                // -> “HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\eventlog\WRDSB\System.Web.Configuration.WebConfigurationManager.AppSettings["loginTitle"].ToString()”
                //2) Add a "String Value" titled "EventMessageFile" with a value of "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\EventLogMessages.dll"
                //See Documentation for more detailed instructions:
                //https://staff.wrdsb.ca/software-development/documentation/dot-net-theme/general-configuration-options/

                //if stack trace is null reference then targetsite also returns null reference
                //Get the name of the method that threw the exception
                MethodBase site = exception.TargetSite;
                string methodName = site == null ? null : site.Name;

                //Get the  filename and linenumber that threw the exception
                var st = new StackTrace(exception, true);
                var frame = st.GetFrame(0);
                var line = frame.GetFileLineNumber();
                var filename = frame.GetFileName();

                //Write the error to the database
                string query = @"INSERT INTO dbo.error_log (timestamp,file_name,method_name,line_number,error_message) VALUES (@timestamp, @fileName, @methodName, @lineNumber, @message)";
                using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["______"].ConnectionString))
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@timestamp", DateTime.Now.ToString());
                    command.Parameters.AddWithValue("@fileName", (object)filename ?? DBNull.Value);
                    command.Parameters.AddWithValue("@methodName", (object)methodName ?? DBNull.Value);
                    command.Parameters.AddWithValue("@lineNumber", (object)line ?? DBNull.Value);
                    command.Parameters.AddWithValue("@message", (object)exception.Message.ToString() ?? DBNull.Value);
                    connection.Open();
                    command.ExecuteNonQuery();
                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.EventLog log = new System.Diagnostics.EventLog();
                string errorMessage = "Message: " + ex.Message + "\r\nSource: " + ex.Source + "\r\nStack: " + ex.StackTrace;
                if (ex.TargetSite != null)
                {
                    errorMessage += "\r\nTarget: " + ex.TargetSite.ToString();
                }

                if (ex.InnerException != null)
                {
                    errorMessage += "\r\nInner: " + ex.InnerException.ToString();
                }
                log.Source = System.Web.Configuration.WebConfigurationManager.AppSettings["loginTitle"].ToString();
                log.WriteEntry(errorMessage, System.Diagnostics.EventLogEntryType.Error);
            }
        }
    }
}