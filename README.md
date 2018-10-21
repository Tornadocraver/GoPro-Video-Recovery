# GoPro Video Recovery
This repository contains a GUI application which can be used to recover corrupt MP4 (.mp4) videos. It is based on the freeware versions of <a href="https://www.videohelp.com/software/recover-mp4-to-h264">recover_mp4.exe</a> and <a href="https://ffmpeg.zeranoe.com/builds/">ffmpeg.exe</a>. Both of these programs are command line tools, so this project combines, automates, and simpifies the process of using them.
### Latest Build
<a href="https://github.com/Tornadocraver/GoPro-Video-Recovery/releases/latest">Windows (32-bit)</a>
### Usage
1. Download the zip file and extract its contents

2. Run the executable file (as an Administrator if you don't have read/write permissions)

3. Click the `BROWSE` button

4. Select the corrupt video(s) you would like to recover and click `Open`

5. Select a working sample video (from the same camera) and click `Open`

6. Click the `RECOVER` button and wait until the process has finished*

7. Test the recovered video(s) by opening them in a media player
### Things to Note
* Errors that occur during the recovery process will be logged and displayed at the end.

* Testing has only been conducted on videos from the GoPro Hero 4, but, as the recover_mp4 documentation states, it should work with any corrupt MP4.

* The recover_mp4 tool may not be able to recover some videos (i.e. if they are filled with empty [00] bytes)

* This project may have undiscovered flaws, so I strongly recommend backing up your video files to prevent data loss. If you run into a problem, please open an issue on the <a href="https://github.com/Tornadocraver/GoPro-Video-Recovery/issues">Issues</a> page.
