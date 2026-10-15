using SimpleJobTracker.Models;
using SimpleJobTracker.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SimpleJobTracker
{
    public partial class Visual : Form
    {
        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(
            int nLeftRect,     // x-coordinate of upper-left corner
            int nTopRect,      // y-coordinate of upper-left corner
            int nRightRect,    // x-coordinate of lower-right corner
            int nBottomRect,   // y-coordinate of lower-right corner
            int nWidthEllipse, // width of ellipse
            int nHeightEllipse // height of ellipse
        );

        JavaScraper javaScraper = new JavaScraper();
        //ProfileManager ProfileManager = new ProfileManager();

        public Visual()
        {
            InitializeComponent();
            
            this.FormBorderStyle = FormBorderStyle.None;
            Region = System.Drawing.Region.FromHrgn(CreateRoundRectRgn(0, 0, Width, Height, 20, 20));

            CompanyDetails.Hide();
            //ProfileManager.LoadJobProfiles();

            /*List<JobProfile> jobProfiles = ProfileManager.GetjobProfiles();

            for(int i = 0; i < jobProfiles.Count; i++)
            {
                Control con = new System.Windows.Forms.GroupBox();

                con.Controls.Add(this.button2);
                con.Controls.Add(this.label3);
                con.Controls.Add(this.button1);
                con.Controls.Add(this.CompanyLink);
                con.Dock = System.Windows.Forms.DockStyle.Top;
                con.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(243)))), ((int)(((byte)(244)))), ((int)(((byte)(244)))));
                con.Location = new System.Drawing.Point(3, 3);
                con.Name = "Profile: " + jobProfiles[i].Id;
                con.Padding = new System.Windows.Forms.Padding(5, 10, 5, 3);
                con.Size = new System.Drawing.Size(300, 100);
                con.TabIndex = 2;
                con.TabStop = false;
                con.Text = jobProfiles[i].Name;

                ProfileList.Controls.Add(con);
            }*/
            
        }

        private void Search_Click(object sender, EventArgs e)
        {
            searchWeb();
        }

        private async Task<bool> searchWeb()
        {
            Stopwatch sw = Stopwatch.StartNew(); //stopwatch for speed testing
            label3.Text = "";

            try
            {
                //Search the web page and count every instance of the tags from the tag string
                var tagString = CompanyTags.Text;
                string[] companyTags = tagString.Split(' ');
                int amount = await javaScraper.CheckWeb(CompanyLink.Text, companyTags);
                label3.Text = amount.ToString();
            }
            catch (Exception ex)
            {
                label3.Text = ex.Message;
            }
            
            //Turn elapsed time into seconds
            sw.Stop();
            float time = sw.ElapsedMilliseconds;
            int roundedTime = (int)(time);
            
            return true;
        }

        //open the setting menu for clicked profile
        private void ProfileSetting_Click(object sender, EventArgs e)
        {
            CompanyDetails.Show();
            Control settingButton = sender as Control;
            GroupBox companyHeader = settingButton.Parent as GroupBox;
            if (companyHeader != null)
            {
                CompanyDetails.Text = companyHeader.Text;
            }

        }
    }
}
