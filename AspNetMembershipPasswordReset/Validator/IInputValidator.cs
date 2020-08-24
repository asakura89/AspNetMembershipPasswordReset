using System;

namespace AspNetMembershipPasswordReset.Validator {
    public interface IInputValidator {
        String InvalidMessage { get; }
        Boolean Validate(String value);
    }
}