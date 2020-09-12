using System;
using ConsoleApp.Validator;

namespace ConsoleApp {
    public class ExtConsole {
        ExtConsole() { }

        public static ExtConsole Create() => new ExtConsole();

        String label;

        public ExtConsole LabelWith(String label) {
            if (String.IsNullOrEmpty(this.label))
                this.label = label;

            return this;
        }

        public String GetString(IInputValidator validator = null) {
            if (String.IsNullOrEmpty(label))
                throw new InvalidOperationException("Needs a label.");

            Console.Write(label);
            String value = Console.ReadLine();
            if (validator == null)
                return value;

            while (!validator.Validate(value)) {
                Console.WriteLine(validator.InvalidMessage);
                Console.Write(label);
                value = Console.ReadLine();
            }

            return value;
        }

        public Int32 GetInt(IInputValidator validator = null) {
            if (String.IsNullOrEmpty(label))
                throw new InvalidOperationException("Needs a label.");

            Console.Write(label);
            String stringValue = Console.ReadLine();
            Boolean valid = Int32.TryParse(stringValue, out Int32 value);
            if (validator == null)
                return value;

            while (!valid && !validator.Validate(stringValue)) {
                Console.WriteLine(validator.InvalidMessage);
                Console.Write(label);
                stringValue = Console.ReadLine();
                valid = Int32.TryParse(stringValue, out value);
            }

            return value;
        }

        public Boolean GetBoolean(IInputValidator validator = null) {
            if (String.IsNullOrEmpty(label))
                throw new InvalidOperationException("Needs a label.");

            Console.Write(label);
            String stringValue = Console.ReadLine();
            Boolean valid = Boolean.TryParse(stringValue, out Boolean value);
            if (validator == null)
                return value;

            while (!valid && !validator.Validate(stringValue)) {
                Console.WriteLine(validator.InvalidMessage);
                Console.Write(label);
                stringValue = Console.ReadLine();
                valid = Boolean.TryParse(stringValue, out value);
            }

            return value;
        }
    }
}
