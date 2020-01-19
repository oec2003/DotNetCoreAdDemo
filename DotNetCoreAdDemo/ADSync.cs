using System;
using System.Collections.Generic;
using Novell.Directory.Ldap;

namespace DotNetCoreAdDemo
{
    public class ADSync
    {
        User _user = new User();
        Org _org = new Org();

        public bool Sync()
        {
            bool result = true;
            List<LdapEntry> entryList = ADHelper.GetRootEntries();
            _org = new Org();
            _user = new User();
            Org rootOrg = _org.GetRootOrg();
            foreach (LdapEntry entry in entryList)
            {
                SyncDirectoryEntry(entry, rootOrg, entry);
            }

            return result;
        }

        private Org SyncOrgFromEntry(LdapEntry rootEntry, Org parentOrg, LdapEntry entry)
        {
            string orgId = entry.Guid().ToLower();
            Org org = this._org.GetOrgById(orgId) as Org;
            if (org != null)
            {
                if (entry.ContainsAttr("ou"))
                {
                    org.Name = entry.getAttribute("ou").StringValue + string.Empty;
                }
                //设置其他属性的值
                _org.UpdateOrg(org);
                return org;
            }
            org = new Org
            {
                Id = orgId,
                ParentId = parentOrg.Id,
            };

            //设置其他属性的值
            this._org.AddOrg(org);
            return org;
        }

        private User SyncUserFromEntry(LdapEntry rootEntry, Org parentOrg, LdapEntry entry)
        {
            string userId = entry.Guid().ToLower();
            User user = this._user.GetUserById(userId);
            if (user != null)
            {
                user.ParentId = parentOrg.Id;
                //设置其他属性的值
                this._user.UpdateUser(user);

                return user;
            }
            user = new User
            {
                Id = userId,
                ParentId = parentOrg.Id
            };
            //设置其他属性的值
            this._user.AddUser(user);
            return user;
        }

        private void SyncDirectoryEntry(LdapEntry rootEntry, Org parentOrg, LdapEntry currentEntry)
        {
            List<LdapEntry> entryList = currentEntry.Children(ADHelper.Connection);
            foreach (LdapEntry entry in entryList)
            {
                if (entry.IsOrganizationalUnit())
                {
                    Org org = this.SyncOrgFromEntry(rootEntry, parentOrg, entry);
                    this.SyncDirectoryEntry(rootEntry, org, entry);
                }
                else if (entry.IsUser())
                {
                    this.SyncUserFromEntry(rootEntry, parentOrg, entry);
                }
            }
        }
    }
}
