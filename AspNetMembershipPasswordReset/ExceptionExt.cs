using System;
using System.Text;

namespace Exy {
    public static class ExceptionExt {
        public static String GetExceptionMessage(this Exception ex) {
            var errorList = new StringBuilder();
            Exception current = ex;
            while (current != null) {
                errorList
                    .AppendLine($"Exception: {current.GetType().FullName}")
                    .AppendLine($"Message: {current.Message}")
                    .AppendLine($"Source: {current.Source}")
                    .AppendLine(current.StackTrace)
                    .AppendLine();

                current = current.InnerException;
            }

            return errorList.ToString();
        }
    }
}