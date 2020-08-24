using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Security;
using Arvy;
using AspNetMembershipPasswordReset.Validator;
using Databossy;
using Exy;

namespace AspNetMembershipPasswordReset {
    public class ConsoleInterface {
        public void Run() {
            try
            {
                String response = Initialize();
                Boolean proceed = HandleResponse(response);

                while (proceed) {
                    ShowMenu();

                    Console.WriteLine(new StringBuilder()
                        .AppendLine("Enter to continue. C, E or Q to exit.")
                        .AppendLine()
                        .ToString());

                    proceed = HandleResponse(Console.ReadLine());
                }

                HandleExit();
            }
            catch (Exception ex) {
                HandleError(ex);
            }
        }

        public String Initialize() {
            Console.Title = "AspNet-Membership password reset tools";

            Console.WriteLine(new StringBuilder()
                .AppendLine()
                .AppendLine("AspNet-Membership password reset tools")
                .ToString());

            Console.WriteLine(new StringBuilder()
                .AppendLine("Enter to continue. C, E or Q to exit.")
                .AppendLine()
                .ToString());

            return Console.ReadLine();
        }

        readonly IDictionary<String, Action<String, String, String, String, String>> modeAction = new Dictionary<String, Action<String, String, String, String, String>> {
            ["r"] = ResetAction,
            ["c"] = CreateAction,
            ["d"] = DeleteAction
        };

        public void ShowMenu() {
            String mode = ExtConsole
                .Create()
                .LabelWith("Mode: (R for Reset, C for Create, D for Delete) ")
                .GetString(new ModeValidator("Choose one: R, C, D"));

            String connString = ExtConsole
                .Create()
                .LabelWith("Connection String: ")
                .GetString(new SimpleStringValidator("Same as the one from your app.config / web.config"));

            String appName = ExtConsole
                .Create()
                .LabelWith("App Name: ")
                .GetString(new SimpleStringValidator("Same as the one from your app.config / web.config"));

            String hashAlgo = ExtConsole
                .Create()
                .LabelWith("Hash Algo: (MD5, SHA1, SHA512) ")
                .GetString(new SimpleStringValidator("Choose one: MD5, SHA1, SHA512"));

            String username = ExtConsole
                .Create()
                .LabelWith("Username: ")
                .GetString(new SimpleStringValidator("Input Username you want to reset"));

            String pwd = ExtConsole
                .Create()
                .LabelWith("Password: ")
                .GetString(new SimpleStringValidator("Input new Password"));

            Action<String, String, String, String, String> action = modeAction[mode.ToLowerInvariant()];
            action(connString, appName, hashAlgo, username, pwd);

            Console.WriteLine(new StringBuilder()
                .AppendLine("Done.")
                .AppendLine()
                .ToString());
        }

        static void ResetAction(String connString, String appName, String hashAlgo, String username, String pwd) {
            SqlMembershipProvider provider = MembershipService.InitializeAndGetAspMembershipConfig(connString, appName, hashAlgo);
            MembershipUser user = provider.GetUser(username, false);
            if (user == null)
                throw new InvalidOperationException("User not found.");

            Console.WriteLine($"User '{username}' found.");

            String reset = provider.ResetPassword(username, null);
            provider.ChangePassword(username, reset, pwd);
            UpdateUserLoginProperty(connString, username);
        }

        static void CreateAction(String connString, String appName, String hashAlgo, String username, String pwd) {
            String email = ExtConsole
                .Create()
                .LabelWith("Email: ")
                .GetString(new SimpleEmailValidator("Email format is invalid"));

            Console.WriteLine("Roles:");
            using (var db = new Database(connString, true)) {
                db.Query<SimpleResult>(@"
                    BEGIN
                        SET NOCOUNT ON

                        SELECT RoleName Result
                        FROM dbo.aspnet_Roles

                        SET NOCOUNT OFF
                    END")
                    .ToList()
                    .ForEach(result => Console.WriteLine($"  - {result.Result}"));
            }

            String role = ExtConsole
                .Create()
                .LabelWith("Role: ")
                .GetString(new SimpleStringValidator("Choose one from above"));

            SqlMembershipProvider provider = MembershipService.InitializeAndGetAspMembershipConfig(connString, appName, hashAlgo);
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

            UpdateUserLoginProperty(connString, username);
        }

        static void UpdateUserLoginProperty(String connString, String username) {
            using (var db = new Database(connString, true))
            {
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
        }

        static void DeleteAction(String connString, String appName, String hashAlgo, String username, String pwd) {
            SqlMembershipProvider provider = MembershipService.InitializeAndGetAspMembershipConfig(connString, appName, hashAlgo);
            provider.DeleteUser(username, true);
        }

        public Boolean HandleResponse(String response) {
            String[] quitCommands = { "c", "e", "q" };
            return !quitCommands.Contains(response.ToLowerInvariant());
        }

        public void HandleExit() {
            Console.WriteLine(new StringBuilder()
                .AppendLine("Exit.")
                .AppendLine()
                .ToString());

            Console.ReadLine();
        }

        public void HandleError(Exception ex) {
            Console.WriteLine(new StringBuilder()
                .AppendLine("Error:")
                .AppendLine(ex.GetExceptionMessage())
                .AppendLine()
                .ToString());

            Console.ReadLine();
        }
    }
}
