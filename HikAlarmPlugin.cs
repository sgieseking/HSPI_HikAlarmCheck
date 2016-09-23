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
using System.Collections.Generic;
using System.Collections.Specialized;
using HomeSeerAPI;
using HSCF.Communication.Scs.Communication.EndPoints.Tcp;
using HSCF.Communication.ScsServices.Client;
using HSPI_Template;

namespace HSPI_HikAlarmCheck
{
    /// <summary>
    /// HikAlarmCheck plugin for Homeseer.
    /// <para/>
    /// Initialization is performed in the InitIO() function.
    /// The plugin is shutdown in the ShutdownIO() function.
    /// Configuration of the plugin is through a single web page that appears in the HomeSeer plugin menu item.
    /// See functions GetPagePlugin and PostBackProc.
    /// <para/>
    /// An INI file is used to save the information on the camera parameters.
    /// The HomeSeer device reference id is used to specify a section name as Camera{refId}.
    /// The IpAddress, Username, and Password are stored under this section name.
    /// <para/>
    /// Trigger are not currently supported with this plugin.
    /// <para/>
    /// HomeSeer will look for this class at a specific location.  To work:
    /// <list type="number">
    /// <item><description>The namespace must be the same as the EXE filename (without extension).</description></item>
    /// <item><description>This class must be public, named HSPI, and be in the root of the namespace.</description></item>
    /// </list>
    /// </summary>
    /// <seealso cref="HomeSeerAPI.IPlugInAPI" />
    class HSPI : HspiBase
    {
    #region User variables
        /// <summary> Definition of the configuration web page </summary>
        HikAlarmConfig configPage;
    #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="HSPI"/> class.
        /// </summary>
        public HSPI()
        {
            // Can't do much here because this class gets loaded and then destroyed by Homeseer during initial discovery & reflection.
            // Instead wait to be initialized during the Connect and InitIO methods, called by the console wrapper and homeseer respectively.

            // set the name for this plugin
            IFACE_NAME = "HikAlarmCheck";
            INI_File = IFACE_NAME + ".ini";
        }

    #region Required Plugin Methods - Information & Initialization
        /// <summary>
        /// Initialize the plugin and associated hardware/software, start any threads
        /// </summary>
        /// <param name="port">The COM port for the plugin if required.</param>
        /// <returns>Warning message or empty for success.</returns>
        public override string InitIO(string port)
        {
            // initialize everything here, return a blank string only if successful, or an error message
            try
            {
                Scheduler.Classes.clsDeviceEnumeration EN = (Scheduler.Classes.clsDeviceEnumeration)hs.GetDeviceEnumerator();
                if (EN == null) throw new Exception(IFACE_NAME + " failed to get a device enumerator from HomeSeer.");
                do
                {
                    Scheduler.Classes.DeviceClass dv = EN.GetNext();
                    if ((dv != null) &&
                        (dv.get_Interface(hs) != null) &&
                        (dv.get_Interface(hs).Trim() == IFACE_NAME))
                    {
                        //Console.WriteLine("Found device: refId = {0}", dv.get_Ref(hs));
                        int refId = dv.get_Ref(hs);
                        string ipAddress = hs.GetINISetting("Camera" + refId, "IpAddress", "", INI_File);
                        string username = hs.GetINISetting("Camera" + refId, "Username", "", INI_File);
                        string password = hs.GetINISetting("Camera" + refId, "Password", "", INI_File);
                        Console.WriteLine("Camera: RefId: {0}, IpAddress: {1}, Username: {2}, Password: {3}", refId, ipAddress, username, password);

                        HikAlarmThreadManager.AddDevice(this, refId, ipAddress, username, password);
                    }
                } while (!EN.Finished);
            }
            catch (Exception ex)
            {
                hs.WriteLog(IFACE_NAME + " Error", "Exception in Find_Create_Devices/Enumerator: " + ex.Message);
            }

            // Create and register the web pages - only need one for configuration
            configPage = new HikAlarmConfig(this);
            RegisterWebPage(configPage.Name);

            return "";
        }

        /// <summary>
        /// If a device is owned by your plug-in (interface property set to the name of the plug-in) and the device's status_support property is set to True, 
        /// then this procedure will be called in your plug-in when the device's status is being polled, such as when the user clicks "Poll Devices" on the device status page.
        /// Normally your plugin will automatically keep the status of its devices updated.
        /// There may be situations where automatically updating devices is not possible or CPU intensive.
        /// In these cases the plug-in may not keep the devices updated. HomeSeer may then call this function to force an update of a specific device.
        /// This request is normally done when a user displays the status page, or a script runs and needs to be sure it has the latest status.
        /// </summary>
        /// <param name="dvref">Reference Id for the device</param>
        /// <returns>IPlugInAPI.PollResultInfo</returns>
        public override IPlugInAPI.PollResultInfo PollDevice(int dvref)
        {
            double status = -1;
            IPlugInAPI.PollResultInfo pollResult = new IPlugInAPI.PollResultInfo();

            if (HikAlarmThreadManager.GetDeviceStatus(dvref, out status))
            {
                pollResult.Result = IPlugInAPI.enumPollResult.OK;
                pollResult.Value = status;
            }
            else
            {
                pollResult.Result = IPlugInAPI.enumPollResult.Device_Not_Found;
                pollResult.Value = 0;
            }

            return pollResult;
        }

