using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web.Security;
using Arvy;
using Databossy;
using Exy;

namespace AspNetMembershipPasswordReset {
    class Program {
        static void Main(String[] args) {
            try {
                Console.Title = "AspNet-Membership password reset tools";

                Func<String, String, String, SqlMembershipProvider> InitializeAndGetAspMembershipConfig = (connectionstring, appname, hashAlgo) => {
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
                };

                Console.WriteLine(new StringBuilder()
                    .AppendLine()
                    .AppendLine("AspNet-Membership password reset tools")
                    .ToString());

                Console.WriteLine(new StringBuilder()
                    .AppendLine("Enter to continue. C, E or Q to exit.")
                    .AppendLine()
                    .ToString());

                String input = Console.ReadLine();

                String[] quitCommands = new[] { "c", "e", "q" };
                while (!quitCommands.Contains(input.ToLowerInvariant())) {
                    Console.Write("Connection String: ");
                    String connString = Console.ReadLine();

                    Console.Write("App Name: ");
                    String appName = Console.ReadLine();

                    Console.Write("Hash Algo: (MD5, SHA1, SHA512) ");
                    String hashAlgo = Console.ReadLine();

                    Console.Write("Mode: (R for Reset, C for Create) ");
                    String mode = Console.ReadLine();

                    Console.Write("Username: ");
                    String username = Console.ReadLine();

                    Console.Write("Password: ");
                    String pwd = Console.ReadLine();

                    Boolean create = mode.Equals("c", StringComparison.InvariantCultureIgnoreCase);
                    String email = String.Empty;
                    String role = String.Empty;
                    if (create) {
                        Console.Write("Email: ");
                        email = Console.ReadLine();

                        Console.WriteLine("Roles:");
                        using (var db = new Database(connString, true)) {
                            IList<SimpleResult> results = db.Query<SimpleResult>(@"
                                    BEGIN
                                        SET NOCOUNT ON

                                        SELECT RoleName Result
                                        FROM dbo.aspnet_Roles

                                        SET NOCOUNT OFF
                                    END").ToList();

                            foreach (SimpleResult result in results)
                                Console.WriteLine(result.Result);
                        }

                        Console.Write("Role: ");
                        role = Console.ReadLine();
                    }

                    Boolean valid =
                        !String.IsNullOrEmpty(connString) && !String.IsNullOrWhiteSpace(connString) &&
                        !String.IsNullOrEmpty(appName) && !String.IsNullOrWhiteSpace(appName) &&
                        !String.IsNullOrEmpty(hashAlgo) && !String.IsNullOrWhiteSpace(hashAlgo) &&
                        !String.IsNullOrEmpty(username) && !String.IsNullOrWhiteSpace(username) &&
                        !String.IsNullOrEmpty(pwd) && !String.IsNullOrWhiteSpace(pwd) &&
                        (create ? !String.IsNullOrEmpty(email) && !String.IsNullOrWhiteSpace(email) : true);

                    if (valid) {
                        SqlMembershipProvider provider = InitializeAndGetAspMembershipConfig(connString, appName, hashAlgo);

                        if (!create) {
                            MembershipUser user = provider.GetUser(username, false);
                            if (user == null)
                                throw new InvalidOperationException("User not found.");

                            Console.WriteLine($"User '{username}' found.");

                            String reset = provider.ResetPassword(username, null);
                            Boolean changed = provider.ChangePassword(username, reset, pwd);
                        }
                        else {
                            MembershipUser user = provider.CreateUser(username, pwd, email, "Kobold", "Not a dragon", true, Guid.NewGuid(), out MembershipCreateStatus status);
                            IDictionary<MembershipCreateStatus, String> statusMessage = new Dictionary<MembershipCreateStatus, String> {
                                [MembershipCreateStatus.DuplicateUserName] = "Username already exists. Please enter a different user name.",
                                [MembershipCreateStatus.DuplicateEmail] = "A username for that email address already exists. Please enter a different email address.",
                                [MembershipCreateStatus.InvalidPassword] = "The password provided is invalid. Please enter a valid password value.",
                                [MembershipCreateStatus.InvalidEmail] = "The email address provided is invalid. Please check the value and try again.",
                                [MembershipCreateStatus.InvalidAnswer] = "The password retrieval answer provided is invalid. Please check the value and try again.",
                                [MembershipCreateStatus.InvalidQuestion] = "The password retrieval question provided is invalid. Please check the value and try again.",
                                [MembershipCreateStatus.InvalidUserName] = "The user name provided is invalid. Please check the value and try again.",
                                [MembershipCreateStatus.ProviderError] = "The authentication provider returned an error. Please verify your entry and try again. If the problem persists, please contact your system administrator.",
                                [MembershipCreateStatus.UserRejected] = "The user creation request has been canceled. Please verify your entry and try again. If the problem persists, please contact your system administrator.",
                                [MembershipCreateStatus.Success] = "the user creation done in success."
                            };

                            if (!statusMessage.ContainsKey(status))
                                throw new InvalidOperationException("An unknown error occurred. Please verify your entry and try again. If the problem persists, please contact your system administrator.");

                            Console.WriteLine(statusMessage[status]);

                            using (var db = new Database(connString, true)) {
                                Int32 result = db.NQueryScalar<Int32>(@"
                                    BEGIN
                                        SET NOCOUNT ON

                                        SELECT COUNT(0) FROM dbo.aspnet_Roles
                                        WHERE LoweredRoleName = @RoleName
                
                                        SET NOCOUNT OFF
                                    END", new { RoleName = role.ToLowerInvariant() });

                                if (result < 1)
                                    throw new InvalidOperationException($"Role {role} isn't found anywhere.");
                            }

                            using (var db = new Database(connString, true)) {
                                String result = db.NQueryScalar<String>(@"
                                    BEGIN
                                        SET NOCOUNT ON
                                        BEGIN TRAN AssignRole

                                        BEGIN TRY
                                            DECLARE
                                                @@message VARCHAR(MAX)

                                            DECLARE @@roleId UNIQUEIDENTIFIER
                                            SELECT TOP 1 @@roleId = RoleId FROM dbo.aspnet_Roles WHERE LoweredRoleName = @RoleName
        
                                            INSERT INTO dbo.aspnet_UsersInRoles
                                            (RoleId, UserId)
                                            VALUES (@@roleId, @UserId)

                                            COMMIT TRAN AssignRole
                                            SET @@message = 'S|Finish'
                                        END TRY
                                        BEGIN CATCH
                                            ROLLBACK TRAN AssignRole
                                            SET @@message = 'E|' + CAST(ERROR_LINE() AS VARCHAR) + ': ' + ERROR_MESSAGE()
                                        END CATCH
                
                                        SET NOCOUNT OFF
                                        SELECT @@message [Message]
                                    END",
                                    new {
                                        UserId = user.ProviderUserKey.ToString(),
                                        RoleName = role.ToLowerInvariant()
                                    });

                                result.AsActionResponseViewModel();
                            }
                        }

                        using (var db = new Database(connString, true)) {
                            String result = db.NQueryScalar<String>(@"
                                BEGIN
                                    SET NOCOUNT ON
                                    BEGIN TRAN ResetPwd

                                    BEGIN TRY
                                        DECLARE
                                            @@message VARCHAR(MAX)

                                        UPDATE dbo.aspnet_Membership SET
                                        IsApproved = '1',
                                        IsLockedOut = '0',
                                        LastLoginDate = DATEADD(DAY, -2, GETDATE()),
                                        LastPasswordChangedDate = DATEADD(DAY, -2, GETDATE())
                                        WHERE UserId IN (SELECT UserId
                                            FROM dbo.aspnet_Users
                                            WHERE UserName = @Username
                                        )

                                        COMMIT TRAN ResetPwd
                                        SET @@message = 'S|Finish'
                                    END TRY
                                    BEGIN CATCH
                                        ROLLBACK TRAN ResetPwd
                                        SET @@message = 'E|' + CAST(ERROR_LINE() AS VARCHAR) + ': ' + ERROR_MESSAGE()
                                    END CATCH
                
                                    SET NOCOUNT OFF
                                    SELECT @@message [Message]
                                END", new { Username = username });
                            result.AsActionResponseViewModel();
                        }

                        Console.WriteLine(new StringBuilder()
                            .AppendLine("Done.")
                            .AppendLine()
                            .ToString());
                    }
                    else {
                        Console.WriteLine(new StringBuilder()
                            .AppendLine("Data not valid.")
                            .AppendLine()
                            .ToString());
                    }

                    Console.WriteLine(new StringBuilder()
                        .AppendLine("Enter to continue. C, E or Q to exit.")
                        .AppendLine()
                        .ToString());

                    input = Console.ReadLine();
                }

                Console.WriteLine(new StringBuilder()
                    .AppendLine("Exit.")
                    .AppendLine()
                    .ToString());

                Console.ReadLine();
            }
            catch (Exception ex) {
                Console.WriteLine(new StringBuilder()
                    .AppendLine("Error:")
                    .AppendLine(ex.GetExceptionMessage())
                    .AppendLine()
                    .ToString());

                Console.ReadLine();
            }
        }
    }
}
