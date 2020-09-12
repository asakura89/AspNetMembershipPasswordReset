using System;
using System.Linq;
using ConsoleApp.Validator;

namespace AspNetMembershipPasswordReset.Validator {
    class ModeValidator : IInputValidator {
        public ModeValidator(String invalidMessage) {
            InvalidMessage = invalidMessage;
        }

        public String InvalidMessage { get; }
        public Boolean Validate(String value) =>
            !String.IsNullOrEmpty(value.Trim(' ')) &&
            new[] { "r", "c", "d", "v", "l" }.Contains(value.Trim(' ').ToLowerInvariant());
    }
}
