using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Configuration;
using System.Net.Mail;

namespace DotNetThemeMVC.Controllers
{
    public class Error
    {
        public void handleError(Exception exception, string emailText)
        {
            try
            {
                //Fire off email
                MailMessage message = new MailMessage();
                message.From = new MailAddress("noreply@wrdsb.on.ca", "WRDSB");
                message.To.Add(new MailAddress("_____@googleapps.wrdsb.ca"));
                message.To.Add(new MailAddress("_____@wrdsb.on.ca"));

                message.Subject = System.Web.Configuration.WebConfigurationManager.AppSettings["loginTitle"].ToString() + " Exception";

                message.Body = emailText;
                SmtpClient client = new SmtpClient();
                client.Send(message);
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
                //LINK

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