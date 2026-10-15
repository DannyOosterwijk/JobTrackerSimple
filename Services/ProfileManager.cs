using OpenQA.Selenium.Internal.Logging;
using SimpleJobTracker.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace SimpleJobTracker.Services
{
    public class ProfileManager
    {
        private string saveFolder;
        private string saveFileName;

        private List<JobProfile> jobs = new List<JobProfile>();

        public ProfileManager() 
        {
            //setup save folder location and name
            saveFolder = Path.Combine(Environment.CurrentDirectory + "/Saves/");
            saveFileName = "ProfileSave.txt";

            LoadJobProfiles();
        }

        //Get a list with all the job profiles, loads from save file if jobs are empty
        public List<JobProfile> GetjobProfiles() 
        {
            if (jobs.Count == 0) LoadJobProfiles();
            return jobs;
        }

        //Serializes all job profiles to JSON and stores into the save file
        public void SaveJobProfiles()
        {
            try
            {
                var serializedObject = Newtonsoft.Json.JsonConvert.SerializeObject(jobs, Newtonsoft.Json.Formatting.Indented);//serialize profiles
                
                //Write save data to file
                string path = Path.Combine(saveFolder + saveFileName);
                using (StreamWriter sw = new StreamWriter(path))
                {
                    sw.Write(serializedObject);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: could not serialize to json or write to file");
            }
        }

        //Get save file and deserializes all job profiles. profiles get stored in jobs
        public void LoadJobProfiles()
        {
            try
            {
                //Get save data from file
                string path = Path.Combine(saveFolder + saveFileName);
                string fileContent = null;
                using (StreamReader sr = new StreamReader(path))
                {
                    fileContent = sr.ReadToEnd();
                }

                jobs = Newtonsoft.Json.JsonConvert.DeserializeObject<List<JobProfile>>(fileContent);//deserialize profiles
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: could not deserialize from json or read file");
            }
            
        }

        //Update a job profile and save to save file
        public void UpdateJobProfile(JobProfile profile)
        {
            try
            {
                jobs[profile.Id] = profile;
                SaveJobProfiles();
            }
            catch
            {
                Console.WriteLine("ERROR: could not update job profile");
            }
        }

        //Create a new job profile with a unique id
        public void CreateJobProfile()
        {
            try
            {
                JobProfile profile = new JobProfile();
                profile.Id = jobs.Count;

                jobs.Add(profile);
                SaveJobProfiles();
            }
            catch (Exception ex) 
            {
                Console.WriteLine("ERROR: could not Create new job profile");
            }
            
        }

        //Remove a job profile from the list and update the profile Id's
        public void DeleteJobProfile(JobProfile profile)
        {
            try
            {
                jobs.RemoveAt(profile.Id);

                //update all Id's to be in order
                for (int i = 0; i < jobs.Count; i++)
                {
                    jobs[i].Id = i;
                }

                SaveJobProfiles();
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: could not delete job profile");
            }
        }

    }
}
