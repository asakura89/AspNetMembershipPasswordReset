using System;
using System.Linq;
using Exy;

namespace Arvy {
    [Serializable]
    public class ActionResponseViewModel {
        public const String Info = "I";
        public const String Warning = "W";
        public const String Error = "E";
        public const String Success = "S";
        public String ResponseType { get; set; }
        public String Message { get; set; }

        public override String ToString() => ToString(true);

        public String ToString(Boolean alwaysReturn) {
            if (!alwaysReturn && ResponseType == Error)
                throw new InvalidOperationException(Message);

            return ResponseType + "|" + Message;
        }
    }

    public static class ActionResponseExt {
        public static ActionResponseViewModel AsActionResponseViewModel(this String resultString, Boolean alwaysReturn = false) {
            String[] splittedResult = new[] { resultString.Substring(0, 1), resultString.Substring(2, resultString.Length - 2) };
            String[] responseTypeList = new[] { ActionResponseViewModel.Info, ActionResponseViewModel.Warning, ActionResponseViewModel.Error, ActionResponseViewModel.Success };
            if (!responseTypeList.Contains(splittedResult[0]))
                throw new ArgumentException("resultString is bad formatted.");

            var viewModel = new ActionResponseViewModel {
                ResponseType = splittedResult[0],
                Message = splittedResult[1]
            };

            if (!alwaysReturn && viewModel.ResponseType == ActionResponseViewModel.Error)
                throw new InvalidOperationException(viewModel.Message);

            return viewModel;
        }

        public static ActionResponseViewModel AsActionResponseViewModel(this Exception ex) =>
            new ActionResponseViewModel {
                ResponseType = ActionResponseViewModel.Error,
                Message = ex.GetExceptionMessage()
            };
    }
}