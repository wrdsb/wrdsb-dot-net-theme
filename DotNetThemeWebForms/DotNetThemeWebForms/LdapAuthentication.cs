using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.DirectoryServices;
using System.Text;

namespace DotNetThemeWebForms
{
    public class LdapAuthentication
    {
        private String _path;
        private String _filterAttribute;

        public LdapAuthentication(String path)
        {
            _path = path;
        }

        public bool IsAuthenticated(String domain, String username, String pwd)
        {
            String domainAndUsername = domain + @"\" + username;
            DirectoryEntry entry = new DirectoryEntry(_path, domainAndUsername, pwd);

            try
            {	//Bind to the native AdsObject to force authentication.			
                Object obj = entry.NativeObject;

                DirectorySearcher search = new DirectorySearcher(entry);

                search.Filter = "(SAMAccountName=" + username + ")";
                search.PropertiesToLoad.Add("cn");
                SearchResult result = search.FindOne();

                if (null == result)
                {
                    return false;
                }

                //Update the new path to the user in the directory.
                _path = result.Path;
                _filterAttribute = (String)result.Properties["cn"][0];
            }
            catch (Exception ex)
            {
                throw new Exception("Error authenticating user. " + ex.Message);
            }

            return true;
        }

        public String GetDescription()
        {
            DirectorySearcher search = new DirectorySearcher(_path);
            search.Filter = "(cn=" + _filterAttribute + ")";
            search.PropertiesToLoad.Add("description");
            string dept = "";

            try
            {
                SearchResult result = search.FindOne();
                dept = result.Properties["description"][0].ToString();
            }
            catch (Exception ex)
            {
                throw new Exception("Error obtaining department name. " + ex.Message);
            }
            return dept;
        }

        public String GetDepartment()
        {
            DirectorySearcher search = new DirectorySearcher(_path);
            search.Filter = "(cn=" + _filterAttribute + ")";
            search.PropertiesToLoad.Add("department");
            string dept = "";

            try
            {
                SearchResult result = search.FindOne();
                dept = result.Properties["department"][0].ToString();
            }
            catch (Exception ex)
            {
                throw new Exception("Error obtaining department name. " + ex.Message);
            }
            return dept;
        }
        public String GetSurname()
        {
            DirectorySearcher search = new DirectorySearcher(_path);
            search.Filter = "(cn=" + _filterAttribute + ")";
            search.PropertiesToLoad.Add("sn");
            string surname = "";

            try
            {
                SearchResult result = search.FindOne();
                surname = result.Properties["sn"][0].ToString();
            }
            catch (Exception ex)
            {
                throw new Exception("Error obtaining surname. " + ex.Message);
            }
            return surname;
        }
        public String GetFirstname()
        {
            DirectorySearcher search = new DirectorySearcher(_path);
            search.Filter = "(cn=" + _filterAttribute + ")";
            search.PropertiesToLoad.Add("givenName");
            string firstname = "";

            try
            {
                SearchResult result = search.FindOne();
                firstname = result.Properties["givenName"][0].ToString();
            }
            catch (Exception ex)
            {
                throw new Exception("Error obtaining first name. " + ex.Message);
            }
            return firstname;
        }
        public String GetFullName()
        {
            DirectorySearcher search = new DirectorySearcher(_path);
            search.Filter = "(cn=" + _filterAttribute + ")";
            search.PropertiesToLoad.Add("displayName");
            string fullname = "";

            try
            {
                SearchResult result = search.FindOne();
                fullname = result.Properties["displayName"][0].ToString();
            }
            catch (Exception ex)
            {
                throw new Exception("Error obtaining display name. " + ex.Message);
            }
            return fullname;
        }
        public String GetGroups()
        {
            DirectorySearcher search = new DirectorySearcher(_path);
            search.Filter = "(cn=" + _filterAttribute + ")";
            search.PropertiesToLoad.Add("memberOf");
            StringBuilder groupNames = new StringBuilder();

            try
            {
                SearchResult result = search.FindOne();

                int propertyCount = result.Properties["memberOf"].Count;

                String dn;
                int equalsIndex, commaIndex;

                for (int propertyCounter = 0; propertyCounter < propertyCount; propertyCounter++)
                {
                    dn = (String)result.Properties["memberOf"][propertyCounter];

                    equalsIndex = dn.IndexOf("=", 1);
                    commaIndex = dn.IndexOf(",", 1);
                    if (-1 == equalsIndex)
                    {
                        return null;
                    }

                    groupNames.Append(dn.Substring((equalsIndex + 1), (commaIndex - equalsIndex) - 1));
                    groupNames.Append("|");

                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error obtaining group names. " + ex.Message);
            }
            return groupNames.ToString();
        }
    }
}