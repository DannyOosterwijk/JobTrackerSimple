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
using System.Threading.Tasks;
using System.Windows.Forms;

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

        //Web scrapping
        private JavaScraper javaScraper = new JavaScraper();
        private ProfileManager profileManager = new ProfileManager();
        private List<Control> profileControls = new List<Control>();

        //Navigating & moving
        private int scrollLocation = 0;
        private float scrollSpeed = 0.2f;
        private Point startPoint = Point.Empty;

        private Control selectedProfile;
        private Color selectedColor = System.Drawing.Color.FromArgb(((int)(((byte)(97)))), ((int)(((byte)(45)))), ((int)(((byte)(83)))));
        private Color defaultColor = System.Drawing.Color.FromArgb(((int)(((byte)(44)))), ((int)(((byte)(44)))), ((int)(((byte)(44)))));

        //Notification tray
        private NotifyIcon notifyIcon;

        public MainForm()
        {
            InitializeComponent();
            
            this.FormBorderStyle = FormBorderStyle.None;
            Region = System.Drawing.Region.FromHrgn(CreateRoundRectRgn(0, 0, Width, Height, 20, 20));

            CompanyDetails.Hide();

            InitializeIconContext();
            UpdateProfileList();

            //Set Scroll settings (make sure to do this after updating the profile list)
            ProfileList.AutoScrollPosition = new Point(0, 0);
            UpdateScrollSettings();
            this.MouseWheel += ProfileListScroll;
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
                    ProfileList.Controls[i].Text = profiles[i].Name;
                    ProfileList.Controls[i].Controls[1].Text = "";
                    ProfileList.Controls[i].Controls[3].Text = profiles[i].Link;
                }
                else
                {
                    AddProfileControler(profiles[i]);
                }
            }
            ProfileList.Controls.Add(CreateLabel);
        }

        //Try to search the web and retrieve the amount of instances of the profiles tags
        private async Task<bool> searchWeb(GroupBox company)
        {
            Stopwatch sw = Stopwatch.StartNew(); //stopwatch for speed testing
            company.Controls[1].Text = "";

            try
            {
                JobProfile profile = profileManager.GetjobProfiles()[ProfileList.Controls.GetChildIndex(company)];

                //Search the web page and count every instance of the tags from the tag string
                var tagString = profile.Tags;
                string[] companyTags = tagString.Split(' ');
                int amount = await javaScraper.CheckWeb(profile.Link, companyTags);
                company.Controls[1].Text = amount.ToString();
            }
            catch (Exception ex)
            {
                ResultLabel.Text = ex.Message;
            }
            
            //Turn elapsed time into seconds
            sw.Stop();
            float time = sw.ElapsedMilliseconds;
            int roundedTime = (int)(time / 10f);
            float timeInSeconds = roundedTime / 100f;
            Console.WriteLine("Succesfully gotten job page info, time elapsed: " + timeInSeconds);
            
            return true;
        }

        //Create a groupbox with the profile information and add it to the list
        //TODO: Turn profiles into a class
        private void AddProfileControler(JobProfile profile)
        {
            var con = new System.Windows.Forms.GroupBox();
            var settingButton = new System.Windows.Forms.Button();
            var resultLabel = new System.Windows.Forms.Label();
            var searchButton = new System.Windows.Forms.Button();
            var companyLink = new System.Windows.Forms.TextBox();

            // CREATING BUTTON
            settingButton.BackColor = SettingButton.BackColor;
            settingButton.ForeColor = SettingButton.ForeColor;
            settingButton.Location = SettingButton.Location;
            settingButton.Name = "SettingButton " + profile.Id;
            settingButton.Size = SettingButton.Size;
            settingButton.TabIndex = SettingButton.TabIndex;
            settingButton.Text = ". . .";
            settingButton.UseVisualStyleBackColor = SettingButton.UseVisualStyleBackColor;
            settingButton.Click += new System.EventHandler(this.ProfileSetting_Click);

            // CREATING LABEL
            resultLabel.Location = ResultLabel.Location;
            resultLabel.Name = "ResultLabel " + profile.Id;
            resultLabel.Size = SettingButton.Size;
            resultLabel.TabIndex = SettingButton.TabIndex;
            resultLabel.TextAlign = SettingButton.TextAlign;

            // CREATING SETTING BUTTON
            searchButton.BackColor = SearchButton.BackColor;
            searchButton.ForeColor = SearchButton.ForeColor;
            searchButton.Location = SearchButton.Location;
            searchButton.Name = "SearchButton " + profile.Id;
            searchButton.Size = SearchButton.Size;
            searchButton.TabIndex = SearchButton.TabIndex;
            searchButton.Text = "Search";
            searchButton.UseVisualStyleBackColor = SearchButton.UseVisualStyleBackColor;
            searchButton.Click += new System.EventHandler(this.Search_Click);

            // CREATING COMPANY LINK
            companyLink.BackColor = CompanyLink.BackColor;
            companyLink.Dock = CompanyLink.Dock;
            companyLink.ForeColor = CompanyLink.ForeColor;
            companyLink.Location = CompanyLink.Location;
            companyLink.Name = "CompanyLink " + profile.Id;
            companyLink.Size = CompanyLink.Size;
            companyLink.TabIndex = CompanyLink.TabIndex;
            companyLink.Text = profile.Link;

            // CREATING PROFILE GROUPBOX
            con.Controls.Add(settingButton);
            con.Controls.Add(resultLabel);
            con.Controls.Add(searchButton);
            con.Controls.Add(companyLink);
            con.Dock = TestProfile.Dock;
            con.ForeColor = TestProfile.ForeColor;
            con.Location = TestProfile.Location;
            con.Name = "Profile: " + profile.Id;
            con.Padding = TestProfile.Padding;
            con.Size = TestProfile.Size;
            con.TabIndex = TestProfile.TabIndex;
            con.TabStop = false;
            con.Text = profile.Name;

            ProfileList.Controls.Add(con);
        }

        ///
        /// UI Linked Events
        ///

        //open the setting menu for clicked profile
        private void ProfileSetting_Click(object sender, EventArgs e)
        {
            CompanyDetails.Show();
            Control settingButton = sender as Control;
            GroupBox companyHeader = settingButton.Parent as GroupBox;
            if (companyHeader != null)
            {
                //Fill details panel with information
                JobProfile profile = profileManager.GetjobProfiles()[ProfileList.Controls.GetChildIndex(companyHeader)];
                CompanyDetails.Text = profile.Name;
                CompanyTags.Text = profile.Tags;

                //Change background color to visualize selected profile
                if (selectedProfile != null) selectedProfile.BackColor = defaultColor;
                selectedProfile = companyHeader;
                selectedProfile.BackColor = selectedColor;
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
            var clickedButton = sender as Control;
            var companyHeader = clickedButton.Parent as GroupBox;
            searchWeb(companyHeader);
        }

        //Exit application
        private void Exit_Clicked(object sender, EventArgs e)
        {
            Application.Exit();
        }

        //Exit application
        private void Minimize_Clicked(object sender, EventArgs e)
        {
            if (this.WindowState != FormWindowState.Minimized)
            {
                this.WindowState = FormWindowState.Minimized;
                this.ShowInTaskbar = false;
                notifyIcon.Visible = true;
            }
        }

        //
        private void NotifyIcon_DoubleClick(object sender, EventArgs e)
        {
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

        private void Delete_Click(object sender, EventArgs e)
        {
            JobProfile profile = profileManager.GetjobProfiles()[ProfileList.Controls.GetChildIndex(selectedProfile)];
            profileManager.DeleteJobProfile(profile);
            ProfileList.Controls.Remove(selectedProfile);
            selectedProfile = null;
            CompanyDetails.Hide();
            UpdateScrollSettings();
        }
    }
}
