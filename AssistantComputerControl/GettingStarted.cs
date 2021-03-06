﻿/*
 * AssistantComputerControl
 * Made by Albert MN.
 * Updated: v1.1.3, 15-11-2018
 * 
 * Use:
 * - The 'Getting Started' setup guide
 */

using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AssistantComputerControl {
    [ComVisible(true)]
    public partial class GettingStarted : Form {

        public static WebBrowser theWebBrowser = null;
        private static TabControl theTabControl;

        [ComVisible(true)]
        public class WebBrowserHandler {
            private static string backgroundCheckerServiceName;
            public static bool stopCheck = false;
            private static string customSetPath = String.Empty;

            private bool CheckSetPath(string chosenService) {
                if (customSetPath != String.Empty) {
                    if (Directory.Exists(customSetPath)) {
                        if (!customSetPath.Contains("AssistantComputerControl") && !customSetPath.Contains("assistantcomputercontrol")) {
                            customSetPath = Path.Combine(customSetPath, "AssistantComputerControl");
                            MainProgram.DoDebug("Changed path to include 'AssistantComputerControl': " + customSetPath);

                            if (!Directory.Exists(customSetPath))
                                Directory.CreateDirectory(customSetPath);
                        }

                        Properties.Settings.Default.ActionFilePath = customSetPath;
                        Properties.Settings.Default.Save();

                        MainProgram.SetupListener();
                        return true;
                    }
                } else {
                    string checkPath = MainProgram.GetCloudServicePath(chosenService);
                    MainProgram.DoDebug("Checking: " + checkPath);
                    if (!String.IsNullOrEmpty(checkPath)) {
                        if (Directory.Exists(checkPath)) {
                            if (!checkPath.Contains("AssistantComputerControl") && !checkPath.Contains("assistantcomputercontrol")) {
                                checkPath = Path.Combine(checkPath, "AssistantComputerControl");
                                MainProgram.DoDebug("Changed path to include 'AssistantComputerControl': " + checkPath);

                                if (!Directory.Exists(checkPath))
                                    Directory.CreateDirectory(checkPath);
                            }

                            Properties.Settings.Default.ActionFilePath = checkPath;
                            Properties.Settings.Default.Save();

                            MainProgram.SetupListener();
                            return true;
                        }
                    }
                }
                return false;
            }

            public void SetPath(string chosenService) {
                if (!CheckSetPath(chosenService)) {
                    theWebBrowser.Document.InvokeScript("DoneError");
                }
            }
            
            public void AllDone(string chosenService) {
                MainProgram.DoDebug("'AllDone' pressed");
                if (CheckSetPath(chosenService)) {
                    theTabControl.SelectTab(2);

                    MainProgram.SetRegKey("ActionFolder", MainProgram.CheckPath());
                } else {
                    theWebBrowser.Document.InvokeScript("DoneError");
                }
            }

            public void ClearCustomSetPath() {
                customSetPath = String.Empty;
            }
            
            public void ExpertChosen() {
                theTabControl.SelectTab(1);
            }

            public void StopCheck() {
                stopCheck = true;
            }

            public void CheckManualPath(string path) {
                MainProgram.DoDebug("Checking manually-entered path...");
                try {
                    Path.GetFullPath(path);
                } catch {
                    MainProgram.DoDebug("Path not good");
                    theWebBrowser.Document.InvokeScript("ManualPathValidated", new Object[1] { false });
                    return;
                }
                if (Directory.Exists(path)) {
                    //Good
                    theWebBrowser.Document.InvokeScript("ManualPathValidated", new Object[1] { true });
                    MainProgram.DoDebug("Path good");
                    customSetPath = path;
                } else {
                    theWebBrowser.Document.InvokeScript("ManualPathValidated", new Object[1] { false });
                    MainProgram.DoDebug("Path not good");
                }
            }

            public void SkipGuide() {
                MainProgram.gettingStarted.SetupDone();
                MainProgram.DoDebug("Skipped setup guide");
                MainProgram.gettingStarted.Close();
            }

            public void CloudServiceChosen(string service = "") {
                switch (service) {
                    case "dropbox":
                    case "onedrive":
                    case "googledrive":
                        backgroundCheckerServiceName = service;
                        break;
                    default:
                        return;
                }

                if (MainProgram.GetCloudServicePath(backgroundCheckerServiceName) != "") {
                    //Cloud service found
                    MainProgram.DoDebug("Cloud service " + backgroundCheckerServiceName + " is installed");
                    if (backgroundCheckerServiceName == "googledrive") {
                        bool partial = MainProgram.GetGoogleDriveFolder() != String.Empty;
                        if (theWebBrowser != null)
                            if (theWebBrowser.Handle != null)
                                theWebBrowser.Document.InvokeScript("CloudServiceInstalled", new Object[2] { true, partial });
                        if (partial)
                            CheckLocalGoogleDrive();
                    } else {
                        if (theWebBrowser != null)
                            if (theWebBrowser.Handle != null)
                                theWebBrowser.Document.InvokeScript("CloudServiceInstalled", new Object[1] { true });
                    }
                } else {
                    //Not found
                    new Thread(() => {
                        Thread.CurrentThread.IsBackground = true;
                        string checkValue = "";
                        stopCheck = false;

                        MainProgram.DoDebug("Could not find cloud service. Running loop to check");
                        while (checkValue == "" && !stopCheck) {
                            checkValue = MainProgram.GetCloudServicePath(backgroundCheckerServiceName);
                            Thread.Sleep(1000);
                        }
                        if (stopCheck) {
                            stopCheck = false;
                            return;
                        }
                        
                        //Cloud service has been installed since we last checked!
                        MainProgram.DoDebug("Cloud service has been installed since last check. Proceed.");

                        if (theWebBrowser != null) {
                            if (theWebBrowser.Handle != null) {
                                theWebBrowser.Invoke(new Action(() => {
                                    if (backgroundCheckerServiceName == "googledrive") {
                                        bool partial = MainProgram.GetGoogleDriveFolder() != String.Empty;
                                        theWebBrowser.Document.InvokeScript("CloudServiceInstalled", new Object[2] { true, partial });
                                        if (partial)
                                            CheckLocalGoogleDrive();
                                    } else {
                                        theWebBrowser.Document.InvokeScript("CloudServiceInstalled", new Object[1] { true });
                                    }
                                }));
                            }
                        }
                    }).Start();
                }
            }

            private void CheckLocalGoogleDrive() {
                new Thread(() => {
                    Thread.CurrentThread.IsBackground = true;
                    stopCheck = false;

                    MainProgram.DoDebug("Starting loop to check for Google Drive folder locally");
                    while (backgroundCheckerServiceName == "googledrive" && MainProgram.GetGoogleDriveFolder() == String.Empty && !stopCheck) {
                        Thread.Sleep(1000);
                    }
                    if (stopCheck) {
                        stopCheck = false;
                        return;
                    }

                    if (backgroundCheckerServiceName == "googledrive") {
                        //Cloud service has been installed since we last checked!
                        MainProgram.DoDebug("Google Drive has been added to local PC. Proceed.");
                        theWebBrowser.Invoke(new Action(() => {
                            theWebBrowser.Document.InvokeScript("CloudServiceInstalled", new Object[2] { true, false });
                        }));
                    } else {
                        MainProgram.DoDebug("Service has since been changed. Stopping the search for Google Drive.");
                    }
                }).Start();
            }
        }

        public GettingStarted(int startTab = 0) {
            InitializeComponent();
            Thread.CurrentThread.Priority = ThreadPriority.Highest;

            theTabControl = tabControl;

            FormClosed += delegate {
                if (MainProgram.aboutVersionAwaiting) {
                    Properties.Settings.Default.LastKnownVersion = MainProgram.softwareVersion;
                    new NewVersion().Show();
                    Properties.Settings.Default.Save();
                }
            };

            tabControl.Appearance = TabAppearance.FlatButtons;
            tabControl.ItemSize = new Size(0, 1);
            tabControl.SizeMode = TabSizeMode.Fixed;
            tabControl.BackColor = Color.Transparent;
            tabControl.SelectTab(startTab);

            tabControl.Selected += delegate {
                if (tabControl.SelectedIndex == 1) {
                    //Clicked on recommended setup guide (HTML), can show "move on" popover now
                    //theWebBrowser.Document.InvokeScript("showHelpPopover"); // Why would I do this...?
                } else if (tabControl.SelectedIndex == 2) {
                    expert.Focus();
                }
            };

            //Set GettingStarted web-browser things
            string fileName = Path.Combine(MainProgram.currentLocation, "WebFiles/GettingStarted.html");
            if (File.Exists(fileName)) {
                string fileLoc = "file:///" + fileName;
                Uri theUri = new Uri(fileLoc);
                GettingStartedWebBrowser.Url = theUri;
            } else {
                GettingStartedWebBrowser.Visible = false;
            }
            GettingStartedWebBrowser.ObjectForScripting = new WebBrowserHandler();

            theWebBrowser = GettingStartedWebBrowser;

            theWebBrowser.DocumentCompleted += BrowserDocumentCompleted;
            theWebBrowser.Navigating += BrowserNavigating;
            theWebBrowser.NewWindow += NewBrowserWindow;



            //Further expert settings
            actionFolderPath.KeyDown += new KeyEventHandler(FreakingStopDingSound);
            actionFileExtension.KeyDown += new KeyEventHandler(FreakingStopDingSound);

            void FreakingStopDingSound(Object o, KeyEventArgs e) {
                if (e.KeyCode == Keys.Enter) {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            }

            actionFolderPath.Text = MainProgram.CheckPath();
            actionFileExtension.Text = Properties.Settings.Default.ActionFileExtension;

            actionFolderPath.KeyDown += delegate {
                MainProgram.SetCheckFolder(actionFolderPath.Text);
                actionFolderPath.Text = MainProgram.CheckPath();
            };
            actionFolderPath.KeyUp += delegate {
                MainProgram.SetCheckFolder(actionFolderPath.Text);
                actionFolderPath.Text = MainProgram.CheckPath();
            };

            actionFileExtension.KeyDown += delegate { MainProgram.SetCheckExtension(actionFileExtension.Text); };
            actionFileExtension.KeyUp += delegate { MainProgram.SetCheckExtension(actionFileExtension.Text); };

            expert.Click += delegate {
                expert.Focus();
            };
            customSetupInfo.Click += delegate {
                expert.Focus();
            };

            expertDoneButton.FlatStyle = FlatStyle.Flat;
            expertDoneButton.FlatAppearance.BorderSize = 0;

            closeWindowButton.FlatStyle = FlatStyle.Flat;
            closeWindowButton.FlatAppearance.BorderSize = 0;

            analyticsMoveOn.FlatStyle = FlatStyle.Flat;
            analyticsMoveOn.FlatAppearance.BorderSize = 0;

            VisibleChanged += delegate {
                MainProgram.testingAction = Visible;
                MainProgram.gettingStarted = Visible ? this : null;
            };
            FormClosed += delegate {
                MainProgram.testingAction = false;
                MainProgram.gettingStarted = null;
            };


            this.HandleCreated += delegate {
                Invoke(new Action(() => {
                    FlashWindow.Flash(this);
                    if (Application.OpenForms[this.Name] != null) {
                        Application.OpenForms[this.Name].Activate();
                        Application.OpenForms[this.Name].Focus();
                    }
                }));
            };
        }

        public void SendActionThrough(Object[] objArray) {
            if ((string)objArray[0] == "success") {
                SetupDone();
            }
            this.Invoke(new Action(() => {
                FlashWindow.Flash(this);
                if (Application.OpenForms[this.Name] != null) {
                    Application.OpenForms[this.Name].Activate();
                    Application.OpenForms[this.Name].Focus();
                }
            }));

            theWebBrowser.Invoke(new Action(() => {
                theWebBrowser.Document.InvokeScript("actionWentThrough", objArray);
            }));
        }

        private void BrowserNavigating(object sender, WebBrowserNavigatingEventArgs e) {
            if (!(e.Url.ToString().Equals("about:blank", StringComparison.InvariantCultureIgnoreCase))) {
                Process.Start(e.Url.ToString());
                e.Cancel = true;
            }

            e.Cancel = true;
            Process.Start(e.Url.ToString());
        }

        private void BrowserDocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e) {
            string tagUpper = "";

            foreach (HtmlElement tag in (sender as WebBrowser).Document.All) {
                tagUpper = tag.TagName.ToUpper();

                if ((tagUpper == "AREA") || (tagUpper == "A")) {
                    tag.MouseUp += new HtmlElementEventHandler(this.link_MouseUp);
                }
            }
        }
        void link_MouseUp(object sender, HtmlElementEventArgs e) {
            Regex pattern = new Regex("href=\\\"(.+?)\\\"");
            Match match = pattern.Match((sender as HtmlElement).OuterHtml);
            if (match.Groups.Count >= 1) {
                string link = match.Groups[1].Value;

                if (link.Length > 0) {
                    if (link[0] != '#')
                        Process.Start(link);
                    /*else if (link == "#recommendedFinished") {
                        tabControl.SelectTab(2);
                    }*/
                }
            }
        }
        private void NewBrowserWindow(object sender, CancelEventArgs e) {
            e.Cancel = true;
        }


        private void SetupDone() {
            //Start with Windows if user said so
            if (Properties.Settings.Default.StartWithWindows != startWithWindowsCheckbox.Checked) {
                Properties.Settings.Default.StartWithWindows = startWithWindowsCheckbox.Checked;
                MainProgram.SetStartup(startWithWindowsCheckbox.Checked);

                Properties.Settings.Default.Save();

                MainProgram.DoDebug("Starting with Windows now");
            }

            Properties.Settings.Default.AnalyticsInformed = true;
            if (Properties.Settings.Default.SendAnonymousAnalytics != analyticsEnabledBox.Checked) {
                MainProgram.UpdateAnalyticsSharing(analyticsEnabledBox.Checked);
            }

            MainProgram.DoDebug("Anonymous analyitcs " + (analyticsEnabledBox.Checked ? "IS" : "is NOT") + " enabled");

            MainProgram.DoDebug("Completed setup guide");
            Properties.Settings.Default.HasCompletedTutorial = true;
            Properties.Settings.Default.Save();
        }

        private void pickFolderBtn_Click(object sender, EventArgs e) {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog() {
                InitialDirectory = MainProgram.CheckPath(),
                IsFolderPicker = true
            };
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok) {
                MainProgram.SetCheckFolder(dialog.FileName);
                actionFolderPath.Text = MainProgram.CheckPath();
            }
        }

        private void expertDoneButton_Click(object sender, EventArgs e) {
            tabControl.SelectTab(2);
        }

        private void closeWindowButton_Click(object sender, EventArgs e) {
            SetupDone();
            Close();
        }

        private void iftttActions_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) {
            Process.Start("https://github.com/AlbertMN/AssistantComputerControl#supported-computer-actions");
        }

        private void skipGuide_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) {
            SetupDone();
            MainProgram.DoDebug("Skipped setup guide");
            Close();
        }

        private void gotoGoogleDriveGuide_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) {
            Process.Start("https://acc.readme.io/docs/use-google-drive-ifttt-instead-of-dropbox");
        }

        private void analyticsLearnMore_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) {
            Process.Start("https://acc.readme.io/v1.1.0/docs/how-analytics-work");
        }

        private void analyticsMoveOn_Click(object sender, EventArgs e) {
            tabControl.SelectTab(3);
        }

        private void analyticsEnabledBox_CheckedChanged(object sender, EventArgs e) {
            Properties.Settings.Default.AnalyticsInformed = true;
            Properties.Settings.Default.Save();
            MainProgram.UpdateAnalyticsSharing(analyticsEnabledBox.Checked);
        }
    }
}