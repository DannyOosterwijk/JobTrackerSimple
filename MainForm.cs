using IWshRuntimeLibrary;
using Microsoft.Win32;
using SimpleJobTracker.Models;
using SimpleJobTracker.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace SimpleJobTracker
{
    public partial class MainForm : Form
    {
        //External DLL's
        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(
            int nLeftRect,     // x-coordinate of upper-left corner
            int nTopRect,      // y-coordinate of upper-left corner
            int nRightRect,    // x-coordinate of lower-right corner
            int nBottomRect,   // y-coordinate of lower-right corner
            int nWidthEllipse, // width of ellipse
            int nHeightEllipse // height of ellipse
        );

#if DEBUG
        private bool inDebug = true;
#else
        private bool inDebug = false;
#endif

        //Web scrapping
        private JavaScraper javaScraper = new JavaScraper();
        private ProfileManager profileManager = new ProfileManager();
        private List<Control> profileControls = new List<Control>();
        private Emailer emailer = new Emailer();

        //Navigating & moving
        private int scrollLocation = 0;
        private float scrollSpeed = 0.2f;
        private Point startPoint = Point.Empty;

        private Control selectedProfile;
        private Color selectedColor = System.Drawing.Color.FromArgb(((int)(((byte)(97)))), ((int)(((byte)(45)))), ((int)(((byte)(83)))));
        private Color defaultColor = System.Drawing.Color.FromArgb(((int)(((byte)(44)))), ((int)(((byte)(44)))), ((int)(((byte)(44)))));
        private Color buttonColor = System.Drawing.Color.FromArgb(((int)(((byte)(133)))), ((int)(((byte)(57)))), ((int)(((byte)(83)))));

        //Notification tray
        private NotifyIcon notifyIcon;
        private int bubbleDuration = 5;

        public MainForm()
        {
            InitializeComponent();
            
            this.FormBorderStyle = FormBorderStyle.None;
            Region = System.Drawing.Region.FromHrgn(CreateRoundRectRgn(0, 0, Width, Height, 20, 20));

            CompanyDetails.Hide();

            CreateLabel.MinimumSize = new Size(ProfileList.Width - ProfileList.Margin.Left - ProfileList.Margin.Right, CreateLabel.Height);

            InitializeIconContext();
            UpdateProfileList();

            //Set Scroll settings (make sure to do this after updating the profile list)
            ProfileList.AutoScrollPosition = new Point(0, 0);
            UpdateScrollSettings();
            this.MouseWheel += ProfileListScroll;

            //Create a shortcut in the startups folder so application starts on pc startup
            string userName = Environment.UserName;
            string shortcutName = "SimpleJobTracker.lnk";
            string startupPath = $"C:/Users/{userName}/AppData/Roaming/Microsoft/Windows/Start Menu/Programs/Startup/";

            if (!System.IO.File.Exists(Path.Combine(startupPath, shortcutName))){
                WshShell shell = new WshShell();
                IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(Path.Combine(startupPath, shortcutName));

                shortcut.TargetPath = Application.ExecutablePath;
                shortcut.IconLocation = Path.Combine(Environment.CurrentDirectory, "IconJTS.ico");
                shortcut.WorkingDirectory = Environment.CurrentDirectory;
                shortcut.Save();
            }

            //TODO:turn into function
            try
            {
                string fileName = "EmailSettings.txt";
                var dirInfo = System.IO.Directory.CreateDirectory(Environment.CurrentDirectory + "/Saves");
                string path = Path.Combine(Environment.CurrentDirectory + "/Saves", fileName);

                string fileContent = null;
                using (StreamReader sr = new StreamReader(path))
                {
                    fileContent = sr.ReadToEnd();
                }

                var deserializedSettings = Newtonsoft.Json.JsonConvert.DeserializeObject<EmailSettings>(fileContent);
                EmailSettings settings = deserializedSettings != null ? deserializedSettings : new EmailSettings();

                EmailSettingsAdress.Text = settings.Adress;
                EmailSettingsBubbleBox.Checked = settings.BubbleChecked;
                EmailSettingsSendBox.Checked = settings.SendChecked;
                EmailSettingsSearchBox.Checked = settings.SearchOnStartupChecked;
            }
            catch
            {
                Console.WriteLine("WARNING: Email settings save file does not exist");
            }

            EmailSettingsBubbleBox.Click += new System.EventHandler(EmailSettings_ValueChanged);
            EmailSettingsSendBox.Click += new System.EventHandler(EmailSettings_ValueChanged);
            EmailSettingsAdress.TextChanged += new System.EventHandler(EmailSettings_ValueChanged);

            if (!inDebug)
            {
                this.WindowState = FormWindowState.Minimized;
                this.ShowInTaskbar = false;
                notifyIcon.Visible = true;

            }

            if (EmailSettingsSearchBox.Checked)
            {
                SearchAllProfiles(); //make sure this is called only after everything is initialized
            }
        }

        //Initialize the icon in the notificiation tray
        private void InitializeIconContext()
        {
            components = new System.ComponentModel.Container();
            notifyIcon = new NotifyIcon(components)
            {
                ContextMenuStrip = new ContextMenuStrip(),
                Icon = new Icon(Path.Combine(Directory.GetCurrentDirectory(), "IconJTS.ico")),
                Text = "Simple Job Tracker",
                Visible = true,
                BalloonTipTitle = "Simple Job Tracker",
                BalloonTipText = "Finished checking job pages"
            };
            notifyIcon.DoubleClick += NotifyIcon_DoubleClick;
        }

        //Set the max scroll height
        private void UpdateScrollSettings()
        {
            int scrollMaximum = ProfileList.Controls[0].Height + ProfileList.Controls[0].Margin.Top + ProfileList.Controls[0].Margin.Bottom;//Add height of profile control
            scrollMaximum *= ProfileList.Controls.Count - 1; //Multiply by amount of profiles
            scrollMaximum += CreateLabel.Height + CreateLabel.Margin.Top + CreateLabel.Margin.Bottom; //Add Create label height
            scrollMaximum -= ProfileList.Height; //Remove height of list window
            ProfileList.VerticalScroll.Maximum = scrollMaximum;

            //If scroll height become lower, make sure windows scrolls with it
            if(scrollLocation > ProfileList.VerticalScroll.Maximum)
            {
                scrollLocation = ProfileList.VerticalScroll.Maximum;
                ProfileList.AutoScrollPosition = new Point(0, scrollLocation);
            } 
        }

        //Add all the profiles to the list. will overwrite old controls if the list is already filled
        private void UpdateProfileList()
        {
            ProfileList.Controls.Remove(CreateLabel);
            int existingControls = ProfileList.Controls.Count;
            var profiles = profileManager.GetjobProfiles();
            if(profiles.Count == 0) { 
                ProfileList.Controls.Clear(); 
            }

            for (int i = 0; i < profiles.Count; i++) {
                if(i < existingControls)
                {
                    ProfileList.Controls[i].Controls[0].Text = profiles[i].Name;
                    if (profiles[i].GetLastResult() != null)
                    {
                        ProfileList.Controls[i].Controls[2].Text = "Tags Found: " + profiles[i].GetLastResult().Amount.ToString();
                    }
                }
                else
                {
                    AddProfileControler(profiles[i]);
                }
            }
            ProfileList.Controls.Add(CreateLabel);
        }

        public void UpdateSettingMenu(JobProfile profile)
        {
            //Fill details panel with information
            SettingsName.Text = profile.Name;
            SettingsLink.Text = profile.Link;
            SettingsTags.Text = profile.Tags;

            //Convert history into text
            //TODO: Turn history into a diagram
            string history = "";
            if (profile.History != null)
            {
                for(int i = profile.History.Count - 1; i >= 0; i--)
                {
                    history += profile.History[i].Date + ": ";
                    history += profile.History[i].Amount.ToString();
                    history += "\n";
                }
            }

            SettingsHistory.Text = history;
            SaveWarningImage.Hide();

            //Change background color to visualize selected profile
            
        }

        private async Task SearchAllProfiles()
        {
            var jobs = profileManager.GetjobProfiles();
            bool changeFound = false;

            string mailBody = "";
            string mailHeader = "Job tracker has detected changes in the following websites: \n\n";

            string increasedJobs = "";
            string decreasedJobs = "";

            for (int i = 0; i < jobs.Count; i++)
            {
                bool resultChanged = await searchWeb(jobs[i]);
                if (resultChanged)
                {
                    changeFound = true;
                    if (jobs[i].History.Count > 1) {
                        if(jobs[i].History[jobs[i].History.Count - 1].Amount > jobs[i].History[jobs[i].History.Count - 2].Amount)
                        {
                            increasedJobs += jobs[i].Name + " - increased from " + jobs[i].History[jobs[i].History.Count - 2].Amount.ToString() + " to " + jobs[i].History[jobs[i].History.Count - 1].Amount.ToString() + "\n";
                        }
                        else
                        {
                            decreasedJobs += jobs[i].Name + " - decreased from " + jobs[i].History[jobs[i].History.Count - 2].Amount + " to " + jobs[i].History[jobs[i].History.Count - 1].Amount + "\n";
                        }
                    }
                    else
                    {
                        increasedJobs += jobs[i].Name + " - increased from 0 to " + jobs[i].History[0].Amount.ToString() + "\n";
                    }
                }
            }

            if (changeFound)
            {
                if (increasedJobs != "")
                {
                    mailBody += "Jobs that have been increased: \n";
                    mailBody += increasedJobs;
                }
                if (decreasedJobs != "")
                {
                    mailBody += "Jobs that have been decreased: \n";
                    mailBody += decreasedJobs;
                }

                if (EmailSettingsSendBox.Checked)
                {
                    await emailer.SendEmailAsync(EmailSettingsAdress.Text, mailHeader + mailBody);
                }
                if (EmailSettingsBubbleBox.Checked)
                {
                    notifyIcon.BalloonTipTitle = mailHeader;
                    notifyIcon.BalloonTipText = mailBody;
                    notifyIcon.ShowBalloonTip(bubbleDuration);
                }
            }
        }

        //Try to search the web and retrieve the amount of instances of the profiles tags
        private async Task<bool> searchWeb(JobProfile profile)
        {
            Console.WriteLine("Searching page for: " + profile.Name);

            Stopwatch sw = Stopwatch.StartNew(); //Stopwatch for speed testing
            ProfileList.Controls[profile.Id].Controls[2].Text = ""; //Empty search result
            
            //Disable both search buttons
            SearchButton.Enabled = false;
            SearchButton.BackColor = selectedColor;
            SearchAllButton.Enabled = false;
            SearchAllButton.BackColor = selectedColor;

            bool resultChanged = false;

            try
            {

                //Search the web page and count every instance of the tags from the tag string
                var tagString = profile.Tags;
                if (tagString != null && tagString.Length > 0)
                {
                    string[] companyTags = tagString.Split(' ');
                    int amount = await javaScraper.CheckWeb(profile.Link, companyTags);
                    ProfileList.Controls[profile.Id].Controls[2].Text = "Tags Found: " + amount.ToString();

                    if (profile.History == null || profile.History.Count == 0 || profile.History[profile.History.Count - 1].Amount != amount)
                    {
                        profileManager.AddNewResult(profile, amount);
                        UpdateSettingMenu(profile);
                        resultChanged = true;
                    }
                }
            }
            catch (Exception ex)
            {
                ProfileList.Controls[profile.Id].Controls[2].Text = ex.Message;
            }
            
            //Turn elapsed time into seconds
            sw.Stop();
            float time = sw.ElapsedMilliseconds;
            int roundedTime = (int)(time / 10f);
            float timeInSeconds = roundedTime / 100f;
            Console.WriteLine("Succesfully gotten job page info, time elapsed: " + timeInSeconds);

            //Enable search buttons
            SearchButton.Enabled = true;
            SearchButton.BackColor = buttonColor;
            SearchAllButton.Enabled = true;
            SearchAllButton.BackColor = buttonColor;

            Console.WriteLine("Succesfully searched page");
            return resultChanged;
        }

        //Create a groupbox with the profile information and add it to the list
        //TODO: Turn profiles into a class
        private void AddProfileControler(JobProfile profile)
        {
            var con = new System.Windows.Forms.GroupBox();
            var settingButton = new System.Windows.Forms.Button();
            var resultLabel = new System.Windows.Forms.Label();
            var companyName = new System.Windows.Forms.Label();


            // CREATING SETTINGS BUTTON
            settingButton.BackColor = SettingButton.BackColor;
            settingButton.ForeColor = SettingButton.ForeColor;
            settingButton.Location = SettingButton.Location;
            settingButton.Name = "SettingButton " + profile.Id;
            settingButton.Size = SettingButton.Size;
            settingButton.TabIndex = SettingButton.TabIndex;
            settingButton.Text = ". . .";
            settingButton.UseVisualStyleBackColor = SettingButton.UseVisualStyleBackColor;
            settingButton.Click += new System.EventHandler(this.ProfileSetting_Click);

            // CREATING RESULT LABEL
            resultLabel.Location = ResultLabel.Location;
            resultLabel.Name = "ResultLabel " + profile.Id;
            resultLabel.Size = ResultLabel.Size;
            resultLabel.TabIndex = ResultLabel.TabIndex;
            resultLabel.TextAlign = ResultLabel.TextAlign;
            resultLabel.Font = ResultLabel.Font;

            // CREATING NAME LABEL
            companyName.Dock = CompanyName.Dock;
            companyName.Font = CompanyName.Font;
            companyName.Location = CompanyName.Location;
            companyName.Name = "CompanyName " + profile.Id;
            companyName.Size = CompanyName.Size;
            companyName.TabIndex = CompanyName.TabIndex;
            companyName.Text = profile.Name;

            // CREATING PROFILE GROUPBOX
            con.Controls.Add(companyName);
            con.Controls.Add(settingButton);
            con.Controls.Add(resultLabel);
            con.Dock = TestProfile.Dock;
            con.ForeColor = TestProfile.ForeColor;
            con.Location = TestProfile.Location;
            con.Name = "Profile: " + profile.Id;
            con.Padding = TestProfile.Padding;
            con.Size = TestProfile.Size;
            con.TabIndex = TestProfile.TabIndex;
            con.TabStop = false;

            ProfileList.Controls.Add(con);
        }

        ///
        /// UI Linked Events
        ///

        //open the setting menu for clicked profile
        //TODO: let user open settings by clicking profile and let user drag profile to change order
        private void ProfileSetting_Click(object sender, EventArgs e)
        {
            CompanyDetails.Show();
            Control settingButton = sender as Control;
            GroupBox companyHeader = settingButton.Parent as GroupBox;
            if (companyHeader != null)
            {
                if (selectedProfile != null) selectedProfile.BackColor = defaultColor;
                selectedProfile = companyHeader;
                selectedProfile.BackColor = selectedColor;

                JobProfile profile = profileManager.GetjobProfiles()[ProfileList.Controls.GetChildIndex(companyHeader)];
                UpdateSettingMenu(profile);
            }

        }

        //Scroll through job profiles
        private void ProfileListScroll(object sender, MouseEventArgs e)
        {
            if (ProfileList.ClientRectangle.Contains(e.X, e.Y)) 
            {
                var temp = scrollLocation - (int)(e.Delta * scrollSpeed);
                if (temp < 0)
                {
                    scrollLocation = 0;
                    ProfileList.AutoScrollPosition = new Point(0, scrollLocation);
                }
                else if (temp > ProfileList.VerticalScroll.Maximum)
                {
                    scrollLocation = ProfileList.VerticalScroll.Maximum;
                    ProfileList.AutoScrollPosition = new Point(0, scrollLocation);
                }
                else
                {
                    scrollLocation = temp;
                    ProfileList.VerticalScroll.Value = scrollLocation;
                }
            }
            
        }

        //Search individual job page
        private void Search_Click(object sender, EventArgs e)
        {
            var companyHeader = selectedProfile as GroupBox;
            JobProfile profile = profileManager.GetjobProfiles()[ProfileList.Controls.GetChildIndex(companyHeader)];
            searchWeb(profile);
        }

        //Exit application
        private void Exit_Clicked(object sender, EventArgs e)
        {
            Application.Exit();
        }

        //Move application to notification tray
        private void Minimize_Clicked(object sender, EventArgs e)
        {
            //TODO: Turn into function
            if (this.WindowState != FormWindowState.Minimized)
            {
                this.WindowState = FormWindowState.Minimized;
                this.ShowInTaskbar = false;
                notifyIcon.Visible = true;
            }
        }

        //Open application from notification tray
        private void NotifyIcon_DoubleClick(object sender, EventArgs e)
        {
            //TODO: Turn into function
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.WindowState = FormWindowState.Normal;
                this.ShowInTaskbar = true;
                notifyIcon.Visible = false;
            }
        }

        //Start grabbing the window
        private void Header_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                startPoint = new Point(e.X, e.Y);
            }
        }

        //Let go of the window
        private void Header_MouseUp(object sender, MouseEventArgs e)
        {
            if(e.Button == MouseButtons.Left)
            {
                startPoint = Point.Empty;
            }
        }

        //Move the window
        private void Header_MouseMove(object sender, MouseEventArgs e)
        {
            if (startPoint != Point.Empty)
            {
                Location = new Point(Left + (e.X - startPoint.X), Top + (e.Y - startPoint.Y));
            }
        }

        private void Create_Click(object sender, EventArgs e)
        {
            var profile = profileManager.CreateJobProfile();
            ProfileList.Controls.Remove(CreateLabel);
            AddProfileControler(profile);
            ProfileList.Controls.Add(CreateLabel);
            UpdateScrollSettings();
        }

        private void Create_MouseDown(object sender, MouseEventArgs e)
        {
            var button = sender as Control;
            button.BackColor = selectedColor;
        }

        private void Create_MouseUp(object sender, MouseEventArgs e)
        {
            var button = sender as Control;
            button.BackColor = defaultColor;
        }

        //Delete the selected profile
        private void Delete_Click(object sender, EventArgs e)
        {
            //Get selected profile and delete it
            JobProfile profile = profileManager.GetjobProfiles()[ProfileList.Controls.GetChildIndex(selectedProfile)];
            profileManager.DeleteJobProfile(profile);
            
            //Remove the control for the removed profile
            ProfileList.Controls.Remove(selectedProfile);
            selectedProfile = null;

            //Hide settings menu
            CompanyDetails.Hide();
            UpdateScrollSettings();
        }

        //Save Settings to the selected profile
        private void Save_Click(object sender, EventArgs e)
        {
            //Get selected profile and set its values to the values in settings
            JobProfile profile = profileManager.GetjobProfiles()[ProfileList.Controls.GetChildIndex(selectedProfile)];
            profile.Name = SettingsName.Text;
            profile.Link = SettingsLink.Text;
            profile.Tags = SettingsTags.Text;

            profileManager.UpdateJobProfile(profile); //update profile in save file
            UpdateProfileList();
            SaveWarningImage.Hide();
        }

        private void SettingsInput_TextChanged(object sender, EventArgs e)
        {
            if(selectedProfile != null)
            {
                JobProfile profile = profileManager.GetjobProfiles()[ProfileList.Controls.GetChildIndex(selectedProfile)];
                if (profile.Name != SettingsName.Text || profile.Link != SettingsLink.Text || profile.Tags != SettingsTags.Text)
                {
                    SaveWarningImage.Show();
                }
                else
                {
                    SaveWarningImage.Hide();
                }
            }
        }

        private void EmailSettings_ValueChanged(object sender, EventArgs e)
        {
            //TODO: turn into service class
            try
            {
                string fileName = "EmailSettings.txt";
                string path = Path.Combine(Environment.CurrentDirectory + "/Saves", fileName);

                if (!string.IsNullOrEmpty(Environment.CurrentDirectory + "/Saves"))
                {
                    Directory.CreateDirectory(Environment.CurrentDirectory + "/Saves");
                }

                var settings = new EmailSettings() { Adress = EmailSettingsAdress.Text, BubbleChecked = EmailSettingsBubbleBox.Checked, SendChecked = EmailSettingsSendBox.Checked, SearchOnStartupChecked = EmailSettingsSearchBox.Checked };
                var serializedObject = Newtonsoft.Json.JsonConvert.SerializeObject(settings, Newtonsoft.Json.Formatting.Indented);//serialize profiles

                using (StreamWriter sw = new StreamWriter(path))
                {
                    sw.Write(serializedObject);
                }

                Console.WriteLine("Succesfully saved email settings");
            }
            catch
            {
                Console.WriteLine("ERROR: Could not write to email settings save file");
            }
            
        }

        private void SearchAll_Click(object sender, EventArgs e)
        {
            SearchAllProfiles();
        }
    }
}
