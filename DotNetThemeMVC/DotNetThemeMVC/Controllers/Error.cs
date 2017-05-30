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

namespace DotNetThemeMVC.Controllers
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

                    //Add custom recipients here
                    //email.SendEmail("_____@wrdsb.on.ca", WebConfigurationManager.AppSettings["loginTitle"].ToString() + " Exception", emailText);
                    //email.SendEmail("_____@googleapps.wrdsb.ca", WebConfigurationManager.AppSettings["loginTitle"].ToString() + " Exception", emailText);
                    
                    //If you want to email administrators
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

            try
            {

#if DEBUG
                string environment = "test";
#else
                string environment = "production";
#endif
                var config = new LoggingConfiguration();
                var awsTarget = new AWSTarget()
                {
                    LogGroup = "/dotnet/" + WebConfigurationManager.AppSettings["loginTitle"].ToString().ToLower() + "/" + environment,
                    Region = "us-east-1"
                };
                config.AddTarget("aws", awsTarget);
                config.LoggingRules.Add(new LoggingRule("*", LogLevel.Fatal, awsTarget));
                LogManager.Configuration = config;
                Logger logger = LogManager.GetCurrentClassLogger();
                //logger.Info("Check the AWS Console Cloudwatch Logs in us-east-1");
                //logger.Fatal("Fatal error occured");

                //if stack trace is null reference then targetsite also returns null reference
                //Get the name of the method that threw the exception
                MethodBase site = exception.TargetSite;
                string methodName = site == null ? null : site.Name;

                //Get the  filename and linenumber that threw the exception
                var st = new StackTrace(exception, true);
                var frame = st.GetFrame(0);
                var line = frame.GetFileLineNumber();
                var filename = frame.GetFileName();
                
                //Compare what gets logged
                //come up with a good format
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
        }
    }
}