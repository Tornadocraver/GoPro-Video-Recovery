namespace GoPro_Video_Recovery
{
    public class ProcessResult
    {
        public object Result { get; private set; }
        public bool Success { get; private set; }

        public ProcessResult(bool success)
        {
            Success = success;
        }
        public ProcessResult(bool success, object result) : this(success)
        {
            Result = result;
        }
    }
}