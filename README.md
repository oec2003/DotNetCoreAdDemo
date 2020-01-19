# DotNetCoreAdDemo
use Novell.Directory.Ldap

## 环境

* dotNET Core：3.0
* Novell.Directory.Ldap.NETStandard2_0：3.1.0
* AD：windows server 2012

## 基本操作

### 查询

在 AD 中进行用户的操作，通常需要先判断用户是否存在，这时就需要使用查询了，用下面代码可以进行 AD 中的查询：

```
 var entities = _connection.Search(ADClient.BaseDC, 		LdapConnection.SCOPE_SUB,
     $"sAMAccountName={loginName}",
     null, false);
```

参数说明：

* base: 指定的总体搜索范围，通常在创建连接时会指定一个 BaseDC，表示后面的操作在此 DC 范围内,如果希望从根开始搜索，此参数可传空
* scope：查询遍历的方式，分为 SCOPE_BASE 、SCOPE_ONE 和 SCOPE_SUB 三种
	* SCOPE_BASE：通常知道对象的 DN，并希望获取其属性时，使用此项
	* SCOPE_ONE：查询 base 的下一层级
	* SCOPE_SUB：查询 base 下的所有对象，包含 base
* filter：用来过滤的表达式，下面列出一些常用的表达式
 
 ```
 (cn=oec2003)：返回 cn 等于 oec2003 的用户
 (sAMAccountName=oec*)：返回登录名以 oec 开头的用户
 !(cn=oec2003)：返回 cn 不等于 oec2003 的用户
 (|(cn=oec2003)(telephonenumber=888*))：返回 cn 等于 oec2003 ，或者电话号码以 888 开头的用户
 (&(cn=oec2003)(telephonenumber=888*))：返回 cn 等于 oec2003 ，并且电话号码以 888 开头的用户
 ```
 其他更多的表达式可以参考官方文档：[https://www.novell.com/documentation/developer/ldapcsharp/?page=/documentation/developer/ldapcsharp/cnet/data/bovtuda.html](https://www.novell.com/documentation/developer/ldapcsharp/?page=/documentation/developer/ldapcsharp/cnet/data/bovtuda.html)

* attrs：字符串数组，可以指定返回的属性的列表，不指定返回所有的属性

例如根据登录名来查询用户的示例代码如下：

```
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
```

### 添加用户

```
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
```

注意点：

* 默认密码的设置需要给密码加上引号
* 默认情况下创建的用户时禁用的，如果要启用需要加上代码`attributeSet.Add(new LdapAttribute("userAccountControl", (66080).ToString()));`

### 修改密码

```
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
```

### 禁用用户

```
public static bool EnblaedUser(string loginName)
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
```

### 启用用户

```
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
```

### 移动用户到新的 OU

```
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
```

注意点：

* 一个用户一旦创建，DN是不能修改的，可以通过`Rename`方法来修改CN来达到修改DB的目的
* 如果传入第三个参数`ouContainer`,就可以实现将用户移动到目标OU

### 添加用户到组

```
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
```

### 用户从组中移除

```
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
```

### 添加用户登录到

```
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
```
