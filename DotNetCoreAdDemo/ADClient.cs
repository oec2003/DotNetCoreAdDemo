using System;
namespace DotNetCoreAdDemo
{
    public class ADClient
    {
        public ADClient()
        {
        }

        public string[] PathList { get; set; }

        public string Host { get; set; }

        public string AdminUserName { get; set; }

        public string AdminUserPwd { get; set; }

        public string BaseDC { get; set; }
    }

    public enum UserWorkStationOperType
    {
        Add,
        Remove
    }
}
