using System;

namespace ConsoleApp.Validator {
    public class SimpleBooleanValidator : IInputValidator {
        readonly Func<String, Boolean> stringValidator;

        public SimpleBooleanValidator(String invalidMessage, Func<String, Boolean> stringValidator = null) {
            InvalidMessage = invalidMessage;
            if (stringValidator == null)
                this.stringValidator = value => new SimpleStringValidator(invalidMessage).Validate(value);
        }

        public String InvalidMessage { get; }
        public Boolean Validate(String value) {
            Boolean validString = stringValidator(value);
            if (!validString)
                return false;

            return Boolean.TryParse(value, out Boolean result);
        }
    }
}
