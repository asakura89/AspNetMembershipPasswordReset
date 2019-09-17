using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Reflection;
using System.Text;
using System.Web.Security;
using Arvy;
using Databossy;

namespace AspNetMembershipPasswordReset {
    public static class ExceptionExt {
        public static String GetExceptionMessage(this Exception ex) {
            var errorList = new StringBuilder();
            if (ex.InnerException != null)
                errorList.AppendLine(GetExceptionMessage(ex.InnerException));

            return errorList
                .AppendLine(ex.Message)
                .AppendLine(ex.StackTrace)
                .ToString();
        }
    }

    class Program {
        static void Main(String[] args) {
            try {
                Func<String, String, SqlMembershipProvider> InitializeAndGetAspMembershipConfig = (connectionstring, appname) => {
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

                    return membershipProv;
                };

                Console.WriteLine(new StringBuilder()
                    .AppendLine()
                    .AppendLine("AspNet-Membership password reset tools")
                    .ToString());

                Console.WriteLine(new StringBuilder()
                    .AppendLine("Enter to continue, C or Q to exit.")
                    .AppendLine()
                    .ToString());
                String input = Console.ReadLine();

                while (!(input == "c" || input == "C" || input == "q" || input == "Q")) {

                    Console.Write("Connection String: ");
                    String connString = Console.ReadLine();

                    Console.Write("App Name: ");
                    String appName = Console.ReadLine();

                    Console.Write("Username: ");
                    String username = Console.ReadLine();

                    Console.Write("New Password: ");
                    String newPwd = Console.ReadLine();

                    Boolean valid = !String.IsNullOrEmpty(connString) && !String.IsNullOrWhiteSpace(connString) &&
                        !String.IsNullOrEmpty(appName) && !String.IsNullOrWhiteSpace(appName) &&
                        !String.IsNullOrEmpty(username) && !String.IsNullOrWhiteSpace(username) &&
                        !String.IsNullOrEmpty(newPwd) && !String.IsNullOrWhiteSpace(newPwd);

                    if (valid) {
                        SqlMembershipProvider provider = InitializeAndGetAspMembershipConfig(connString, appName);
                        MembershipUser user = provider.GetUser(username, false);
                        if (user == null)
                            throw new InvalidOperationException("User not found.");

                        Console.WriteLine($"User '{username}' found.");

                        String reset = provider.ResetPassword(username, null);
                        Boolean changed = provider.ChangePassword(username, reset, newPwd);

                        using (var db = new Database(connString, true)) {
                            String result = db.NQueryScalar<String>(@"
                                BEGIN
                                    SET NOCOUNT ON
                                    BEGIN TRAN ResetPwd

                                    BEGIN TRY
                                        DECLARE
                                            @@message VARCHAR(MAX)

                                        UPDATE [dbo].[aspnet_Membership] SET
                                        [IsApproved] = '1',
                                        [IsLockedOut] = '0',
                                        [LastLoginDate] = DATEADD(DAY, -2, GETDATE()),
                                        [LastPasswordChangedDate] = DATEADD(DAY, -2, GETDATE())
                                        WHERE UserId IN (SELECT [UserId]
                                            FROM [dbo].[aspnet_Users]
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
                                END
                            ", new { Username = username });
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
                        .AppendLine("Enter to continue, C or Q to exit.")
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
