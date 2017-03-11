// Copyright (C) 2016 SRG Technology, LLC
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;

namespace HSPI_HikAlarmCheck
{
    /// <summary>
    /// Class for the main program.
    /// </summary>
    class Program
    {
        /// <summary>
        /// The homeseer server address.  Defaults to the local computer but can be changed through the command line argument, server=address.
        /// </summary>
        private static string serverAddress = "127.0.0.1";
        private const int serverPort = 10400;

        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        /// <param name="args">Command line arguments</param>
        static void Main(string[] args)
        {
            Console.WriteLine("Hik Alarm Check Plugin");

            // parse command line arguments
            foreach (string sCmd in args)
            {
                string[] parts = sCmd.Split('=');
                switch (parts[0].ToLower())
                {
                    case "server":
                        serverAddress = parts[1];
                        break;
                }
            }

            // create an instance of our plugin.
            HSPI plugin = new HSPI();

            // Get our plugin to connect to Homeseer
            Console.WriteLine("\nConnecting to Homeseer at " + serverAddress + ":" + serverPort + " ...");
            try
            {
                plugin.Connect(serverAddress, serverPort);

                // got this far then success
                Console.WriteLine("  connection to homeseer successful.\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine("  connection to homeseer failed: " + ex.Message);
                return;
            }

            // let the plugin do it's thing, wait until it shuts down or the connection to homeseer fails.
            try
            {
                while (true)
                {
                    // do nothing for a bit
                    System.Threading.Thread.Sleep(200);

                    // test the connection to homeseer
                    if (!plugin.Connected)
                    {
                        Console.WriteLine("Connection to homeseer lost, exiting");
                        break;
                    }

                    // test for a shutdown signal
                    else if (plugin.Shutdown)
                    {
                        Console.WriteLine("Plugin has been shut down, exiting");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unhandled exception from Plugin: " + ex.Message);
            }
            Console.WriteLine("Hik Alarm Check Plugin Exit");
            System.Environment.Exit(0);
        }
    }
}
