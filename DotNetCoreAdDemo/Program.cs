using Novell.Directory.Ldap;
using System;

namespace DotNetCoreAdDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            ADClient client = new ADClient();
            client.BaseDC = "OU=我的公司,DC=wdhac,DC=com,DC=cn";
            client.Host = "192.168.16.233";
            client.AdminUserName = "administrator";
            client.AdminUserPwd = "P@ssw0rd";
            client.PathList = new string[] { "OU=我的公司,DC=wdhac,DC=com,DC=cn" };

            try
            {
                //初始化连接
                ADHelper.Connect(client);
                //获取用户
                //LdapEntry entry=  ADHelper.GetUser("oec2003");
                //修改用户密码
                // ADHelper.UpdatePassword("oec2003", "P@ssw0rd1");
                //禁用用户
                //ADHelper.DisabledUser("oec2003");
                //启用用户
                //ADHelper.EnblaedUser("oec2003");
                //修改cn
                //ADHelper.MoveUserToOU("oec2003",rcn:"冯威");
                //移动用户到新的OU
                //string ouContainer = "OU=产品部,OU=我的公司,DC=wdhac,DC=com,DC=cn";
                //ADHelper.MoveUserToOU("jl_fw4", rcn: "jl_fw4_oec1",ouContainer:ouContainer);

                ADHelper.AddUserToGroup("oec2003", "CN=前端开发组,OU=产品部,OU=我的公司,DC=wdhac,DC=com,DC=cn");
                //ADHelper.RemoveUserFromGroup("oec2003", "CN=前端开发组,OU=产品部,OU=我的公司,DC=wdhac,DC=com,DC=cn");

                
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
            }

            Console.WriteLine("Success");
        }
    }
}
