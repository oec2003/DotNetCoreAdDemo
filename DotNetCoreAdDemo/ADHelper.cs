using System;
using System.Collections.Generic;
using System.Text;
using Novell.Directory.Ldap;

namespace DotNetCoreAdDemo
{
    public static class ADHelper
    {
        private static LdapConnection _connection;
        public static LdapConnection Connection => _connection;

        private static ADClient _adClient;
        public static ADClient ADClient => _adClient;

        public static bool Connect(ADClient client)
        {
            if ((string.IsNullOrEmpty(client.Host) || string.IsNullOrEmpty(client.AdminUserName)) ||
                string.IsNullOrEmpty(client.AdminUserPwd))
            {
                throw new ArgumentException("AD连接参数不完整");
            }
            try
            {
                _adClient = client;

                _connection = new LdapConnection();

                //不加上下面两行,修改密码时会报错
                _connection.SecureSocketLayer = true;
                _connection.UserDefinedServerCertValidationDelegate += (sender, certificate, chain, sslPolicyErrors) => true;

                _connection.Connect(client.Host, LdapConnection.DEFAULT_SSL_PORT);
                _connection.Bind(LdapConnection.Ldap_V3, client.AdminUserName, client.AdminUserPwd);
            }
            catch
            {
                return false;
            }

            return true;
        }

        public static List<LdapEntry> GetRootEntries()
        {
            List<LdapEntry> list = new List<LdapEntry>();
            foreach (string path in ADClient.PathList)
            {
                if (!string.IsNullOrEmpty(ADClient.Host))
                {
                    LdapEntry entry = _connection.Read(path);
                    list.Add(entry);
                }
            }
            return list;
        }

        /// <summary>
        /// 添加用户
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="loginName"></param>
        /// <param name="defaultPwd"></param>
        /// <param name="container"></param>
        /// <returns></returns>
        public static bool AddUser(string userName, string loginName, string defaultPwd, string container)
        {
            //设置默认密码
            defaultPwd = $"\"{defaultPwd}\"";
            sbyte[] encodedBytes = SupportClass.ToSByteArray(Encoding.Unicode.GetBytes(defaultPwd));

            LdapAttributeSet attributeSet = new LdapAttributeSet();
            attributeSet.Add(new LdapAttribute("objectclass", "user"));
            attributeSet.Add(new LdapAttribute("sAMAccountName", userName));
            //设置创建用户后启用
            attributeSet.Add(new LdapAttribute("userAccountControl", (66080).ToString()));
            attributeSet.Add(new LdapAttribute("unicodePwd", encodedBytes));

            string dn = $"CN={loginName},{container}";
            LdapEntry newEntry = new LdapEntry(dn, attributeSet);
            _connection.Add(newEntry);
            return true;
        }

        /// <summary>
        /// 修改密码
        /// </summary>
        /// <param name="name"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static bool UpdatePassword(string loginName, string password)
        {
            LdapEntry entry = GetUser(loginName);
            if (entry == null)
            {
                throw new Exception($"名为:{loginName} 的用户在AD中不存在");
            }

            password = $"\"{password}\"";
            sbyte[] encodedBytes = SupportClass.ToSByteArray(Encoding.Unicode.GetBytes(password));
            LdapAttribute attributePassword = new LdapAttribute("unicodePwd", encodedBytes);

            _connection.Modify(entry.DN, new LdapModification(LdapModification.REPLACE, attributePassword));

            return true;
        }

        /// <summary>
        /// 禁用用户
        /// </summary>
        /// <param name="loginName"></param>
        /// <returns></returns>
        public static bool DisabledUser(string loginName)
        {
            LdapEntry entry = GetUser(loginName);
            if (entry == null)
            {
                throw new Exception($"名为:{loginName} 的用户在AD中不存在");
            }

            LdapAttribute attributePassword = new LdapAttribute("userAccountControl", (66082).ToString());
            _connection.Modify(entry.DN, new LdapModification(LdapModification.REPLACE, attributePassword));

            return true;
        }

        /// <summary>
        /// 启用用户
        /// </summary>
        /// <param name="loginName"></param>
        /// <returns></returns>
        public static bool EnblaedUser(string loginName)
        {
            LdapEntry entry = GetUser(loginName);
            if (entry == null)
            {
                throw new Exception($"名为:{loginName} 的用户在AD中不存在");
            }

            LdapAttribute attributePassword = new LdapAttribute("userAccountControl", (66080).ToString());
            _connection.Modify(entry.DN, new LdapModification(LdapModification.REPLACE, attributePassword));

            return true;
        }

