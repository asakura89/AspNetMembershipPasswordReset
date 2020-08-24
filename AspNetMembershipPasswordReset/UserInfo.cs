using System;

namespace AspNetMembershipPasswordReset {
    public class UserInfo {
        public String App { get; set; }
        public String AppDesc { get; set; }
        public String Username { get; set; }
        public String LastActivity { get; set; }
        public String Email { get; set; }
        public String Approved { get; set; }
        public String LockedOut { get; set; }
        public String LastLogin { get; set; }
        public String LastPwdChanged { get; set; }
        public String LastLockedOut { get; set; }
        public String FailedLoginCount { get; set; }
        public String FailedPwdAnswerCount { get; set; }
        public String Role { get; set; }
        public String RoleDesc { get; set; }
        public String ProfileNames { get; set; }
        public String ProfileValues { get; set; }
    }
}
