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

                if (!string.IsNullOrEmpty(saveFolder))
                {
                    Directory.CreateDirectory(saveFolder);
                }

                using (StreamWriter sw = new StreamWriter(path))
                {
                    sw.Write(serializedObject);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: could not serialize to json or write to file");
                throw new Exception(ex.Message);
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

                if(!File.Exists(path)) SaveJobProfiles(); //create empty save file if save file does not exist

                using (StreamReader sr = new StreamReader(path))
                {
                    fileContent = sr.ReadToEnd();
                }

                var deserializedJobs = Newtonsoft.Json.JsonConvert.DeserializeObject<List<JobProfile>>(fileContent);//deserialize profiles
                jobs = deserializedJobs != null ? deserializedJobs : new List<JobProfile>();//Make list empty if save file is empty
                
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: could not deserialize from json or read file");
                throw new Exception(ex.Message);
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
        public JobProfile CreateJobProfile()
        {
            try
            {
                JobProfile profile = new JobProfile();
                profile.Id = jobs.Count;
                profile.Name = "New Company" + jobs.Count.ToString();

                jobs.Add(profile);
                SaveJobProfiles();

                return profile;
            }
            catch (Exception ex) 
            {
                Console.WriteLine("ERROR: could not Create new job profile");
                throw new Exception(ex.Message);
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
                throw new Exception(ex.Message);
            }
        }

        public void AddNewResult(JobProfile profile, int amount)
        {
            try
            {
                string date = DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString() + "-" + DateTime.Now.Day.ToString();
                if(profile.History == null)
                {
                    profile.History = new List<SearchResult>();
                }
                profile.History.Add(new SearchResult() { Amount = amount, Date = date });
                UpdateJobProfile(profile);
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: could not add date to history");
                throw new Exception(ex.Message);
            }
        }

    }
}
