namespace GoPro_Video_Recovery
{
    public class CommandSet
    {
        public bool HasCommands { get; private set; }
        public string MuxCommand { get; private set; }
        public string RecoverCommand { get; private set; }

        public CommandSet()
        {
            HasCommands = false;
        }
        public CommandSet(string recoverCommand, string muxCommand)
        {
            HasCommands = true;
            MuxCommand = muxCommand;
            RecoverCommand = recoverCommand;
        }
    }
}