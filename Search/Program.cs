using Ionic.Zip;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Threading;
namespace Search
{
    internal class Program
    {
        private static string sDate;                                                                // Holds the current log Date
        private static string sLogPath = Environment.GetEnvironmentVariable("ININ_TRACE_ROOT");     // Grabs the log path from the ININ_TRACE_ROOT variable
        private static string sSearchTerm;                                                          // Holds the search term
        private static string sFullLogPath;                                                         // Full log with date variable
        private static string sEmailAddress = null;                                                 // Email address to send file list to                                                                   
        private static List<string> logList = new List<string>();                                   // Stores items found with sSearchTerm in them
        private static Logger logs = LogManager.GetCurrentClassLogger();                            // Console / Log file logging
        private static string sRawLogExtension = "*.ininlog";                                       // Holds the raw log file extension
        private static string sCompressedLogExtension = "*.zip";                                    // Hold the compressed log file extension
        private static bool bParallelProcessing;                                                    // Use parallel processing : true - yes | false - no
        private static string sUserDomain = Environment.UserDomainName;                             // Used to detect the user domain : smtp relays
        private static int[] _SMTPport = new int[2] { 25, 587 };                                    // SMTP Port Array : 25 | 587
        private static string[] _SMTPserver = new string[4]
        {
            "smtp.admin.inin.local",                                                                // 0 - AD server : SMTP Relay
            "smtp.caas.local",                                                                      // 1 - CaaS server : SMTP Relay
            "ex.intu.inin.local",                                                                    // 2 - Intu server : SMTP Relay
            "smtp.inin.com"                                                                         // 3 - ININ Server : SMTP
        };

        [STAThread]
        static void Main(string[] args)
        {
            bParallelProcessing = Convert.ToBoolean(ConfigurationManager.AppSettings["parallel"]);  // Runs based on value in config file
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException); // Handles unhandled Exception errors
            Console.ResetColor();                                                                   // Resets the console display color
            Console.ForegroundColor = ConsoleColor.Green;                                           // Sets the console display color to green
            Console.WriteLine("Enter date to search - [yyyy-mm-dd]");                                              
            Console.ResetColor();                                                                   // Resets the console display color
            sDate = Console.ReadLine();                                                             // Add's the inputed value to sDate
            Console.ForegroundColor = ConsoleColor.Cyan;                                            // Sets the console display color to cyan
            Console.WriteLine("Enter a search term - [Interaction ID, String, etc]");
            Console.ResetColor();                                                                   // Resets the console display color
            sSearchTerm = Console.ReadLine();                                                       // Add's the inputted value to sSearchTerm
            Console.ForegroundColor = ConsoleColor.Magenta;                                         // Sets the console display color to blue
            Console.WriteLine("Enter email address - [Optional will send list of found logs]");
            Console.ResetColor();                                                                   // Resets the console display color
            sEmailAddress = Console.ReadLine();                                                     // Add's the inputted value to sEmailAddress
            sFullLogPath = sLogPath + "\\" + sDate + "\\";                                          // Combines the sLogPath with the entered sDate

            if (sDate.Equals(DateConversion(DateTime.Today.ToString("yyyy-MM-dd"))))                // Checks if the date is today
            { SearchLogs("today", sRawLogExtension); }                                              // If today search raw logs 
            else
            { SearchLogs("nottoday", sCompressedLogExtension); }                                    // If not today search compressed logs

            if (string.IsNullOrEmpty(sEmailAddress))                                                // Checks if sEmailAddress is empty
            { /* Do nothing */ }                                                                    // If empty skip don't send email
            else
            { 
                try
                {
                    switch (sUserDomain)                                                            // Analyizes the Domain and processes accordingly
                    {
                        case "ad":                                                                  // AD (AdminHub)
                        case "AD":
                            Send_Email(_SMTPserver[0], _SMTPport[0], false);                        // Populate the values from the arrays
                            break;

                        case "caas":                                                                // CaaS
                        case "CAAS":
                        case "remote":                                                              // Remote
                        case "REMOTE":
                            Send_Email(_SMTPserver[1], _SMTPport[1], false);                        // Populate the values from the arrays
                            break;
                        
                        case "intu":                                                                // Intu
                        case "INTU":
                            Send_Email(_SMTPserver[2], _SMTPport[1], true);                         // Populate the values from the arrays
                            break;
                        default:
                            Send_Email(_SMTPserver[3], _SMTPport[0], false);                        // Populates the values from arrays
                            break;
                    }
                }
                catch (UnauthorizedAccessException ae)                                              // Catches unauthorized access messages
                {
                    logs.Fatal(ae.Message);                                                         // Logs to console & files
                }
                catch (SystemException se)                                                          // Catches system execptions
                {
                    logs.Fatal(se.Message);                                                         // Logs to console & files
                }
                catch (ApplicationException ape)                                                    // Catches application exceptions
                {
                    logs.Fatal(ape.Message);                                                        // Logs to console & files
                }
                catch (Exception e)                                                                 // Catches exceptions
                {
                    logs.Fatal(e.Message);                                                          // Logs to console & files
                }
            }                                                                                     // If sEmailAddress has value Send Email * Works on most servers *

