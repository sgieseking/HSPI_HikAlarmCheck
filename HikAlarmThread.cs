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
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Xml;

namespace HSPI_HikAlarmCheck
{
    /// <summary>
    /// Manager for the threads used to communicate with the HikVision cameras.
    /// <para/>
    /// Each connection to a camera uses a separate thread.
    /// The threads are created when the plugin initializes by access the HomeSeer devices that
    /// are associated with this plugin and then pulling the additional information from an INI file.
    /// </summary>
    class HikAlarmThreadManager
    {
        /// <summary>
        /// List of threads that communicate with the HikVision cameras.
        /// </summary>
        private static List<HikAlarmThread> hikAlarmThreads = new List<HikAlarmThread>();

        /// <summary>
        /// Add a new device/thread to communicate with an HikVision camera.
        /// </summary>
        /// <param name="plugin">The HikVisionAlarm plugin for HomeSeer.</param>
        /// <param name="refId">The device reference identifier for HomeSeer.</param>
        /// <param name="ipAddressStr">The IP address string for the camera.</param>
        /// <param name="username">The username to use for the camera.</param>
        /// <param name="password">The password to use for the camera.</param>
        public static void AddDevice(HSPI plugin, int refId, string ipAddressStr, string username, string password)
        {
            HikAlarmThread hikAlarmThread = new HikAlarmThread();
            hikAlarmThread.Start(plugin, refId, ipAddressStr, username, password);

            hikAlarmThreads.Add(hikAlarmThread);

        }

        /// <summary>
        /// Deletes the device/thread with the reference id.
        /// </summary>
        /// <param name="refId">The device reference identifier for HomeSeer.</param>
        public static void DeleteDevice(int refId)
        {
            // search through the list of threads for the correct one
            foreach (HikAlarmThread hikAlarmThread in hikAlarmThreads)
            {
                if (hikAlarmThread.RefId == refId)
                {
                    // found the referenced device - stop the thread and delete
                    hikAlarmThread.Shutdown();
                    hikAlarmThreads.Remove(hikAlarmThread);
                    break;
                }
            }
        }

        /// <summary>
        /// Shutdown all of the threads and clear the list.
        /// </summary>
        public static void Shutdown()
        {
            foreach (HikAlarmThread hikAlarmThread in hikAlarmThreads)
                hikAlarmThread.Shutdown();
            hikAlarmThreads.Clear();
        }

        /// <summary>
        /// Return a list of the currently active camera threads.
        /// </summary>
        public static NameValueCollection CameraNameList()
        {
            NameValueCollection nameList = new NameValueCollection();

            foreach (HikAlarmThread thread in hikAlarmThreads)
                nameList.Add(thread.Name, thread.RefId.ToString());

            return nameList;
        }

        /// <summary>
        /// Get the status of a camera from the HomeSeer reference id.
        /// </summary>
        /// <param name="refId">The device reference identifier for HomeSeer.</param>
        /// <param name="status">The status of the device.</param>
        /// <returns><c>true</c> if device is found, <c>false</c> otherwise.</returns>
        public static bool GetDeviceStatus(int refId, out double status)
        {
            // search through the list of threads for the correct one
            foreach (HikAlarmThread hikAlarmThread in hikAlarmThreads)
            {
                if (hikAlarmThread.RefId == refId)
                {
                    // found the referenced device
                    status = hikAlarmThread.Status;
                    return true;
                }
            }
            status = -1;
            return false;
        }
    }

    /// <summary>
    /// Thread that communicates with a specific HikVision camera.  A command is sent
    /// to the camera to provide a stream of updates on the status of motion event that
    /// are configured in the camera.  The message are normally sent at a rate of three
    /// per second.  The status of the associated HomeSeer device is updated if the motion
    /// state changes.
    /// </summary>
    class HikAlarmThread
    {
        private HSPI plugin;
        private int refId;
        private string ipAddressStr;
        private string username;
        private string password;

        private Thread hikAlarmThread;
        private bool hikClientShutdown = false;
        private Socket sock;

        private string buffer = "";
        const string xmlEndStr = "</EventNotificationAlert>";
        const string xmlStartStr = "<EventNotification";
        const string eventTypeVideoloss = "videoloss";
        const string eventStateInactive = "inactive";

        enum CameraStateEnum { Start, Connect, Send, Receive, Wait }
        private CameraStateEnum cameraState = CameraStateEnum.Start;
        private DateTime lastAlertMsg;

        enum CameraStatus { Unknown=-1, NoMotion, Motion }
        private CameraStatus hikAlarmStatus = CameraStatus.Unknown;

