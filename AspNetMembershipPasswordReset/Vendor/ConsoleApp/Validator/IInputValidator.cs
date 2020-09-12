using System;

namespace ConsoleApp.Validator {
    public interface IInputValidator {
        String InvalidMessage { get; }
        Boolean Validate(String value);
    }
}