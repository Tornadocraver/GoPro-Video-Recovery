using Microsoft.Win32;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace GoPro_Video_Recovery
{
    public class MainPageViewModel : ViewModelBase
    {
        #region Commands
        public ICommand BrowseCommand
        {
            get { return new DelegateCommand(Browse); }
        }
        public ICommand RecoverCommand
        {
            get { return new DelegateCommand(Recover); }
        }

        private void Browse(object context)
        {
            if (!Running)
            {
                Dialog.Multiselect = true;
                Dialog.FileName = string.Empty;
                Dialog.Title = "Select Corrupt Video(s)";
                bool? result = Dialog.ShowDialog();
                if (!result.HasValue || !result.Value)
                {
                    Status = "Waiting for file selection.";
                    Valid = false;
                    return;
                }
                BadPaths = Dialog.FileNames;
                Dialog.Multiselect = false;
                Dialog.InitialDirectory = Path.GetDirectoryName(Dialog.FileName);
                Dialog.FileName = string.Empty;
                Dialog.Title = "Select A Sample Video";
                result = Dialog.ShowDialog();
                if (!result.HasValue || !result.Value)
                {
                    Status = "Waiting for file selection.";
                    Valid = false;
                    return;
                }
                GoodPath = Dialog.FileName;
                Status = "Ready.";
                Valid = true;
            }
        }
        private void Recover(object context)
        {
            if (Valid)
            {
                if (!Running)
                {
                    Status = "Starting...";
                    CancellationToken = new CancellationTokenSource();
                    Task.Run(() => RecoverVideos(CancellationToken.Token), CancellationToken.Token).ContinueWith(async (task) => 
                    {
                        if (Errors.Count > 0)
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                ProgressColor = (SolidColorBrush)(new BrushConverter().ConvertFrom("#99FF0000"));
                                ErrorDisplay errors = new ErrorDisplay(Errors);
                                errors.ShowDialog();
                            });
                        await Task.Delay(2500);
                        Application.Current.Dispatcher.Invoke(() => { Progress = 0.0; Status = "Waiting for file selection."; StartText = "RECOVER"; BadPaths = null; Errors.Clear(); GoodPath = string.Empty; ProgressColor = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FF008000")); Running = false; Valid = false; });
                        File.Delete(Path.Combine(Environment.CurrentDirectory, "audio.hdr"));
                        File.Delete(Path.Combine(Environment.CurrentDirectory, "result.aac"));
                        File.Delete(Path.Combine(Environment.CurrentDirectory, "result.h264"));
                        File.Delete(Path.Combine(Environment.CurrentDirectory, "video.hdr"));
                        CancellationToken.Dispose();
                    });
                    StartText = "CANCEL";
                }
                else
                {
                    Status = "Cancelling...";
                    CancellationToken.Cancel(true);
                    ProgressColor = (SolidColorBrush)(new BrushConverter().ConvertFrom("#99FFFF00"));
                }
            }
        }
        #endregion

        #region Recovery
        private async Task RecoverVideos(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Application.Current.Dispatcher.Invoke(() => { Running = true; Progress = 25.0; Status = "Analyzing sample video..."; });
            int total = BadPaths.Count();
            ProcessResult analyze = await AnalyzeVideo(GoodPath, token);
            if (analyze.Success)
            {
                CommandSet commands = (CommandSet)analyze.Result;
                int i = 0;
                foreach (string video in BadPaths)
                {
                    i++;
                    token.ThrowIfCancellationRequested();
                    Application.Current.Dispatcher.Invoke(() => { Progress = 50.0; Status = $"Recovering video {i}/{total}..."; });
                    ProcessResult recover = await RecoverVideo(video, commands.RecoverCommand, token);
                    if (recover.Success)
                    {
                        token.ThrowIfCancellationRequested();
                        Application.Current.Dispatcher.Invoke(() => { Progress = 75.0; Status = $"Muxing video {i}/{total}..."; });
                        ProcessResult mux = await MuxVideo(video, commands.MuxCommand, token);
                        if (!mux.Success)
                            Errors.Add(new Error(ErrorStage.Muxing, string.Join($":{Environment.NewLine}", (string[])mux.Result), video));
                    }
                    else
                        Errors.Add(new Error(ErrorStage.Recovery, string.Join($":{Environment.NewLine}", (string[])recover.Result), video));
                }
            }
            else
                Errors.Add(new Error(ErrorStage.Analysis, string.Join($":{Environment.NewLine}", (string[])analyze.Result), GoodPath));
            int errors = total - Errors.Count;
            Application.Current.Dispatcher.Invoke(() => { Progress = 100.0; Status = $"Finished:  {errors} video{(total > 1 || errors == 0 ? "s" : string.Empty)} recovered."; Valid = false; });
        }

        private async Task<ProcessResult> AnalyzeVideo(string goodVideoPath, CancellationToken token)
        {
            try
            {
                StandardInfo.FileName = RecoverMP4Path;
                StandardInfo.Arguments = $"\"{goodVideoPath}\" --analyze";
                token.ThrowIfCancellationRequested();
                using (Process process = Process.Start(StandardInfo))
                {
                    token.ThrowIfCancellationRequested();
                    string line = await process.StandardOutput.ReadToEndAsync();
                    if (string.IsNullOrWhiteSpace(line))
                        return new ProcessResult(false, new string[] { "An error occurred while reading from the analysis process" });
                    string[] lines = line.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                    int startingIndex = lines.IndexOf("Now run the following command to start recovering:");
                    if (startingIndex < 0)
                        return new ProcessResult(false, new string[] { "An error occurred, and the analysis process did not produce the expected output" });
                    return new ProcessResult(true, new CommandSet(lines[startingIndex + 1], lines[startingIndex + 3]));
                }
            }
            catch (OperationCanceledException exception) { throw exception; }
            catch (Exception exception) { return new ProcessResult(false, new string[] { "An error occurred while launching the analysis process", exception.ToString() }); }
        }
        private async Task<ProcessResult> RecoverVideo(string badVideoPath, string recoverCommand, CancellationToken token)
        {
            try
            {
                StandardInfo.Arguments = recoverCommand.Substring(recoverCommand.IndexOf(' ') + 1).Replace("corrupted_file", $"\"{badVideoPath}\"");
                StandardInfo.FileName = RecoverMP4Path;
                token.ThrowIfCancellationRequested();
                using (Process process = Process.Start(StandardInfo))
                {
                    token.ThrowIfCancellationRequested();
                    string output = await process.StandardOutput.ReadToEndAsync();
                    if (string.IsNullOrWhiteSpace(output))
                        return new ProcessResult(false, new string[] { "An error occurred while reading from the recovery process" });
                    string lowOutput = output.ToLower();
                    if (lowOutput.Contains("error"))
                        return new ProcessResult(false, lowOutput.GetStrings(Environment.NewLine, "error").InsertAt(0, "An error occurred while recovering the video").ToArray());
                    return new ProcessResult(true);
                }
            }
            catch (OperationCanceledException exception) { throw exception; }
            catch (Exception exception) { return new ProcessResult(false, new string[] { "An error occurred while launching the recovery process", exception.ToString() }); }
        }
        private async Task<ProcessResult> MuxVideo(string badVideoPath, string muxCommand, CancellationToken token)
        {
            try
            {
                StandardInfo.Arguments = muxCommand.Substring(muxCommand.IndexOf(' ') + 1).Replace("result.mp4", $"\"{badVideoPath}\" -y"); //muxCommand = muxCommand.Substring(muxCommand.IndexOf(' ') + 1).Replace("result.mp4", $"\"{badVideoPath}\" -y"); StandardInfo.Arguments = muxCommand.Insert(muxCommand.IndexOf("-i"), "-fflags +genpts ");
                StandardInfo.FileName = FFmpegPath;
                token.ThrowIfCancellationRequested();
                using (Process process = Process.Start(StandardInfo))
                {
                    token.ThrowIfCancellationRequested();
                    string output = await process.StandardOutput.ReadToEndAsync();
                    if (string.IsNullOrWhiteSpace(output))
                        return new ProcessResult(false, new string[] { "An error occurred while reading from the muxing process" });
                    string lowOutput = output.ToLower();
                    if (lowOutput.Contains("error"))
                        return new ProcessResult(false, lowOutput.GetStrings(Environment.NewLine, "error").InsertAt(0, "An error occurred while muxing the video").ToArray());
                    return new ProcessResult(true);
                }
            }
            catch (OperationCanceledException exception) { throw exception; }
            catch (Exception exception) { return new ProcessResult(false, new string[] { "An error occurred while launching the muxing process", exception.ToString() }); }
        }
        #endregion

        #region Variables
        private string[] BadPaths;
        private CancellationTokenSource CancellationToken;
        private OpenFileDialog Dialog = new OpenFileDialog()
        {
            AddExtension = true,
            CheckFileExists = true,
            CheckPathExists = true,
            DefaultExt = "*.mp4",
            DereferenceLinks = true,
            Filter = "Videos (*.mp4)|*.mp4",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
            Multiselect = true,
            ShowReadOnly = true,
            ValidateNames = true
        };
        private SolidColorBrush progressColor = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FF008000"));
        public SolidColorBrush ProgressColor
        {
            get { return progressColor; }
            set { progressColor = value; OnPropertyChanged("ProgressColor"); }
        }
        private readonly string ExecutableDirectory = Path.Combine(Environment.CurrentDirectory, "Executables");
        public ObservableCollection<Error> Errors = new ObservableCollection<Error>();
        private readonly string FFmpegPath = Path.Combine(Environment.CurrentDirectory, "Executables", $"ffmpeg.exe");  //{(Environment.Is64BitOperatingSystem ? "_x86" : "_x86")}
        private string GoodPath = string.Empty;
        private double progress = 0.0;
        public double Progress
        {
            get { return progress; }
            set { progress = value; OnPropertyChanged("Progress"); }
        }
        private readonly string RecoverMP4Path = Path.Combine(Environment.CurrentDirectory, "Executables", $"recover_mp4.exe");  //{(Environment.Is64BitOperatingSystem ? "_x86" : "_x86")}
        private bool running = false;
        public bool Running
        {
            get { return running; }
            set { running = value; OnPropertyChanged("Running"); }
        }
        private ProcessStartInfo StandardInfo = new ProcessStartInfo()
        {
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            WindowStyle = ProcessWindowStyle.Hidden
        };
        private string startText = "RECOVER";
        public string StartText
        {
            get { return startText; }
            set { startText = value; OnPropertyChanged("StartText"); }
        }
        private string status = "Waiting for file selection.";
        public string Status
        {
            get { return status; }
            set { status = value; OnPropertyChanged("Status"); }
        }
        private bool valid = false;
        public bool Valid
        {
            get { return valid; }
            set { valid = value; OnPropertyChanged("Valid"); }
        }
        #endregion
    }

    public class BooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(bool)value;
        }
    }

    static class ExtensionMethods
    {
        public static List<string> GetStrings(this string input, string delimeter, string searchPattern)
        {
            return input.Split(new string[] { delimeter }, StringSplitOptions.RemoveEmptyEntries).GetStrings(searchPattern);
        }

        public static List<string> GetStrings(this IList<string> list, string searchPattern)
        {
            List<string> input = list.ToList();
            List<string> matches = new List<string>();
            for (int i = IndexOf(input, searchPattern); i > -1; i = IndexOf(input, searchPattern))
            {
                matches.Add(input[i]);
                input.Remove(input[i]);
            }
            return matches;
        }

        public static int IndexOf(this IList<string> list, string searchPattern)
        {
            for (int i = 0; i < list.Count; i++)
            {
                string searchable = list[i];
                if (!string.IsNullOrWhiteSpace(searchable) && searchable.Contains(searchPattern))
                    return i;
            }
            return -1;
        }

        public static List<string> InsertAt(this List<string> input, int index, string value)
        {
            input.Insert(index, value);
            return input;
        }
    }
}