        /// <summary> Gets the HomeSeer name for the camera. </summary>
        public string Name
        {
            get { return plugin.DeviceName(refId); }
        }

        /// <summary> Gets the HomeSeer device reference id. </summary>
        public int RefId
        {
            get { return refId; }
        }

        /// <summary> Gets the IP address for the camera. </summary>
        public string IpAddress
        {
            get { return ipAddressStr; }
        }

        /// <summary> Gets the username for the camera login. </summary>
        public string Username
        {
            get { return username; }
        }

        /// <summary> Gets the password for the camera login. </summary>
        public string Password
        {
            get { return password; }
        }

        /// <summary> Gets the status of the camera. </summary>
        public double Status
        {
            get { return (double)hikAlarmStatus; }
        }

        /// <summary>
        /// Starts the thread to communicate with a HikVision camera.
        /// </summary>
        /// <param name="plugin">The HikVisionAlarm plugin for HomeSeer.</param>
        /// <param name="refId">The device reference identifier for HomeSeer.</param>
        /// <param name="ipAddressStr">The IP address string for the camera.</param>
        /// <param name="username">The username to use for the camera.</param>
        /// <param name="password">The password to use for the camera.</param>
        public void Start(HSPI plugin, int refId, string ipAddressStr, string username, string password)
        {
            this.plugin = plugin;
            this.refId = refId;
            this.ipAddressStr = ipAddressStr;
            this.username = username;
            this.password = password;

            hikAlarmThread = new Thread(HikEventClient);
            hikAlarmThread.Start();
        }

        /// <summary>
        /// Shutdowns this thread.  It first tries to perform a graceful shutdown but it will
        /// abort the thread if this does not appear to work.
        /// </summary>
        public void Shutdown()
        {
            // try to shutdown gracefully
            hikClientShutdown = true;
            for (int i = 0; i < 20; i++)
            {
                if (!hikAlarmThread.IsAlive)
                    break;
                Thread.Sleep(50);
            }
            // check if you need to abort
            if (hikAlarmThread.IsAlive)
                hikAlarmThread.Abort();
        }

        /// <summary>
        /// Encode a string using base64.
        /// </summary>
        /// <param name="plainText">The text to encode.</param>
        /// <returns>The text encoded using base64.</returns>
        string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        /// <summary>
        /// The thread loop used to actually communicate with the camera.  It will attempt to
        /// continually reconnect with the camera if the connection fails.
        /// </summary>
        void HikEventClient()
        {
            // Data buffer for incoming data.
            const int bufferSizeMax = 2048;
            byte[] byteBuffer = new byte[bufferSizeMax];

            // Connect to a remote device.
            try
            {
                // set the status to unknown until connected
                SetDeviceStatus(CameraStatus.Unknown);

                // Establish the remote endpoint for the socket.
                IPAddress ipAddress = IPAddress.Parse(ipAddressStr);
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, 80);
                string authenticationStr = Base64Encode(username + ":" + password);


                while (!hikClientShutdown)
                {
                    try
                    {
                        switch (cameraState)
                        {
                            case CameraStateEnum.Start:
                                {
                                    // Create a TCP/IP  socket.
                                    buffer = "";
                                    sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                                    // Begin the connection. This will end either with an exception or a callback.
                                    sock.BeginConnect(remoteEP, new AsyncCallback(ConnectCallback), this);
                                    cameraState = CameraStateEnum.Connect;
                                    break;
                                }
                            case CameraStateEnum.Connect:
                                {
                                    // Wait for connection
                                    Thread.Sleep(100);
                                    break;
                                }
                            case CameraStateEnum.Send:
                                {
                                    LogMessage("Connected to camera");

                                    // Encode the data string into a byte array.
                                    string header = "GET /Event/notification/alertStream HTTP/1.1\r\n"
                                                    + "Host: 192.168.129.65\r\n"
                                                    + "Authorization: Basic " + authenticationStr + "\r\n"
                                                    + "Connection: keep-alive\r\n"
                                                    + "\r\n";

                                    // Send the data to start the alert stream
                                    int bytesSent = sock.Send(Encoding.ASCII.GetBytes(header));
                                    cameraState = CameraStateEnum.Receive;
                                    lastAlertMsg = DateTime.Now;
                                    break;
                                }
                            case CameraStateEnum.Receive:
                                {
                                    // Wait on the socket for data
                                    ArrayList readList = new ArrayList();
                                    readList.Add(sock);
                                    Socket.Select(readList, null, null, 5000);

                                    if (sock.Available > 0)
                                    {
                                        // add new data to the buffer
                                        int length = sock.Receive(byteBuffer);
                                        buffer += System.Text.Encoding.UTF8.GetString(byteBuffer, 0, length);

                                        ProcessBuffer();
                                    }

                                    // Check if too long between messages
                                    TimeSpan span = DateTime.Now - lastAlertMsg;
                                    if (span.TotalMilliseconds > 2000)
                                    {
                                        // Attempt to reconnect
                                        LogMessage(span.TotalMilliseconds.ToString() + " ms since last message. Attempt to reconnect.");
                                        sock.Shutdown(SocketShutdown.Both);
                                        sock.Close();
                                        cameraState = CameraStateEnum.Wait;

                                        // status is unknown
                                        SetDeviceStatus(CameraStatus.Unknown);
                                    }
                                    break;
                                }
                            case CameraStateEnum.Wait:
                                {
                                    Thread.Sleep(100);
                                    cameraState = CameraStateEnum.Start;
                                    break;
                                }
                        }
                    }
                    catch (SocketException se)
                    {
                        LogMessage("Socket exception: " + se.ToString());
                        if (sock.Connected)
                        {
                            sock.Shutdown(SocketShutdown.Both);
                            sock.Close();
                        }
                        cameraState = CameraStateEnum.Start;
                        Thread.Sleep(100);
                    }
                }

                // Release the socket.
                sock.Shutdown(SocketShutdown.Both);
                sock.Close();
            }
            catch (Exception e)
            {
                LogMessage("Unexpected Exception : " + e.ToString());
            }

