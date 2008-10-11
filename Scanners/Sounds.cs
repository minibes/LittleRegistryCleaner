﻿/*
    Little Registry Cleaner
    Copyright (C) 2008 Nick H.

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.Win32;

namespace Little_Registry_Cleaner.Scanners
{
    public class Sounds
    {
        private ScanDlg frmScanDlg;

        /// <summary>
        /// Scans for invalid windows sound events
        /// </summary>
        public Sounds(ScanDlg frm)
        {
            this.frmScanDlg = frm;

            RegistryKey regKey = Registry.CurrentUser.OpenSubKey("AppEvents\\Schemes\\Apps");
            ParseSoundKeys(regKey);
        }

        /// <summary>
        /// Goes deep into sub keys to see if files exist
        /// </summary>
        /// <param name="rk">Registry subkey</param>
        private void ParseSoundKeys(RegistryKey rk)
        {
            foreach (string strSubKey in rk.GetSubKeyNames())
            {
                try
                {
                    // Ignores ".Default" Subkey
                    if ((strSubKey.CompareTo(".Current") == 0) || (strSubKey.CompareTo(".Modified") == 0))
                    {
                        // Gets the (default) key and sees if the file exists
                        RegistryKey rk2 = rk.OpenSubKey(strSubKey);

                        if (rk2 != null)
                        {
                            frmScanDlg.UpdateScanSubKey(rk2.ToString());

                            string strSoundPath = (string)rk2.GetValue("");

                            if (!string.IsNullOrEmpty(strSoundPath))
                                if (!File.Exists(strSoundPath))
                                    ScanDlg.StoreInvalidKey("Invalid file or folder", rk2.Name, "(default)");
                        }

                    }
                    else if (!string.IsNullOrEmpty(strSubKey))
                    {
                        RegistryKey rk2 = rk.OpenSubKey(strSubKey);
                        if (rk2 != null)
                        {
                            frmScanDlg.UpdateScanSubKey(rk2.ToString());
                            ParseSoundKeys(rk2);
                        }
                    }
                }
                catch (System.Security.SecurityException ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }
            }

            return;
        }
    }
}