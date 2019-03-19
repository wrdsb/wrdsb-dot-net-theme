using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Configuration;
using System.Net.Mail;
using NLog;
using NLog.Targets;
using NLog.Config;
using NLog.AWS.Logger;
using System.Web.Configuration;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace DotNetThemeMVC.Controllers
{
    public class Error
    {
        /// <summary>
        /// This will email super admins a notification of an exception. The exception details
        /// are logged to AWS CloudWatch.
        /// </summary>
        /// <param name="exception">The captured exception object.</param>
        /// <param name="emailText">The body text to send in the email.</param>
        public void handleError(Exception exception, string emailText)
        {
            //Send the Super Admins a notification
            try
            {
                //Let's not get stuck in a loop, if an excpeption occurs at the email code don't try to email
                if (emailText != "Exception occured attempting to send email.")
                {
                    //Fire off email
                    Email email = new Email();

                    //Add custom recipients here
                    //email.SendEmail("_____@wrdsb.on.ca", WebConfigurationManager.AppSettings["loginTitle"].ToString() + " Exception", emailText);
                    //email.SendEmail("_____@googleapps.wrdsb.ca", WebConfigurationManager.AppSettings["loginTitle"].ToString() + " Exception", emailText);

                    //Add Exception details to the email message text in the same format that is sent to AWS below
                    //if stack trace is null reference then targetsite also returns null reference
                    //Get the name of the method that threw the exception
                    MethodBase site = exception.TargetSite;
                    string methodName = site == null ? null : site.Name;

                    //Get the  filename and linenumber that threw the exception
                    var st = new StackTrace(exception, true);
                    var frame = st.GetFrame(0);
                    var line = frame.GetFileLineNumber(); 
                    var filename = frame.GetFileName();

                    //Attach the full error message to the custom one sent in from the controller source
                    //Current Format is: [FileName:value][MethodName:value][LineNumber:value][RawMessage:value]
                    var full_error_message = "[Filename:" + filename + "][MethodName:" + methodName + "][LineNumber:" + line + "][RawMessage:" + exception.Message.ToString() + "]";
                    emailText += full_error_message;

                    //Email Super Admins about the exception
                    email.EmailSuperAdmins(WebConfigurationManager.AppSettings["loginTitle"].ToString() + " Exception", emailText);
                }
            }
            //Logging to the System Error log requires some configuration
            //1) Add a Registry(Folder) Key on the server/localhost. The last folder name matching the web config loginTitle key value
            // -> “HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\eventlog\WRDSB\System.Web.Configuration.WebConfigurationManager.AppSettings["loginTitle"].ToString()”
            //2) Add a "String Value" titled "EventMessageFile" with a value of "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\EventLogMessages.dll"
            //See Documentation for more detailed instructions:
            //https://staff.wrdsb.ca/software-development/documentation/dot-net-theme/general-configuration-options/
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
                log.Source = WebConfigurationManager.AppSettings["loginTitle"].ToString();
                log.WriteEntry(errorMessage, System.Diagnostics.EventLogEntryType.Error);
            }

            //Send the exception information to AWS Cloudwatch
            //This requires configuration, see documentation:
            //https://staff.wrdsb.ca/software-development/documentation/dot-net-theme/general-configuration-options/
            try
            {
                //Set the logging group based on debug/production switch
                //Logs will be sent to "/dotnet/$your_applictation_name/$environment
                #if DEBUG
                string environment = "test";
                #else
                string environment = "production";
                #endif

                //Set the AWS values, log group, log level
                var config = new LoggingConfiguration();

                //The log group name cannot have spaces, but the loginTitle is meant to be human readable, transform it for format AWS requires
                var application_name = WebConfigurationManager.AppSettings["loginTitle"].ToString().ToLower().Replace(" ", "_");

                var awsTarget = new AWSTarget()
                {
                    LogGroup = "/dotnet/" + application_name + "/" + environment,
                    Region = "us-east-1"
                };
                config.AddTarget("aws", awsTarget);
                config.LoggingRules.Add(new LoggingRule("*", LogLevel.Fatal, awsTarget));
                LogManager.Configuration = config;
                Logger logger = LogManager.GetCurrentClassLogger();

                //if stack trace is null reference then targetsite also returns null reference
                //Get the name of the method that threw the exception
                MethodBase site = exception.TargetSite;
                string methodName = site == null ? null : site.Name;

                //Get the  filename and linenumber that threw the exception
                var st = new StackTrace(exception, true);
                var frame = st.GetFrame(0);
                var line = frame.GetFileLineNumber();
                var filename = frame.GetFileName();
                
                //Send the event to AWS CloudWatch
                //Current Format is: [FileName:value][MethodName:value][LineNumber:value][RawMessage:value]
                //This will be found in the Message portion of Cloudwatch logs
                logger.Fatal("[Filename:" + filename + "][MethodName:" + methodName + "][LineNumber:" + line + "][RawMessage:" + exception.Message.ToString()+"]");
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
                log.Source = WebConfigurationManager.AppSettings["loginTitle"].ToString();
                log.WriteEntry(errorMessage, System.Diagnostics.EventLogEntryType.Error);
            }

            //Send the exception information to Azure Log Analytics
            //This requires configuration, see documentation:
            //https://docs.microsoft.com/en-us/azure/azure-monitor/platform/data-collector-api
            string result = string.Empty;
            try
            {
                string customer_id = WebConfigurationManager.AppSettings["workspace_id"].ToString();
                string shared_key = WebConfigurationManager.AppSettings["primary_key"].ToString();

#if DEBUG
                string environment = "test";
#else
                string environment = "production";
#endif
                var application_name = WebConfigurationManager.AppSettings["loginTitle"].ToString().ToLower().Replace(" ", "_");

                string log_name = "/dotnet/" + application_name + "/" + environment;

                string timestamp = DateTime.Now.ToString();

                //if stack trace is null reference then targetsite also returns null reference
                //Get the name of the method that threw the exception
                MethodBase site = exception.TargetSite;
                string methodName = site == null ? null : site.Name;

                //Get the  filename and linenumber that threw the exception
                var st = new StackTrace(exception, true);
                var frame = st.GetFrame(0);
                var line = frame.GetFileLineNumber();
                var filename = frame.GetFileName();

                dynamic jsonObject = new JObject();
                jsonObject.Add("FileName", filename);
                jsonObject.Add("MethodName", methodName);
                jsonObject.Add("LineNumber", line);
                jsonObject.Add("RawMessage", exception.Message.ToString());

                string json = jsonObject.ToString(Newtonsoft.Json.Formatting.None);

                // Create a hash for the API signature
                var datestring = DateTime.UtcNow.ToString("r");
                var jsonBytes = Encoding.UTF8.GetBytes(json);
                string stringToHash = "POST\n" + jsonBytes.Length + "\napplication/json\n" + "x-ms-date:" + datestring + "\n/api/logs";
                string hashedString = BuildSignature(stringToHash, shared_key);
                string signature = "SharedKey " + customer_id + ":" + hashedString;

                string url = "https://" + customer_id + ".ods.opinsights.azure.com/api/logs?api-version=2016-04-01";

                System.Net.Http.HttpClient client = new System.Net.Http.HttpClient();
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Add("Log-Type", log_name);
                client.DefaultRequestHeaders.Add("Authorization", signature);
                client.DefaultRequestHeaders.Add("x-ms-date", datestring);
                client.DefaultRequestHeaders.Add("time-generated-field", timestamp);

                System.Net.Http.HttpContent httpContent = new StringContent(json, Encoding.UTF8);
                httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                Task<System.Net.Http.HttpResponseMessage> response = client.PostAsync(new Uri(url), httpContent);

                System.Net.Http.HttpContent responseContent = response.Result.Content;
                result = responseContent.ReadAsStringAsync().Result;

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
                if(!String.IsNullOrEmpty(result))
                {
                    errorMessage += "\r\nAzureResponse: " + result;
                }
                log.Source = WebConfigurationManager.AppSettings["loginTitle"].ToString();
                log.WriteEntry(errorMessage, System.Diagnostics.EventLogEntryType.Error);
            }
        }

        public static string BuildSignature(string message, string secret)
        {
            var encoding = new System.Text.ASCIIEncoding();
            byte[] keyByte = Convert.FromBase64String(secret);
            byte[] messageBytes = encoding.GetBytes(message);
            using (var hmacsha256 = new HMACSHA256(keyByte))
            {
                byte[] hash = hmacsha256.ComputeHash(messageBytes);
                return Convert.ToBase64String(hash);
            }
        }
    }
}