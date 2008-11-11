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
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace Little_Registry_Cleaner.Optimizer
{
    public partial class Optimizer : Form
    {
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool ExitWindowsEx(uint uFlags, uint dwReason);

        // Shutdown reason codes
        const uint MajorOperatingSystem = 0x00020000;
        const uint MinorReconfig = 0x00000004;
        const uint FlagPlanned = 0x80000000;

        private delegate void AddToListViewDelegate(Hive oHive);
        private delegate void SetButtonsEnabledDelegate(bool bValue);

        public static HiveCollection arrHives = new HiveCollection();
        private static Thread tAnalyzeHives = null;
        private bool bAllowFormClosed = true;

        XpProgressBar progressBarAnalyzed = new XpProgressBar();
        XpProgressBar progressBarDefrag = new XpProgressBar();

        public Optimizer()
        {
            InitializeComponent();

            // Add custom progress bars using default properties
            this.progressBarAnalyzed.Name = "progressBarAnalyzed";
            this.progressBarAnalyzed.Location = new Point(13, 27);
            this.progressBarAnalyzed.Size = new Size(326, 30);

            this.progressBarDefrag.Name = "progressBarDefrag";
            this.progressBarDefrag.Location = new Point(13, 63);
            this.progressBarDefrag.Size = new Size(326, 30);

            this.Controls.Add(this.progressBarAnalyzed);
            this.Controls.Add(this.progressBarDefrag);
        }

        public static string GetSizeInMegaBytes(long Length)
        {
            double nMegaBytes = Length / 1024F / 1024F;

            return string.Format("{0} MB", nMegaBytes.ToString("0.00"));
        }

        private void Optimizer_Shown(object sender, EventArgs e)
        {
            if (MessageBox.Show(this, "The program will now analyze your registry files. Continue?", Application.ProductName, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                SecureDesktop frm = new SecureDesktop();
                this.Owner = frm;
                this.FormBorderStyle = FormBorderStyle.None;
                this.Left = (Screen.PrimaryScreen.Bounds.Width / 2 - this.Width / 2);
                this.Top = (Screen.PrimaryScreen.Bounds.Height / 2 - this.Height / 2);

                frm.Show();

                RegistryKey rkHives = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\hivelist");

                if (rkHives == null)
                    return;

                string[] strHives = rkHives.GetValueNames();

                foreach (string strValueName in strHives)
                {
                    string strHivePath = rkHives.GetValue(strValueName) as string;
                    if (!string.IsNullOrEmpty(strValueName) && !string.IsNullOrEmpty(strHivePath))
                    {
                        Hive oHive = new Hive(strValueName, strHivePath);
                        arrHives.Add(oHive);
                    }
                }

                rkHives.Close();

                this.progressBarAnalyzed.PositionMin = 0;
                this.progressBarAnalyzed.PositionMax = arrHives.Count;
                this.progressBarAnalyzed.Text = string.Format("{0:P}", (double)(0 / arrHives.Count));

                SetButtonsEnabled(false);

                Optimizer.tAnalyzeHives = new Thread(new ThreadStart(AnalyzeHives));
                Optimizer.tAnalyzeHives.Start();
            }
            else
                this.Close();
        }

        private void AnalyzeHives()
        {
            if (arrHives.Count > 0)
            {
                foreach (Hive oHive in arrHives)
                {
                    oHive.AnalyzeHive();
                    AddToListView(oHive);
                }

                SetButtonsEnabled(true); 
            }
        }

        private void SetButtonsEnabled(bool bValue)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new SetButtonsEnabledDelegate(SetButtonsEnabled), new object[] { bValue });
                return;
            }

            this.buttonStart.Enabled = bValue;
            this.buttonClose.Enabled = bValue;
            this.bAllowFormClosed = bValue;
        }

        private void AddToListView(Hive oHive)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new AddToListViewDelegate(AddToListView), new object[] { oHive });
                return;
            }

            this.listView1.Items.Add(new ListViewItem(new string[] { oHive.fiHive.Name, GetSizeInMegaBytes(oHive.fiHive.Length), GetSizeInMegaBytes(oHive.fiHiveTemp.Length) }));
            this.progressBarAnalyzed.Position++;
            this.progressBarAnalyzed.Text = string.Format("{0}/{1}", this.progressBarAnalyzed.Position, arrHives.Count);
        }

        private void Optimizer_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!this.bAllowFormClosed)
            {
                e.Cancel = true;
                return;
            }

            if (Optimizer.tAnalyzeHives != null)
                if (Optimizer.tAnalyzeHives.IsAlive)
                    Optimizer.tAnalyzeHives.Abort();

            if (this.Owner != null)
            {
                this.Owner.Close();
                this.Owner = null;
            }
        }

        private void buttonStart_Click(object sender, EventArgs e)
        {
            long lSeqNum = 0;

            SysRestore.StartRestore("Before Little Registry Cleaner Optimization", out lSeqNum);

            SetButtonsEnabled(false);

            this.progressBarDefrag.PositionMin = 0;
            this.progressBarDefrag.PositionMax = arrHives.Count;
            this.labelAction.Text = "Optimizing the registry, Please Wait...";

            foreach (Hive oHive in arrHives)
            {
                oHive.CompactHive();
                this.progressBarDefrag.Position++;
            }

            SetButtonsEnabled(true);

            SysRestore.EndRestore(lSeqNum);

            if (MessageBox.Show(this, "You must restart your computer before the new setting will take effect. Do you want to restart your computer now?", Application.ProductName, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                ExitWindowsEx(0x02, MajorOperatingSystem | MinorReconfig | FlagPlanned);

            this.Close();
        }
    }
}