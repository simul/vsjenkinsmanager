﻿using Devkoes.JenkinsClient.Model;
using Devkoes.JenkinsClient.Properties;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Devkoes.JenkinsClient
{
    public class JenkinsManager
    {
        private const char URI_SEPERATOR = '|';
        private static string JENKINS_BUILD_PREFIX_TEXT = "_anime";
        private static Dictionary<string, string> _colorScheme;

        static JenkinsManager()
        {
            if (Settings.Default.JenkinsServers == null)
            {
                Settings.Default.JenkinsServers = new StringCollection();
            }

            _colorScheme = new Dictionary<string, string>() {
                { "red"+JENKINS_BUILD_PREFIX_TEXT, "Firebrick" },
                { "red", "Firebrick" },
                { "blue"+JENKINS_BUILD_PREFIX_TEXT, "ForestGreen" },
                { "blue", "ForestGreen" }
            };
        }

        /// <summary>
        /// Loads all job information
        /// </summary>
        /// <param name="jenkinsServerUrl">The url of the server, without any api paths (eg http://jenkins.cyanogenmod.com/)</param>
        /// <returns>The list of Jobs</returns>
        public async static Task<IEnumerable<Job>> GetJobs(string jenkinsServerUrl)
        {
            JenkinsOverview overview = null;

            try
            {
                WebClient wc = new WebClient();
                string jsonRawData = await wc.DownloadStringTaskAsync(Path.Combine(jenkinsServerUrl, "api/json?pretty=true"));
                overview = JsonConvert.DeserializeObject<JenkinsOverview>(jsonRawData);
            }
            catch (Exception)
            {
                // do something
            }

            // Fix when something went wrong, never return an empty list
            if (overview == null)
            {
                overview = new JenkinsOverview();
            }
            if (overview.Jobs == null)
            {
                overview.Jobs = new List<Job>();
            }

            overview.Jobs = ParseJobs(overview.Jobs);

            return overview.Jobs;
        }

        private static IEnumerable<Job> ParseJobs(IEnumerable<Job> jobs)
        {
            jobs = jobs.ToArray();
            foreach (var job in jobs)
            {
                if (string.IsNullOrEmpty(job.Name) || string.IsNullOrEmpty(job.Color)) continue;

                if (job.Name.Contains(JENKINS_BUILD_PREFIX_TEXT))
                {
                    job.Building = true;
                }

                if (_colorScheme.ContainsKey(job.Color))
                {
                    job.Color = _colorScheme[job.Color];
                }
            }

            return jobs;
        }

        public static void AddServer(JenkinsServer server)
        {
            // TODO: figure out how to save custom objects in an array through settings
            Settings.Default.JenkinsServers.Add(GetServerConfigName(server));
            Settings.Default.Save();
        }

        private static string GetServerConfigName(JenkinsServer server)
        {
            return string.Concat(server.Name, URI_SEPERATOR, server.Url);
        }

        public static IEnumerable<JenkinsServer> GetServers()
        {
            var savedServers = Settings.Default.JenkinsServers;
            List<JenkinsServer> servers = new List<JenkinsServer>();
            savedServers = savedServers ?? new StringCollection();
            foreach (var server in savedServers)
            {
                var parts = server.Split(URI_SEPERATOR);
                if (parts.Length == 2)
                {
                    servers.Add(new JenkinsServer() { Name = parts[0], Url = parts[1] });
                }
            }

            return servers;
        }

        public static void RemoveServer(JenkinsServer server)
        {
            Settings.Default.JenkinsServers.Remove(GetServerConfigName(server));
            Settings.Default.Save();
        }

        public async static Task ScheduleJob(Job j)
        {
            using (WebClient client = new WebClient())
            {
                byte[] response = await client.UploadValuesTaskAsync(Path.Combine(j.Url, "build"), new NameValueCollection()
                   {
                       { "delay", "0sec" }
                   }
                );
            }
        }
    }
}