            Console.ForegroundColor = ConsoleColor.Gray;                                            // Sets the console display color to gray
            Console.WriteLine("");                                                                  // New line in console
            Console.WriteLine("");                                                                  // New line in console
            Console.WriteLine("Search has finished");
            Console.ResetColor();                                                                   // Resets the console display color
            Console.ForegroundColor = ConsoleColor.Red;                                             // Sets the console display color to red
            Console.WriteLine("");                                                                  // New line in console
            for (int c = 9; c >= 0; c--)                                                            // Simple 9 second countdown, then application closes
            {
                Console.Write("\rApplication will close in {0}", c);
                System.Threading.Thread.Sleep(1000);
            } 
        }

        /// <summary>
        /// Handles the UnhandledException event of the CurrentDomain control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="ue">The <see cref="UnhandledExceptionEventArgs"/> instance containing the event data.</param>
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs ue)
        {
            try
            {
                Exception ex = (Exception)ue.ExceptionObject;
                logs.Fatal(ex.Message);
            }
            finally
            {
                Environment.Exit(0);
            }
        }

        /// <summary>
        /// Dates time conversion.
        /// </summary>
        /// <param name="sDateTime">The date time in yyy-MM-dd format.</param>
        /// <returns></returns>
        private static string DateConversion(string sDateTime)
        {
            string.Format("{0:yyyy-MM-dd}", sDateTime);
            return sDateTime;
        }

        /// <summary>
        /// Searches the logs.
        /// </summary>
        /// <param name="type">The type today | nottoday </param>
        /// <param name="extension">The extension of the log.</param>
        private static void SearchLogs(string type, string extension)
        {
            Console.Clear();                                                                                    // Clears console display
            DirectoryInfo dInfo = new DirectoryInfo(sFullLogPath);                                              // Sets the current directory to sFullLogPath
            FileInfo[] files;                                                                                   // Array to store the files in the directory 
            switch (bParallelProcessing)                                                                        // Processes based on true | false
            {
                case true:
                    logs.Fatal("Parallel Mode");
                    switch (type)                                                                               // Determines which node to hit based on the type
                    {
                        case "today":                                                                           // Today - .ininlog
                            files = dInfo.GetFiles(extension);                                                  // Specifies to only look for .ininlog  files - ignores everything elses
                            logs.Info("Searching logs in " + sDate + " for " + sSearchTerm);                    // Logs to console & files
                            Console.WriteLine("");                                                              // New line console
                            try
                            {
                                //var sw = Stopwatch.StartNew();
                                Parallel.ForEach(files, file =>                                                 // Parallel processing
                                {
                                    try
                                    {
                                        Stream strmTemp = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);   // Temporary file stream
                                        StreamReader strmReader = new StreamReader(strmTemp);                                                   // Reads the stream from strmTemp
                                        String line;                                                                                            // Cursor

                                        while ((line = strmReader.ReadLine()) != null)                                                          // Reads until there is no more data
                                        {
                                            if (line.Contains(sSearchTerm))                                                                     // Does the current line contain the Search Term?
                                            {
                                                logs.Info("{0} contains \"{1}\"", file.Name, sSearchTerm);                                      // Add to console display
                                                logList.Add(file.Name);                                                                         // Add to array
                                                break;                                                                                          // Stop and move onto the next file    
                                            }
                                        }
                                        logList.Sort();                                                                                         // Sorts list A-Z
                                    }
                                    catch (UnauthorizedAccessException ae)                                              // Catches unauthorized access messages
                                    {
                                        logs.Fatal(ae.Message);                                                         // Logs to console & files
                                    }
                                    catch (SystemException se)                                                          // Catches system execptions
                                    {
                                        logs.Fatal(se.Message);                                                         // Logs to console & files
                                    }
                                    catch (ApplicationException ape)                                                    // Catches application exceptions
                                    {
                                        logs.Fatal(ape.Message);                                                        // Logs to console & files
                                    }
                                    catch (Exception e)                                                                 // Catches exceptions
                                    {
                                        logs.Fatal(e.Message);                                                          // Logs to console & files
                                    }
                                    });   
                            }
                            catch (UnauthorizedAccessException ae)                                              // Catches unauthorized access messages
                            {
                                logs.Fatal(ae.Message);                                                         // Logs to console & files
                            }
                            catch (SystemException se)                                                          // Catches system execptions
                            {
                                logs.Fatal(se.Message);                                                         // Logs to console & files
                            }
                            catch (ApplicationException ape)                                                    // Catches application exceptions
                            {
                                logs.Fatal(ape.Message);                                                        // Logs to console & files
                            }
                            catch (Exception e)                                                                 // Catches exceptions
                            {
                                logs.Fatal(e.Message);                                                          // Logs to console & files
                            }
                            break;                                                                              // Stop the code from hitting the next case
                        case "nottoday":                                                                        // Not today - .zip
                            files = dInfo.GetFiles(extension);                                                  // Specifies to only look for .ininlog  files - ignores everything elses
                            logs.Info("Searching logs in " + sDate + " for " + sSearchTerm);                    // Logs to console & files
                            Console.WriteLine("");                                                              // New line console
                            try
                            {
                                Parallel.ForEach(files, file =>                                                 // Parallel processing
                                {
                                    using (ZipFile archive = ZipFile.Read(file.FullName))                       // Loads the current zip
                                    {
                                        Parallel.ForEach(archive, entry =>                                      // Parallel processing
                                        {
                                           if (entry.FileName.EndsWith(".ininlog", StringComparison.OrdinalIgnoreCase))     // Looks for only files that have a .ininlog extension
                                           {
                                               try
                                               {
                                                   using (var tmpStream = entry.OpenReader())
                                                   using (var strmReader = new StreamReader(tmpStream))
                                                   {
                                                       String line;                                                                                            // Cursor

                                                       while ((line = strmReader.ReadLine()) != null)                                                          // Reads until there is no more data
                                                       {
                                                           if (line.Contains(sSearchTerm))                                                                     // Does the current line contain the Search Term?
                                                           {
                                                               logs.Info("{0} contains \"{1}\"", file.Name, sSearchTerm);                                      // Add to console display
                                                               logList.Add(file.Name);                                                                         // Add to array
                                                               break;                                                                                          // Stop and move onto the next file    
                                                           }
                                                       }
                                                       logList.Sort();                                                                                        // Sorts list A-Z
                                                   }
                                               }
                                               catch (UnauthorizedAccessException ae)                                              // Catches unauthorized access messages
                                               {
                                                   logs.Fatal(ae.Message);                                                         // Logs to console & files
                                               }
                                               catch (SystemException se)                                                          // Catches system execptions
                                               {
                                                   logs.Fatal(se.Message);                                                         // Logs to console & files
                                               }
                                               catch (ApplicationException ape)                                                    // Catches application exceptions
                                               {
                                                   logs.Fatal(ape.Message);                                                        // Logs to console & files
                                               }
                                               catch (Exception e)                                                                 // Catches exceptions
                                               {
                                                   logs.Fatal(e.Message);                                                          // Logs to console & files
                                               }
                                           }
                                        });
                                    }
                                });
                            }
                            catch (UnauthorizedAccessException ae)                                              // Catches unauthorized access messages
                            {
                                logs.Fatal(ae.Message);                                                         // Logs to console & files
                            }
                            catch (SystemException se)                                                          // Catches system execptions
                            {
                                logs.Fatal(se.Message);                                                         // Logs to console & files
                            }
                            catch (ApplicationException ape)                                                    // Catches application exceptions
                            {
                                logs.Fatal(ape.Message);                                                        // Logs to console & files
                            }
                            catch (Exception e)                                                                 // Catches exceptions
                            {
                                logs.Fatal(e.Message);                                                          // Logs to console & files
                            }
                            break;
                    }
                    break;
                case false:
                    logs.Fatal("Normal Mode");
                    switch (type)                                                                               // Determines which node to hit based on the type
                    {
                        case "today":                                                                           // Today - .ininlog
                            files = dInfo.GetFiles(extension);                                                  // Specifies to only look for .ininlog  files - ignores everything elses
                            logs.Info("Searching logs in " + sDate + " for " + sSearchTerm);                    // Logs to console & files
                            Console.WriteLine("");                                                              // New line console
                            try
                            {
                                foreach(FileInfo file in files)
                                {
                                    try
                                    {
                                        Stream strmTemp = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);   // Temporary file stream
                                        StreamReader strmReader = new StreamReader(strmTemp);                                                   // Reads the stream from strmTemp
                                        String line;                                                                                            // Cursor

                                        while ((line = strmReader.ReadLine()) != null)                                                          // Reads until there is no more data
                                        {
                                            if (line.Contains(sSearchTerm))                                                                     // Does the current line contain the Search Term?
                                            {
                                                logs.Info("{0} contains \"{1}\"", file.Name, sSearchTerm);                                      // Add to console display
                                                logList.Add(file.Name);                                                                         // Add to array
                                                break;                                                                                          // Stop and move onto the next file    
                                            }
                                        }
                                        logList.Sort();                                                                                         // Sorts list A-Z
                                    }
                                    catch (UnauthorizedAccessException ae)                                              // Catches unauthorized access messages
                                    {
                                        logs.Fatal(ae.Message);                                                         // Logs to console & files
                                    }
                                    catch (SystemException se)                                                          // Catches system execptions
                                    {
                                        logs.Fatal(se.Message);                                                         // Logs to console & files
                                    }
                                    catch (ApplicationException ape)                                                    // Catches application exceptions
                                    {
                                        logs.Fatal(ape.Message);                                                        // Logs to console & files
                                    }
                                    catch (Exception e)                                                                 // Catches exceptions
                                    {
                                        logs.Fatal(e.Message);                                                          // Logs to console & files
                                    }
                                }
                            }
                            catch (UnauthorizedAccessException ae)                                              // Catches unauthorized access messages
                            {
                                logs.Fatal(ae.Message);                                                         // Logs to console & files
                            }
                            catch (SystemException se)                                                          // Catches system execptions
                            {
                                logs.Fatal(se.Message);                                                         // Logs to console & files
                            }
                            catch (ApplicationException ape)                                                    // Catches application exceptions
                            {
                                logs.Fatal(ape.Message);                                                        // Logs to console & files
                            }
                            catch (Exception e)                                                                 // Catches exceptions
                            {
                                logs.Fatal(e.Message);                                                          // Logs to console & files
                            }
                            break;                                                                              // Stop the code from hitting the next case
                        case "nottoday":                                                                        // Not today - .zip
                            files = dInfo.GetFiles(extension);                                                  // Specifies to only look for .ininlog  files - ignores everything elses
                            logs.Info("Searching logs in " + sDate + " for " + sSearchTerm);                    // Logs to console & files
                            Console.WriteLine("");                                                              // New line console
                            try
                            {
                                foreach(var file in files)
                                {
                                    using (ZipFile archive = ZipFile.Read(file.FullName))                       // Loads the current zip
                                    {
                                        foreach(var entry in archive)
                                        {
                                            if (entry.FileName.EndsWith(".ininlog", StringComparison.OrdinalIgnoreCase))     // Looks for only files that have a .ininlog extension
                                            {
                                                try
                                                {
                                                    using (var tmpStream = entry.OpenReader())
                                                    using (var strmReader = new StreamReader(tmpStream))
                                                    {
                                                        String line;                                                                                            // Cursor

                                                        while ((line = strmReader.ReadLine()) != null)                                                          // Reads until there is no more data
                                                        {
                                                            if (line.Contains(sSearchTerm))                                                                     // Does the current line contain the Search Term?
                                                            {
                                                                logs.Info("{0} contains \"{1}\"", file.Name, sSearchTerm);                                      // Add to console display
                                                                logList.Add(file.Name);                                                                         // Add to array
                                                                break;                                                                                          // Stop and move onto the next file    
                                                            }
                                                        }
                                                        logList.Sort();                                                                                         // Sorts list A-Z
                                                    }
                                                }
                                                catch (UnauthorizedAccessException ae)                                              // Catches unauthorized access messages
                                                {
                                                    logs.Fatal(ae.Message);                                                         // Logs to console & files
                                                }
                                                catch (SystemException se)                                                          // Catches system execptions
                                                {
                                                    logs.Fatal(se.Message);                                                         // Logs to console & files
                                                }
                                                catch (ApplicationException ape)                                                    // Catches application exceptions
                                                {
                                                    logs.Fatal(ape.Message);                                                        // Logs to console & files
                                                }
                                                catch (Exception e)                                                                 // Catches exceptions
                                                {
                                                    logs.Fatal(e.Message);                                                          // Logs to console & files
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            catch (UnauthorizedAccessException ae)                                              // Catches unauthorized access messages
                            {
                                logs.Fatal(ae.Message);                                                         // Logs to console & files
                            }
                            catch (SystemException se)                                                          // Catches system execptions
                            {
                                logs.Fatal(se.Message);                                                         // Logs to console & files
                            }
                            catch (ApplicationException ape)                                                    // Catches application exceptions
                            {
                                logs.Fatal(ape.Message);                                                        // Logs to console & files
                            }
                            catch (Exception e)                                                                 // Catches exceptions
                            {
                                logs.Fatal(e.Message);                                                          // Logs to console & files
                            }
                            break;
                    }
                    break;
            }
        }
        /// <summary>
        /// Send_s the email.
        /// </summary>
        /// <param name="_server">The _server.</param>
        /// <param name="_port">The _port.</param>
        /// <param name="_ssl">if set to <c>true</c> [SSL].</param>
        private static void Send_Email(string _server, int _port, bool _ssl)
        {
            StringBuilder emailString = new StringBuilder();                                                    // Creates a StringBuilder for the outgoing email 
            MailMessage mMessage = new MailMessage();                                                           // Creates a new mail message object

            SmtpClient smtpClient = new SmtpClient(_server);                                                    // Creates a new smtp client using the passed _server value
            smtpClient.Port = _port;                                                                            // Assigns the smtpClient port from the _port value
            smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;                                             // Sets the smtpClient deleviery method to network

            if (_ssl.Equals(true))                                                                              // Checks if there is a SSL connection required
            { smtpClient.EnableSsl = true; }
            
            ServicePointManager.ServerCertificateValidationCallback = delegate(object s, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
            { return true; };
            
            mMessage.From = new MailAddress("no-reply@inin.com");                                               // Sets the mMessage.Form to "no-reply@inin.com"
            mMessage.To.Add(sEmailAddress);                                                                     // Sets the mMessage.To value based sEmailAddress

            mMessage.Subject = "Logs from " + Environment.GetEnvironmentVariable("COMPUTERNAME") + " searched " + sDate + " for " + sSearchTerm;
            emailString.Append("<p style='font-family:arial,helvetica,sans-serif;'>Here are the file(s) on <b>" + Environment.GetEnvironmentVariable("COMPUTERNAME") + "</b> under <b>" + sDate + "</b> that contain <b><i>" + sSearchTerm + "</i><b></p><br/><br/>");
            emailString.AppendLine("<table width='100%' border='0' align='center' cellpadding='5' cellspacing='0' style='font-family:arial,helvetica,sans-serif;'><tbody><tr><td style='padding:5px;background-color:rgb(169, 169, 169);color:white;'>Filenames</td></tr>");
            int count = 0;
            foreach (var item in logList)
            {
                if (count % 2 == 0)
                {
                    emailString.AppendLine("<tr><td style='background-color:#bada55;border-collapse:collapse;'>" + Path.GetFileName(item) + "</td></tr>");
                }
                else
                {
                    emailString.AppendLine("<tr><td style='background-color:#55bada;border-collapse:collapse;'>" + Path.GetFileName(item) + "</td></tr>");
                }
                count++;
            }
            emailString.Append("</tr></tbody></table>");
            mMessage.IsBodyHtml = true;

            mMessage.Body = emailString.ToString();

            try
            {
                smtpClient.Send(mMessage);
            }
            catch (SmtpFailedRecipientsException sres)
            {
                logs.Fatal(sres.Message);
            }
            catch (SmtpFailedRecipientException sre)
            {
                logs.Fatal(sre.Message);
            }
            catch (SmtpException se)
            {
                logs.Fatal(se.Message);
            }
        }
    }
}
