using System;
using System.Linq;

namespace AspNetMembershipPasswordReset.Validator {
    class HashAlgoValidator : IInputValidator {
        public HashAlgoValidator(String invalidMessage) {
            InvalidMessage = invalidMessage;
        }

        public String InvalidMessage { get; }
        public Boolean Validate(String value) =>
            !String.IsNullOrEmpty(value.Trim(' ')) &&
            !new[] { "md5", "sha1", "sha512" }.Contains(value.Trim(' ').ToLowerInvariant());
    }
}
