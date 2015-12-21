﻿using DataModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace Phrike.GroundControl.Helper
{
    public class ScreenCaptureHelper
    {
        private static ScreenCaptureHelper screenRercorder;
        private Process gameProcess;
        private Process cameraProcess;

        public Boolean IsRunningGame { get; set; }
        public Boolean IsRunningCamera { get; set; }
        public String CameraConfig { get; set; }
        public String GameConfig { get; set; }

        private ScreenCaptureHelper()
        {
            // Private singleton constructor
            LoadConfig();
            IsRunningGame = false;
            IsRunningCamera = false;
        }

        public static ScreenCaptureHelper GetInstance()
        {
            if (screenRercorder == null)
            {
                //Create new instance if not available
                screenRercorder = new ScreenCaptureHelper();
            }
            return screenRercorder;
        }

        public void LoadConfig()
        {
            Settings.LoadSettings();
            GameConfig = Settings.RecordingGameConfig;
            CameraConfig = Settings.RecordingCameraConfig;
        }

        public void StartRecording(int testId)
        {
            StartGameRecording("gameRecording.mkv", testId);
            StartCameraRecording("cameraRecording.mkv", testId);
        }

        public void StartCameraRecording(String cameraFilename, int testId)
        {
            if (IsRunningCamera)
            {
                return;
            }

            if (StartProcessTask(ref cameraProcess, CameraConfig, cameraFilename, testId))
            {
                IsRunningCamera = true;
                Console.WriteLine(cameraProcess.Id);
            }
        }

        public void StartGameRecording(String gameFilename, int testId)
        {
            if (IsRunningGame)
            {
                return;
            }

            if (StartProcessTask(ref gameProcess, GameConfig, gameFilename, testId))
            {
                IsRunningGame = true;
                Console.WriteLine(gameProcess.Id);
            }

        }

        private bool StartProcessTask(ref Process process, String config, String filename, int testId)
        {
            var aux = FileStorageHelper.ReserveFile(filename, AuxiliaryDataMimeTypes.AnyVideo, testId, DateTime.Now);
            String command = config + " \"" + PathHelper.PhrikeImport + "\\" +  aux.FilePath + "\"";
            process = new Process();
            process.StartInfo.FileName = "ffmpeg.exe";
            process.StartInfo.Arguments = command;
            process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            bool started = process.Start();
            Thread.Sleep(100);
            return started;
        }

        public void StopRecording()
        {
            StopGameRecording();
            StopCameraRecording();
        }

        public void StopCameraRecording()
        {
            if (IsRunningCamera)
            {
                if (StopProcess(ref cameraProcess))
                {
                    IsRunningCamera = false;
                }
            }
        }

        public void StopGameRecording()
        {
            if (IsRunningGame)
            {
                if (StopProcess(ref gameProcess))
                {
                    IsRunningGame = false;
                }
            }
        }

        private bool StopProcess(ref Process process)
        {
            Process stopProcess = new Process();
            stopProcess.StartInfo.FileName = "SendSignalCtrlC.exe";
            stopProcess.StartInfo.Arguments = process.Id.ToString();
            stopProcess.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            bool stopped = false;
            while (!process.HasExited)
            {
                stopped = stopProcess.Start();
                Thread.Sleep(1000);
            }
            return stopped;
        }
    }
}