        /// <summary>
        /// 移动用户到新的OU
        /// </summary>
        /// <param name="loginName">登录名</param>
        /// <param name="rcn">如果要修改cn,可以指定新的值,否则传原始值</param>
        /// <param name="ouContainer"></param>
        /// <returns></returns>
        public static bool MoveUserToOU(string loginName, string rcn = "", string ouContainer = "")
        {
            LdapEntry entry = GetUser(loginName);
            if (entry == null)
            {
                throw new Exception($"名为:{loginName} 的用户在AD中不存在");
            }

            string cn = entry.AttrStringValue("cn");
            cn = rcn == "" ? cn : rcn;
            string newRCN = $"CN={cn}";

            if (string.IsNullOrWhiteSpace(ouContainer))
            {
                _connection.Rename(entry.DN, newRCN, true);
            }
            else
            {
                _connection.Rename(entry.DN, newRCN, ouContainer, true);
            }

            return true;
        }

        public static LdapEntry GetUser(string loginName)
        {
            var entities = _connection.Search(ADClient.BaseDC,LdapConnection.SCOPE_SUB,
                       $"sAMAccountName={loginName}",
                       null, false);
            LdapEntry entry = null;
            while (entities.HasMore())
            {

                try
                {
                    entry = entities.Next();
                }
                catch (LdapException e)
                {
                    Console.WriteLine($"GetUser Error: {e.Message}");
                    continue;
                }
            }

            return entry;
        }


        public static bool AddUserToGroup(string loginName, string groupDN)
        {
            LdapEntry entry = GetUser(loginName);
            if (entry == null)
            {
                throw new Exception($"名为:{loginName} 的用户在AD中不存在");
            }

            List<string> memberOf = entry.AttrStringValueArray("memberOf");
            if (memberOf.Contains(groupDN))
            {
                throw new Exception($"名为:{loginName} 的用户已经加入了组: {groupDN}");
            }

            LdapModification[] modGroup = new LdapModification[1];
            LdapAttribute member = new LdapAttribute("member", entry.DN);
            modGroup[0] = new LdapModification(LdapModification.ADD, member);

            try
            {
                _connection.Modify(groupDN, modGroup);
            }
            catch (LdapException e)
            {
                System.Console.Error.WriteLine("Failed to modify group's attributes: " + e.LdapErrorMessage);
                return false;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("AddUserToGroup Error:" + e.Message);
                return false;
            }
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="loginName"></param>
        /// <param name="groupDN"></param>
        /// <returns></returns>
        public static bool RemoveUserFromGroup(string loginName, string groupDN)
        {
            LdapEntry entry = GetUser(loginName);
            if (entry == null)
            {
                throw new Exception($"名为:{loginName} 的用户在AD中不存在");
            }

            List<string> memberOf = entry.AttrStringValueArray("memberOf");
            if (!memberOf.Contains(groupDN))
            {
                throw new Exception($"名为:{loginName} 的用户不存在于组: {groupDN} 中");
            }

            LdapModification[] modGroup = new LdapModification[1];
            LdapAttribute member = new LdapAttribute("member", entry.DN);
            modGroup[0] = new LdapModification(LdapModification.DELETE, member);

            try
            {
                _connection.Modify(groupDN, modGroup);
            }
            catch (LdapException e)
            {
                System.Console.Error.WriteLine("Failed to delete group's attributes: " + e.LdapErrorMessage);
                return false;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("RemoveUserFromGroup Error:" + e.Message);
                return false;
            }
            return true;
        }

        /// <summary>
        /// 添加用户登录到
        /// </summary>
        /// <param name="loginName"></param>
        /// <param name="computerName"></param>
        /// <returns></returns>
        public static bool UpdateUserWorkStation(string loginName, string computerName, UserWorkStationOperType type)
        {
            LdapEntry entry = GetUser(loginName);
            if (entry == null)
            {
                throw new Exception($"名为:{loginName} 的用户在AD中不存在");
            }

            List<string> stations = entry.AttrStringValue("userWorkstations").Split(',').ToList();
            if (type == UserWorkStationOperType.Add && !stations.Contains(computerName))
            {
                stations.Add(computerName);
            }
            else if (type == UserWorkStationOperType.Remove && stations.Contains(computerName))
            {
                stations.Remove(computerName);
            }

            LdapAttribute attributePassword = new LdapAttribute("userWorkstations", string.Join(',', stations));
            _connection.Modify(entry.DN, new LdapModification(LdapModification.REPLACE, attributePassword));
            return true;
        }

    }
}
