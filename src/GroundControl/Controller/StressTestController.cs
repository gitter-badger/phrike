﻿using DataAccess;
using DataModel;
using NLog;
using Phrike.GroundControl.Helper;
using Phrike.GroundControl.ViewModels;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Phrike.GroundControl.Controller
{
    public class StressTestController
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        public static StressTestController Instance;

        private SubjectVM tempSubject;
        private ScenarioVM tempScenario;

        // View to display the status
        private StressTestViewModel stressTestViewModel;
        private NewStressTestViewModel newStressTestViewModel;

        private UnrealEngineController unrealEngineController;
        private SensorsController sensorsController;

        private UnitOfWork unitOfWork;

        private Test test;

        private string testName;
        private string testLocation;

        #region Constructor

        public StressTestController()
        {
            Instance = this;
            stressTestViewModel = StressTestViewModel.Instance;
            newStressTestViewModel = NewStressTestViewModel.Instance;
        }

        #endregion

        private void SetNewUnrealController()
        {
            // create the Unreal Engine communication object
            unrealEngineController = new UnrealEngineController(test);
            unrealEngineController.PositionReceived += (s, e) =>
            {
                test.PositionData.Add(e);
            };
            unrealEngineController.Ending += (s, e) =>
            {
                StopStressTest();
                newStressTestViewModel.ResetButtons();
                unitOfWork.Save();
                DisableUnrealEngineAndScreenCapturingColor();
                MainViewModel.Instance.PushViewModel(new AnalysisViewModel(test.Id));
            };
            unrealEngineController.Restarting += (s, e) =>
            {
                RestartStressTest(tempSubject, tempScenario);
                StopStressTest();
            };
            unrealEngineController.ErrorOccoured += (s, e) =>
            {
                StopStressTest();
                newStressTestViewModel.ResetButtons();
                DialogHelper.ShowErrorDialog("Fehler in der Simulation aufgetreten.");
                Logger.Error(e);
            };
        }

        public void StartStressTest(SubjectVM subject, ScenarioVM scenario)
        {
            tempSubject = subject;
            tempScenario = scenario;

            if (subject == null || scenario == null)
            {
                return;
            }

            CreateUnitOfWorkAndTest(subject, scenario);
            try
            {
                StartSensorsAndCapturing();
                StartUnrealEngineTask();
            }
            catch (Exception) { }
        }

        public void SetTestName(string name)
        {
            testName = name;
        }

        public void SetTestLocation(string location)
        {
            testLocation = location;
        }

        private void StartSensorsAndCapturing()
        {
            if (Settings.SelectedSensorType == Models.SensorType.GMobiLab)
            {
                StartSensorsTask().RunSynchronously();
            }
            if (Settings.ScreenRecordingEnabled)
            {
                StartScreenCaptureTask(test.Id);
            }
            if (Settings.WebcamRecordingEnabled)
            {
                StartWebcamCaptureTask(test.Id);
            }
        }

        private void RestartStressTest(SubjectVM subject, ScenarioVM scenario)
        {
            StopSensorsAndCapturing();
            CreateUnitOfWorkAndTest(subject, scenario);
            StartSensorsAndCapturing();

        }

        private void CreateUnitOfWorkAndTest(SubjectVM subject, ScenarioVM scenario)
        {
            unitOfWork = new UnitOfWork();
            test = new Test()
            {
                Subject = unitOfWork.SubjectRepository.GetByID(subject.Id),
                Scenario = unitOfWork.ScenarioRepository.GetByID(scenario.Id),
                Time = DateTime.Now,
                Title = testName,
                Location = testLocation
            };
            unitOfWork.TestRepository.Insert(test);
            unitOfWork.Save();
        }

        private void StopSensorsAndCapturing()
        {

            if (Settings.SelectedSensorType == Models.SensorType.GMobiLab)
            {
                StopSensorsTask();
            }
            if (Settings.ScreenRecordingEnabled)
            {
                StopScreenCaptureTask();
            }
            if (Settings.WebcamRecordingEnabled)
            {
                StopWebcamCaptureTask();
            }
        }

        public void StopStressTest()
        {
            StopUnrealEngineTask();
            StopSensorsAndCapturing();
        }

        #region Unreal Engine

        private Task StartUnrealEngineTask()
        {
            return Task.Run(() =>
            {
                SetNewUnrealController();
                // start the external application sub-process
                ProcessController.StartProcess(unrealEngineController.UnrealEnginePath, true, new string[] { "-fullscreen" });
                Logger.Info("Unreal Engine process started!");

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
                    return;
                }

                unrealEngineController.Close();
                unrealEngineController = null;
                ProcessController.StopProcess(unrealEngineController.UnrealEnginePath);
                Logger.Info("Unreal Engine process stoped!");
                DisableUnrealEngineAndScreenCapturingColor();
            });
        }

        #endregion

        #region Sensors

        public Task StartSensorsTask()
        {
            return Task.Run(() =>
            {
                if (sensorsController != null)
                {
                    const string message = "Could not start sensors recording! Recording task is already running.";
                    Logger.Warn(message);
                    return;
                }

                sensorsController = new SensorsController();
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
                    return;
                }
                sensorsController.Close();
                sensorsController = null;
                Logger.Info("Sensors recording successfully stopped!");
                stressTestViewModel.SensorStatusColor = GCColors.Disabled;
            });
        }

        #endregion

        #region Screen Capturing

        public Task StartScreenCaptureTask(int testId)
        {
            return Task.Run(() =>
            {
                ScreenCaptureHelper screenCapture = ScreenCaptureHelper.GetInstance();
                screenCapture.StartGameRecording(testId);
                if (!screenCapture.IsRunningGame)
                {
                    const string message = "Could not start screen recording!";
                    Logger.Warn(message);
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
                screenCapture.StopGameRecording();
                if (screenCapture.IsRunningGame)
                {
                    const string message = "Could not stop screen recording!";
                    Logger.Warn(message);
                    return;
                }
                Logger.Info("Screen Capture successfully stopped!");
                stressTestViewModel.ScreenCapturingStatusColor = GCColors.Disabled;
            });
        }

        #endregion

        #region Webcam

        public Task StartWebcamCaptureTask(int testId)
        {
            return Task.Run(() =>
            {
                ScreenCaptureHelper screenCapture = ScreenCaptureHelper.GetInstance();
                screenCapture.StartCameraRecording(testId);
                if (!screenCapture.IsRunningCamera)
                {
                    const string message = "Could not start screen recording!";
                    Logger.Warn(message);
                    return;
                }
                Logger.Info("Screen Capture successfully started!");
                stressTestViewModel.WebcamCapturingStatusColor = GCColors.Active;
            });
        }

        public Task StopWebcamCaptureTask()
        {
            return Task.Run(() =>
            {
                ScreenCaptureHelper screenCapture = ScreenCaptureHelper.GetInstance();
                screenCapture.StopCameraRecording();
                if (screenCapture.IsRunningCamera)
                {
                    const string message = "Could not stop screen recording!";
                    Logger.Warn(message);
                    return;
                }
                Logger.Info("Screen Capture successfully stopped!");
                stressTestViewModel.WebcamCapturingStatusColor = GCColors.Disabled;
            });
        }

        #endregion


        public void ApplicationCloseTask()
        {
            StopStressTest();
        }

        #region Callbacks

        internal void DisableUnrealEngineAndScreenCapturingColor()
        {
            stressTestViewModel.UnrealStatusColor = GCColors.Disabled;
            stressTestViewModel.ScreenCapturingStatusColor = GCColors.Disabled;
            stressTestViewModel.WebcamCapturingStatusColor = GCColors.Disabled;
        }

        #endregion
    }
}
