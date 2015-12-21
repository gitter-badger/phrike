﻿using DataAccess;
using DataModel;
using NLog;
using Phrike.GroundControl.Helper;
using Phrike.GroundControl.ViewModels;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Phrike.GroundControl.Controller
{
    public class StressTestController
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        // View to display the status
        private StressTestViewModel stressTestViewModel;

        private UnrealEngineController unrealEngineController;
        private SensorsController sensorsController;

        public StressTestController()
        {
            stressTestViewModel = StressTestViewModel.Instance;
            //unrealEngineModel.complete +=
        }

        public void StartStressTest(SubjectVM subject, ScenarioVM scenario)
        {
            if (subject == null || scenario == null)
            {
                return;
            }
            using (UnitOfWork unitOfWork = new UnitOfWork())
            {
                Test test = new Test()
                {
                    Subject = unitOfWork.SubjectRepository.GetByID(subject.Id),
                    Scenario = unitOfWork.ScenarioRepository.GetByID(scenario.Id),
                    Time = DateTime.Now,
                    Title = "Testrun - " + subject.FullName + " " + DateTime.Now,
                    Location = "Test"
                };
                unitOfWork.TestRepository.Insert(test);
                unitOfWork.Save();
                StartUnrealEngineTask(test);
                if (Settings.SelectedSensorType == Models.SensorType.GMobiLab)
                {
                    StartSensorsTask();
                }
                if (Settings.RecordingEnabled)
                {
                    StartScreenCaptureTask(test.Id);
                }
            }
        }


        public void StopStressTest()
        {
            StopUnrealEngineTask();
            if (Settings.SelectedSensorType == Models.SensorType.GMobiLab)
            {
                StopSensorsTask();
            }
            if (Settings.RecordingEnabled)
            {
                StopScreenCaptureTask();
            }
        }

        private Task StartUnrealEngineTask(Test test)
        {
            return Task.Run(() =>
            {
                // start the external application sub-process
                ProcessController.StartProcess(UnrealEngineController.UnrealEnginePath, true, new string[] { "-fullscreen" });
                Logger.Info("Unreal Engine process started!");
                // create the Unreal Engine communication object
                unrealEngineController = new UnrealEngineController(test);
                unrealEngineController.ErrorOccoured += (sender, args) => ShowStressTestError(args.Message);
                unrealEngineController.Ending += (sender, args) => DisableUnrealEngineAndScreenCapturingColor();

                Logger.Info("Unreal Engine is ready to use!");
                stressTestViewModel.UnrealStatusColor = GCColors.Active;
            });
        }

        public Task StopUnrealEngineTask()
        {
            return Task.Run(() =>
            {
                if (unrealEngineController == null)
                {
                    const string message = "Could not stop the Unreal Engine! No Unreal Engine instance active.";
                    Logger.Warn(message);
                    ShowStressTestError(message);
                    return;
                }

                unrealEngineController.Close();
                unrealEngineController = null;
                ProcessController.StopProcess(UnrealEngineController.UnrealEnginePath);
                Logger.Info("Unreal Engine process stoped!");
                DisableUnrealEngineAndScreenCapturingColor();
            });
        }

        public Task StartSensorsTask()
        {
            return Task.Run(() =>
            {
                if (sensorsController != null)
                {
                    const string message = "Could not start sensors recording! Recording task is already running.";
                    Logger.Warn(message);
                    ShowStressTestError(message);
                    return;
                }

                sensorsController = new SensorsController(ShowStressTestError);
                Logger.Info("Sensors instance created!");

                var active = sensorsController.StartRecording();
                if (!active)
                {
                    sensorsController = null;
                }
                else
                {
                    stressTestViewModel.SensorStatusColor = GCColors.Active;
                }
            });
        }

        public Task StopSensorsTask()
        {
            return Task.Run(() =>
            {
                if (sensorsController == null)
                {
                    const string message = "Could not stop sensors recording! No sensors recording instance enabled.";
                    Logger.Warn(message);
                    ShowStressTestError(message);
                    return;
                }
                sensorsController.Close();
                sensorsController = null;
                Logger.Info("Sensors recording successfully stopped!");
                stressTestViewModel.SensorStatusColor = GCColors.Disabled;
            });
        }

        public Task StartScreenCaptureTask(int testId)
        {
            return Task.Run(() =>
            {
                ScreenCaptureHelper screenCapture = ScreenCaptureHelper.GetInstance();
                screenCapture.StartRecording(testId);
                if(!screenCapture.IsRunningCamera || !screenCapture.IsRunningGame)
                {
                    const string message = "Could not start screen recording!";
                    Logger.Warn(message);
                    ShowStressTestError(message);
                    return;
                }
                Logger.Info("Screen Capture successfully started!");
                stressTestViewModel.ScreenCapturingStatusColor = GCColors.Active;
            });
        }

        public Task StopScreenCaptureTask()
        {
            return Task.Run(() =>
            {
                ScreenCaptureHelper screenCapture = ScreenCaptureHelper.GetInstance();
                screenCapture.StopRecording();
                if (screenCapture.IsRunningCamera || screenCapture.IsRunningGame)
                {
                    const string message = "Could not stop screen recording!";
                    Logger.Warn(message);
                    ShowStressTestError(message);
                    return;
                }
                Logger.Info("Screen Capture successfully stopped!");
                stressTestViewModel.ScreenCapturingStatusColor = GCColors.Disabled;
            });
        }

        public void ApplicationCloseTask()
        {
            StopSensorsTask();
            StopScreenCaptureTask();
            StopUnrealEngineTask();
        }

        #region Callbacks

        internal void DisableUnrealEngineAndScreenCapturingColor()
        {
            stressTestViewModel.UnrealStatusColor = GCColors.Disabled;
            stressTestViewModel.ScreenCapturingStatusColor = GCColors.Disabled;
        }

        private void ShowStressTestError(string message)
        {
            MainViewModel.Instance.ShowDialogMessage("Stress Test Error", message);
        }

        #endregion
    }
}