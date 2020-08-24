using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Reflection;
using System.Web.Security;

namespace AspNetMembershipPasswordReset {
    public class MembershipService {

        public static SqlMembershipProvider InitializeAndGetAspMembershipConfig (String connectionstring, String appname, String hashAlgo) {
            typeof(ConfigurationElementCollection)
                .GetField("bReadOnly", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(ConfigurationManager.ConnectionStrings, false);

            ConfigurationManager.ConnectionStrings.Add(new ConnectionStringSettings("DefaultConnection", connectionstring, "System.Data.SqlClient"));

            var membershipProv = new SqlMembershipProvider();
            membershipProv.Initialize("AspNetSqlMembershipProvider", new NameValueCollection {
                ["connectionStringName"] = "DefaultConnection",
                ["applicationName"] = appname,
                ["enablePasswordRetrieval"] = "false",
                ["enablePasswordReset"] = "true",
                ["requiresQuestionAndAnswer"] = "false",
                ["requiresUniqueEmail"] = "true",
                ["minRequiredNonalphanumericCharacters"] = "0",
                ["minRequiredPasswordLength"] = "1",
                ["maxInvalidPasswordAttempts"] = "10",
                ["passwordStrengthRegularExpression"] = ".+",
                ["passwordFormat"] = "Hashed"
            });

            typeof(SqlMembershipProvider)
                .GetField("s_HashAlgorithm", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(membershipProv, hashAlgo);

            return membershipProv;
        }
    }
}
