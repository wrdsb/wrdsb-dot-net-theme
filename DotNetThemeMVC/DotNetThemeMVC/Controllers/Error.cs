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
            //Fire off email
            MailMessage message = new MailMessage();
            message.From = new MailAddress("noreply@wrdsb.on.ca", "WRDSB");
            message.To.Add(new MailAddress("dorian_twardus@googleapps.wrdsb.ca"));
            message.To.Add(new MailAddress("shawn_fitzgerald@wrdsb.on.ca"));

            message.Subject = "French Immersion Registration";

            message.Body = emailText;
            SmtpClient client = new SmtpClient();
            client.Send(message);

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
            using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["SQLForms"].ConnectionString))
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
    }
}