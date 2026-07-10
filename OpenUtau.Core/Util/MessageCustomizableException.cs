using System;

namespace OpenUtau.Core {
    public class MessageCustomizableException : Exception {

        public override string Message { get; } = string.Empty;
        public string TranslatableMessage { get; set; } = string.Empty;
        public Exception SubstanceException { get; }
        public bool ShowStackTrace { get; } = true;
        public object[]? Replaces { get; }
        public bool SuggestPackageManager { get; }
        public string? SuggestDownloadUrl { get; }

        /// <summary>
        /// This allows the use of translatable messages and the hiding of stack traces in the message box.
        /// <summary>
        /// <paramref name="message">untranslated message</paramref>
        /// <paramref name="translatableMessage">By enclosing the resource key with a tag like "<translate:key>", only that part will be translated.</paramref>
        /// <paramref name="e">underlying exception</paramref>
        /// <paramref name="showStackTrace">Can be omitted. Default is true.</paramref>
        public MessageCustomizableException(
                string message,
                string translatableMessage,
                Exception e,
                bool showStackTrace = true,
                object[]? replaces = null,
                bool suggestPackageManager = false,
                string? suggestDownloadUrl = null) {
            if (e is MessageCustomizableException mce) {
                Message = mce.Message;
                TranslatableMessage = mce.TranslatableMessage;
                SubstanceException = mce.SubstanceException;
                ShowStackTrace = mce.ShowStackTrace;
                Replaces = mce.Replaces;
                SuggestPackageManager = mce.SuggestPackageManager;
                SuggestDownloadUrl = mce.SuggestDownloadUrl;
            } else {
                Message = message;
                TranslatableMessage = translatableMessage;
                SubstanceException = e;
                ShowStackTrace = showStackTrace;
                Replaces = replaces;
                SuggestPackageManager = suggestPackageManager;
                SuggestDownloadUrl = suggestDownloadUrl;
            }
        }

        public override string ToString() {
            return SubstanceException.Message;
        }
    }
}