        /// <summary>
        /// Called when HomeSeer is not longer using the plugin. 
        /// This call will be made if a user disables a plugin from the interfaces configuration page and when HomeSeer is shut down.
        /// </summary>
        public override void ShutdownIO()
        {
            // debug
            hs.WriteLog(IFACE_NAME, "Entering ShutdownIO");

            // shut everything down here
            HikAlarmThreadManager.Shutdown();

            // perform any base class shutdown
            base.ShutdownIO();

            // debug
            hs.WriteLog(IFACE_NAME, "Completed ShutdownIO");
        }
    #endregion

    #region Required Plugin Methods - Actions, Triggers & Conditions
    #endregion

    #region Required Plugin Methods - Web Interface
        /// <summary>
        /// A complete page needs to be created and returned.
        /// Web pages that use the clsPageBuilder class and registered with hs.RegisterLink and hs.RegisterConfigLink will then be called through this function. 
        /// </summary>
        /// <param name="page">The name of the page as passed to the hs.RegisterLink function.</param>
        /// <param name="user">The name of logged in user.</param>
        /// <param name="userRights">The rights of the logged in user.</param>
        /// <param name="queryString">The query string.</param>
        public override string GetPagePlugin(string page, string user, int userRights, string queryString)
        {
            if (page == configPage.Name)
                return configPage.GetWebPage(user, userRights, queryString);

            return "";
        }

        /// <summary>
        /// When a user clicks on any controls on one of your web pages, this function is then called with the post data. You can then parse the data and process as needed.
        /// </summary>
        /// <param name="page">The name of the page as passed to the hs.RegisterLink function.</param>
        /// <param name="data">The post data.</param>
        /// <param name="user">The name of logged in user.</param>
        /// <param name="userRights">The rights of the logged in user.</param>
        /// <returns>Any serialized data that needs to be passed back to the web page, generated by the clsPageBuilder class.</returns>
        public override string PostBackProc(string page, string data, string user, int userRights)
        {
            if (page == configPage.Name)
                return configPage.PostBackProc(data, user, userRights);

            return "";
        }

    #endregion

    #region Required Plugin Methods - User defined functions
    #endregion

    #region User functions
        /// <summary>
        /// Gets the Homeseer application interface.
        /// </summary>
        public HomeSeerAPI.IHSApplication HsApplication
        {
            get { return hs; }
        }

        /// <summary>
        /// Create a new device and set the names for the status display.
        /// </summary>
        /// <param name="refId">The device reference identifier for HomeSeer.</param>
        /// <param name="name">The name for the device.</param>
        /// <returns>Scheduler.Classes.DeviceClass.</returns>
        private Scheduler.Classes.DeviceClass CreateDevice(out int refId, string name = "HikVision Camera")
        {
            Scheduler.Classes.DeviceClass dv = null;
            refId = hs.NewDeviceRef(name);
            if (refId > 0)
            {
                dv = (Scheduler.Classes.DeviceClass)hs.GetDeviceByRef(refId);
                dv.set_Address(hs, "Camera" + refId);
                dv.set_Device_Type_String(hs, "HikVision Camera Alarm");
				DeviceTypeInfo_m.DeviceTypeInfo DT = new DeviceTypeInfo_m.DeviceTypeInfo();
				DT.Device_API = DeviceTypeInfo_m.DeviceTypeInfo.eDeviceAPI.Security;
				DT.Device_Type = (int)DeviceTypeInfo_m.DeviceTypeInfo.eDeviceType_Security.Zone_Interior;
                dv.set_DeviceType_Set(hs, DT);
                dv.set_Interface(hs, IFACE_NAME);
                dv.set_InterfaceInstance(hs, "");
                dv.set_Last_Change(hs, DateTime.Now);
                dv.set_Location(hs, "Camera"); // room
                dv.set_Location2(hs, "HikVision"); // floor

				VSVGPairs.VSPair Pair = new VSVGPairs.VSPair(HomeSeerAPI.ePairStatusControl.Status);
				Pair.PairType = VSVGPairs.VSVGPairType.SingleValue;
                Pair.Value = -1;
                Pair.Status = "Unknown";
                Default_VS_Pairs_AddUpdateUtil(refId, Pair);

				Pair = new VSVGPairs.VSPair(HomeSeerAPI.ePairStatusControl.Status);
				Pair.PairType = VSVGPairs.VSVGPairType.SingleValue;
                Pair.Value = 0;
                Pair.Status = "No Motion";
                Default_VS_Pairs_AddUpdateUtil(refId, Pair);

				Pair = new VSVGPairs.VSPair(HomeSeerAPI.ePairStatusControl.Status);
				Pair.PairType = VSVGPairs.VSVGPairType.SingleValue;
                Pair.Value = 1;
                Pair.Status = "Motion";
                Default_VS_Pairs_AddUpdateUtil(refId, Pair);

                dv.MISC_Set(hs, Enums.dvMISC.STATUS_ONLY);
                dv.MISC_Set(hs, Enums.dvMISC.SHOW_VALUES);
                dv.set_Status_Support(hs, true);
            }

            return dv;
        }

