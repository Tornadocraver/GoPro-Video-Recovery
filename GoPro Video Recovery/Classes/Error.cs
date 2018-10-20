using System;

namespace GoPro_Video_Recovery
{
    public class Error
    {
        public Exception Exception { get; private set; }
        public string FileName { get; private set; } = "N/A";
        public string Path { get; private set; } = "N/A";
        public string Reason { get; private set; } = "N/A";
        public ErrorStage Stage { get; private set; }

        public Error(ErrorStage stage, string reason)
        {
            Stage = stage;
            Reason = reason;
        }
        public Error(ErrorStage stage, string reason, string file) : this(stage, reason)
        {
            Path = file;
            FileName = System.IO.Path.GetFileName(file);
        }
        public Error(ErrorStage stage, string reason, string file, Exception exception) : this(stage, reason, file)
        {
            Exception = exception;
        }
    }
}