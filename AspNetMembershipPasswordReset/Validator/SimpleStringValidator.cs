using System;

namespace AspNetMembershipPasswordReset.Validator {
    public class SimpleStringValidator : IInputValidator {
        public SimpleStringValidator(String invalidMessage) {
            InvalidMessage = invalidMessage;
        }

        public String InvalidMessage { get; }
        public Boolean Validate(String value) => !String.IsNullOrEmpty(value.Trim(' '));
    }
}
