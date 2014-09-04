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
        [STAThread]
        static void Main(string[] args)
        {
            bParallelProcessing = Convert.ToBoolean(ConfigurationManager.AppSettings["parallel"]);  // Runs based on value in config file
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException); // Handles unhandled Exception errors
            Console.ResetColor();                                                                   // Resets the console display color
            Console.ForegroundColor = ConsoleColor.Green;                                           // Sets the console display color to green
            Console.WriteLine("Enter date to search");                                              
            Console.WriteLine("yyyy-mm-dd format");
            Console.ResetColor();                                                                   // Resets the console display color
            sDate = Console.ReadLine();                                                             // Add's the inputed value to sDate
            Console.ForegroundColor = ConsoleColor.Cyan;                                            // Sets the console display color to cyan
            Console.WriteLine("Enter a search term");
            Console.WriteLine("Interaction ID, Status, Etc.");
            Console.ResetColor();                                                                   // Resets the console display color
            sSearchTerm = Console.ReadLine();                                                       // Add's the inputted value to sSearchTerm
            Console.ForegroundColor = ConsoleColor.Blue;                                            // Sets the console display color to blue
            Console.WriteLine("Enter email address");
            Console.ResetColor();                                                                   // Resets the console display color
            sEmailAddress = Console.ReadLine();                                                     // Add's the inputted value to sEmailAddress
            sFullLogPath = sLogPath + "\\" + sDate + "\\";                                          // Combines the sLogPath with the entered sDate

            if (sDate.Equals(DateConversion(DateTime.Today.ToString("yyyy-MM-dd"))))                // Checks if the date is today
            { SearchLogs("today", sRawLogExtension); }                                              // If today search raw logs 
            else
            { SearchLogs("nottoday", sCompressedLogExtension); }                                    // If not today search compressed logs

            if (string.IsNullOrEmpty(sEmailAddress))                                                // Checks if sEmailAddress is empty
            { }                                                                                     // If empty skip don't send email
            else
            { }                                                                                     // If sEmailAddress has value Send Email * Works on most servers *

            Console.ForegroundColor = ConsoleColor.Gray;                                            // Sets the console display color to gray
            Console.WriteLine("");                                                                  // New line in console
            Console.WriteLine("");                                                                  // New line in console
            Console.WriteLine("Search has finished");
            Console.WriteLine("Press any key to exit.");
            Console.ResetColor();                                                                   // Resets the console display color
            Console.ReadKey();                                                                      // Wait for user input to close window
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
                    switch (type)                                                                               // Determines which node to hit based on the type
                    {
                        case "today":                                                                           // Today - .ininlog
                            files = dInfo.GetFiles(extension);                                                  // Specifies to only look for .ininlog  files - ignores everything elses
                            logs.Info("Searching logs in " + sDate + " for " + sSearchTerm);                    // Logs to console & files
                            Console.WriteLine("");                                                              // New line console
                            try
                            {
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
                    
                    break;
            }
        }
    }
}