        /// <summary>
        /// Add the protected, default VS/VG pairs WITHOUT overwriting any user added pairs unless absolutely necessary (because they conflict).
        /// </summary>
        /// <param name="refId">The device reference identifier for HomeSeer.</param>
        /// <param name="Pair">The value/status pair.</param>
		private void Default_VS_Pairs_AddUpdateUtil(int refId, VSVGPairs.VSPair Pair)
        {
            if ((Pair == null) || (refId < 1) || (!hs.DeviceExistsRef(refId)))
                return;

            try
            {
				VSVGPairs.VSPair Existing = hs.DeviceVSP_Get(refId, Pair.Value, Pair.ControlStatus);
                if (Existing != null)
                {
                    // This is unprotected, so it is a user's value/ status pair.
                    if ((Existing.ControlStatus == HomeSeerAPI.ePairStatusControl.Both) && (Pair.ControlStatus != HomeSeerAPI.ePairStatusControl.Both))
                    {
                        // The existing one is for BOTH, so try changing it to the opposite of what we are adding and then add it.
                        if (Pair.ControlStatus == HomeSeerAPI.ePairStatusControl.Status)
                        {
                            if (!hs.DeviceVSP_ChangePair(refId, Existing, HomeSeerAPI.ePairStatusControl.Control))
                            {
                                hs.DeviceVSP_ClearBoth(refId, Pair.Value);
                                hs.DeviceVSP_AddPair(refId, Pair);
                            }
                            else
                                hs.DeviceVSP_AddPair(refId, Pair);
                        }
                        else
                        {
                            if (!hs.DeviceVSP_ChangePair(refId, Existing, HomeSeerAPI.ePairStatusControl.Status))
                            {
                                hs.DeviceVSP_ClearBoth(refId, Pair.Value);
                                hs.DeviceVSP_AddPair(refId, Pair);
                            }
                            else
                                hs.DeviceVSP_AddPair(refId, Pair);
                        }
                    }
                    else if (Existing.ControlStatus == HomeSeerAPI.ePairStatusControl.Control)
                    {
                        // There is an existing one that is STATUS or CONTROL - remove it if ours is protected.
                        hs.DeviceVSP_ClearControl(refId, Pair.Value);
                        hs.DeviceVSP_AddPair(refId, Pair);
                    }
                    else if (Existing.ControlStatus == HomeSeerAPI.ePairStatusControl.Status)
                    {
                        // There is an existing one that is STATUS or CONTROL - remove it if ours is protected.
                        hs.DeviceVSP_ClearStatus(refId, Pair.Value);
                        hs.DeviceVSP_AddPair(refId, Pair);
                    }
                }
                else
                {
                    // There is not a pair existing, so just add it.
                    hs.DeviceVSP_AddPair(refId, Pair);
                }
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// Registers a web page with HomeSeer.
        /// </summary>
        /// <param name="link">The link to be registered.</param>
        /// <param name="linktext">The text to appear in the HomeSeer menu system for the link.</param>
        /// <param name="page_title">The title to be displayed for the web page.</param>
        public void RegisterWebPage(string link, string linktext = "", string page_title = "")
        {
            try
            {
                hs.RegisterPage(link, IFACE_NAME, "");

                if (linktext == "")
                    linktext = link;
                linktext = linktext.Replace("_", " ");

                if (page_title == "")
                    page_title = linktext;

                HomeSeerAPI.WebPageDesc wpd = new HomeSeerAPI.WebPageDesc();
                wpd.plugInName = IFACE_NAME;
                wpd.link = link;
                wpd.linktext = linktext;
                wpd.page_title = page_title;
                callback.RegisterConfigLink(wpd);
                callback.RegisterLink(wpd);
            }
            catch (Exception ex)
            {
                hs.WriteLog(IFACE_NAME, "Error registering web links: " + ex.Message);
            }
        }

        /// <summary>
        /// Name of the HomeSeer device.
        /// </summary>
        /// <param name="refId">The device reference identifier for HomeSeer.</param>
        public string DeviceName(int refId)
        {
            string name = "RefId_" + refId.ToString();
            Scheduler.Classes.DeviceClass hsDevice = (Scheduler.Classes.DeviceClass)hs.GetDeviceByRef(refId);
            if (hsDevice != null)
                name = hsDevice.get_Name(hs);
            return name;
        }

        /// <summary>
        /// Creates a device from the configuration information.  Save the information in the INI file.
        /// </summary>
        /// <param name="name">The name for the device.</param>
        /// <param name="ipAddress">The ip address for the device.</param>
        /// <param name="username">The username for the device.</param>
        /// <param name="password">The password for the device.</param>
        /// <returns>The device reference identifier from HomeSeer.</returns>
        public int CreateDeviceFromConfig(string name, string ipAddress, string username, string password)
        {
            Console.WriteLine("Name:      {0}", name);
            Console.WriteLine("IpAddress: {0}", ipAddress);
            Console.WriteLine("Username:  {0}", username);
            Console.WriteLine("Password:  {0}", password);

            // create the device in HomeSeer
            int refId;
            Scheduler.Classes.DeviceClass hsDevice = CreateDevice(out refId, name);
            if (hsDevice == null)
                return -1;

            // save the settings
            hs.SaveINISetting("Camera" + refId, "IpAddress", ipAddress, INI_File);
            hs.SaveINISetting("Camera" + refId, "Username", username, INI_File);
            hs.SaveINISetting("Camera" + refId, "Password", password, INI_File);

            // start a new thread
            HikAlarmThreadManager.AddDevice(this, refId, ipAddress, username, password);

            return refId;
        }

        /// <summary>
        /// Gets the device parameters from the INI file based on the Homeseer reference id.
        /// </summary>
        /// <param name="refIdStr">The device reference identifier (as string) for HomeSeer.</param>
        /// <param name="ipAddress">The ip address.</param>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <returns><c>true</c> if device was found in the INI file, <c>false</c> otherwise.</returns>
        public bool GetDeviceParameters(string refIdStr, ref string ipAddress, ref string username, ref string password)
        {
            ipAddress = hs.GetINISetting("Camera" + refIdStr, "IpAddress", "", INI_File);
            username = hs.GetINISetting("Camera" + refIdStr, "Username", "", INI_File);
            password = hs.GetINISetting("Camera" + refIdStr, "Password", "", INI_File);
            return (ipAddress != "");
        }

        /// <summary>
        /// Updates the device parameters in the INI file.  It is necessary to restart the thread for the new values to take effect.
        /// </summary>
        /// <param name="refIdStr">The device reference identifier (as string) for HomeSeer.</param>
        /// <param name="ipAddress">The ip address.</param>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <returns><c>true</c> if the update was successful, <c>false</c> otherwise.</returns>
        public bool UpdateDevice(string refIdStr, string ipAddress, string username, string password)
        {
            int refId;
            if (!Int32.TryParse(refIdStr, out refId))
                return false;

            Scheduler.Classes.DeviceClass hsDevice = (Scheduler.Classes.DeviceClass)hs.GetDeviceByRef(refId);
            if (hsDevice == null)
                return false;

            // save the settings
            hs.SaveINISetting("Camera" + refIdStr, "IpAddress", ipAddress, INI_File);
            hs.SaveINISetting("Camera" + refIdStr, "Username", username, INI_File);
            if (password != "")
                hs.SaveINISetting("Camera" + refIdStr, "Password", password, INI_File);

            HikAlarmThreadManager.DeleteDevice(refId);
            HikAlarmThreadManager.AddDevice(this, refId, ipAddress, username, password);

            return true;
        }

        /// <summary>
        /// Delete the device in HomeSeer.  Clear the set INI settings.  Delete the thread.
        /// </summary>
        /// <param name="refIdStr">The device reference identifier (as string) for HomeSeer.</param>
        /// <returns><c>true</c> if device was found and deleted, <c>false</c> otherwise.</returns>
        public bool DeleteDevice(string refIdStr)
        {
            int refId;
            bool success = true;

            // delete the device from HomeSeer
            if (!Int32.TryParse(refIdStr, out refId))
                success = false;
            else
            {
                Scheduler.Classes.DeviceClass hsDevice = (Scheduler.Classes.DeviceClass)hs.GetDeviceByRef(refId);
                if ((hsDevice == null) || (!hs.DeleteDevice(refId)))
                    success = false;
            }

            // delete the info from the INI file
            hs.ClearINISection("Camera" + refIdStr, INI_File);

            // delete the thread
            HikAlarmThreadManager.DeleteDevice(refId);

            return success;
        }
    #endregion
    }
}