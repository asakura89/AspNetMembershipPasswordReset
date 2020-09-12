using System;
using System.Text.RegularExpressions;

namespace ConsoleApp.Validator {
    public class SimpleEmailValidator : IInputValidator {
        public SimpleEmailValidator(String invalidMessage) {
            InvalidMessage = invalidMessage;
        }

        const String ValidatePattern = "^[a-zA-Z0-9_-]+(?:\\.[a-zA-Z0-9_-]+)*@(?:[a-zA-Z0-9](?:[a-zA-Z0-9-]*[a-zA-Z0-9])?\\.)+[a-zA-Z0-9](?:[a-zA-Z0-9-]*[a-zA-Z0-9])?$";

        public String InvalidMessage { get; }
        public Boolean Validate(String value) =>
            !String.IsNullOrEmpty(value.Trim(' ')) &&
            Regex.IsMatch(value.Trim(' '), ValidatePattern, RegexOptions.Singleline | RegexOptions.Compiled);
    }
}