            // reset the status to unknown when exiting
            SetDeviceStatus(CameraStatus.Unknown);
        }

        /// <summary>
        /// Asynchronous callback for when the camera makes a connection or times out.
        /// </summary>
        /// <param name="ar">Access to the thread.</param>
        private static void ConnectCallback(IAsyncResult ar)
        {
            HikAlarmThread thread = (HikAlarmThread)ar.AsyncState;
            try
            {
                // Finish the connection
                thread.sock.EndConnect(ar);
                thread.cameraState = CameraStateEnum.Send;
            }
            catch (Exception)
            {
                // connection failed
                thread.LogMessage("Connection failed, retry");
                thread.cameraState = CameraStateEnum.Wait;
            }
        }

        /// <summary>
        /// Processes the buffer to look for an appropriate xml message.  The message is then
        /// extracted for the type of event ("eventType") and the state("eventState").
        /// Any alarm from the camera will trigger a motion event in HomeSeer.
        /// </summary>
        private void ProcessBuffer()
        {
            // check for message boundary
            int index = buffer.IndexOf(xmlEndStr);
            if (index >= 0)
            {
                int xmlStart = buffer.IndexOf(xmlStartStr);
                int xmlEnd = index + xmlEndStr.Length;
                int xmlLength = index + xmlEndStr.Length - xmlStart;
                if (xmlStart >= 0)
                {
                    try
                    {
                        string xmlStr = buffer.Substring(xmlStart, xmlLength);

                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(xmlStr);
                        XmlNodeList eventType = doc.GetElementsByTagName("eventType");
                        XmlNodeList eventState = doc.GetElementsByTagName("eventState");

                        if ((eventType[0].InnerXml != eventTypeVideoloss) || (eventState[0].InnerXml != eventStateInactive))
                        {
                            if (hikAlarmStatus != CameraStatus.Motion)
                            {
                                // motion detected and status changing
                                SetDeviceStatus(CameraStatus.Motion);
                                LogMessage(eventType[0].InnerXml);
                            }
                        }
                        else if (hikAlarmStatus != CameraStatus.NoMotion)
                        {
                            // no motion and status changing
                            SetDeviceStatus(CameraStatus.NoMotion);
                            LogMessage("Inactive");
                        }
                        lastAlertMsg = DateTime.Now;
                    }
                    catch (Exception ex)
                    {
                        SetDeviceStatus(CameraStatus.Unknown);
                        LogMessage("Xml processing error: " + ex.ToString());
                    }
                }
                // remove data through the boundary
                buffer = buffer.Substring(index + xmlEndStr.Length);
            }
        }

        /// <summary>
        /// Sets the device status.
        /// </summary>
        /// <param name="state">The new state.</param>
        private void SetDeviceStatus(CameraStatus status)
        {
            plugin.SetDeviceValue(refId, (double)status);
            hikAlarmStatus = status;
        }

        /// <summary>
        /// Log a message to the console for debugging purposes.
        /// </summary>
        private void LogMessage(string msg)
        {
            string timeStr = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            Console.WriteLine("{0} IpAddress: {1}, {2}", timeStr, ipAddressStr, msg);
        }
    }
}

