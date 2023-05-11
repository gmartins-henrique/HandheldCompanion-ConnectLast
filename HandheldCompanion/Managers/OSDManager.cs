﻿using ControllerCommon.Managers;
using HandheldCompanion.Controls;
using PrecisionTiming;
using RTSSSharedMemoryNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WinRT;
using static ControllerCommon.WinAPI;
using static HandheldCompanion.Platforms.HWiNFO;
using static PInvoke.Kernel32;

namespace HandheldCompanion.Managers
{
    public static class OSDManager
    {
        private static bool IsInitialized;
        private static short OverlayLevel;

        private static PrecisionTimer RefreshTimer;
        private static int RefreshInterval = 100;

        private static Dictionary<int, OSD> OnScreenDisplay = new();

        public static event InitializedEventHandler Initialized;
        public delegate void InitializedEventHandler();

        private const string Header = "<C0=008040><C1=0080C0><C2=C08080><C3=FF0000><C4=FFFFFF><C250=FF8000><A0=-4><A1=5><A2=-2><A3=-3><A4=-4><A5=-5><S0=-50><S1=50>";
        private static List<string> Content;

        static OSDManager()
        {
            SettingsManager.SettingValueChanged += SettingsManager_SettingValueChanged;

            PlatformManager.RTSS.Hooked += RTSS_Hooked;
            PlatformManager.RTSS.Unhooked += RTSS_Unhooked;

            // timer used to monitor foreground application framerate
            RefreshInterval = SettingsManager.GetInt("OnScreenDisplayRefreshRate");

            RefreshTimer = new PrecisionTimer();
            RefreshTimer.SetAutoResetMode(true);
            RefreshTimer.SetResolution(0);
            RefreshTimer.SetPeriod(RefreshInterval);
            RefreshTimer.Tick += UpdateOSD;
        }

        private static void RTSS_Unhooked(int processId)
        {
            // clear previous display
            if (OnScreenDisplay.TryGetValue(processId, out var OSD))
            {
                OSD.Update(string.Empty);
                OSD.Dispose();
            }

            OnScreenDisplay.Remove(processId);
        }

        private static void RTSS_Hooked(int processId)
        {
            ProcessEx processEx = ProcessManager.GetProcess(processId);
            if (processEx is null)
                return;

            OnScreenDisplay[processId] = new(processEx.Title);
        }

        public static void Start()
        {
            IsInitialized = true;
            Initialized?.Invoke();

            LogManager.LogInformation("{0} has started", "OSDManager");
        }

        private static void UpdateOSD(object? sender, EventArgs e)
        {
            if (OverlayLevel == 0)
                return;

            foreach (var pair in OnScreenDisplay)
            {
                int processId = pair.Key;
                OSD processOSD = pair.Value;

                try
                {
                    string content = Draw(processId);
                    processOSD.Update(content);
                }
                catch (FileNotFoundException) { }
            }
        }

        public static string Draw(int processId)
        {
            SensorElement sensor;
            Content = new()
            {
                Header
            };

            switch (OverlayLevel)
            {
                default:
                case 0:
                    break;

                case 1:
                    {
                        OverlayRow row1 = new();

                        OverlayEntry FPSentry = new("FPS");
                        FPSentry.elements.Add(new SensorElement()
                        {
                            Value = PlatformManager.RTSS.GetInstantaneousFramerate(processId),
                            szUnit = "FPS"
                        });
                        row1.entries.Add(FPSentry);

                        Content.Add(row1.ToString());
                    }
                    break;

                case 2:
                    {
                        OverlayRow row1 = new();

                        OverlayEntry BATTentry = new("BATT");
                        if (PlatformManager.HWiNFO.MonitoredSensors.TryGetValue(SensorElementType.BatteryChargeLevel, out sensor))
                            BATTentry.elements.Add(sensor);
                        if (PlatformManager.HWiNFO.MonitoredSensors.TryGetValue(SensorElementType.BatteryRemainingCapacity, out sensor))
                            BATTentry.elements.Add(sensor);
                        row1.entries.Add(BATTentry);

                        OverlayEntry GPUentry = new("GPU");
                        if (PlatformManager.HWiNFO.MonitoredSensors.TryGetValue(SensorElementType.GPUUsage, out sensor))
                            GPUentry.elements.Add(sensor);
                        if (PlatformManager.HWiNFO.MonitoredSensors.TryGetValue(SensorElementType.GPUPower, out sensor))
                            GPUentry.elements.Add(sensor);
                        row1.entries.Add(GPUentry);

                        OverlayEntry CPUentry = new("CPU");
                        if (PlatformManager.HWiNFO.MonitoredSensors.TryGetValue(SensorElementType.CPUUsage, out sensor))
                            CPUentry.elements.Add(sensor);
                        if (PlatformManager.HWiNFO.MonitoredSensors.TryGetValue(SensorElementType.CPUPower, out sensor))
                            CPUentry.elements.Add(sensor);
                        row1.entries.Add(CPUentry);

                        OverlayEntry RAMentry = new("RAM");
                        if (PlatformManager.HWiNFO.MonitoredSensors.TryGetValue(SensorElementType.PhysicalMemoryUsage, out sensor))
                            RAMentry.elements.Add(sensor);
                        row1.entries.Add(RAMentry);

                        OverlayEntry FPSentry = new("FPS");
                        FPSentry.elements.Add(new SensorElement()
                        {
                            Value = PlatformManager.RTSS.GetInstantaneousFramerate(processId),
                            szUnit = "FPS"
                        });
                        row1.entries.Add(FPSentry);

                        Content.Add(row1.ToString());
                    }
                    break;

                case 3:
                    {
                        OverlayRow row1 = new();
                        OverlayRow row2 = new();
                        OverlayRow row3 = new();
                        OverlayRow row4 = new();
                        OverlayRow row5 = new();
                        OverlayRow row6 = new();

                        OverlayEntry GPUentry = new("GPU", true);
                        if (PlatformManager.HWiNFO.MonitoredSensors.TryGetValue(SensorElementType.GPUUsage, out sensor))
                            GPUentry.elements.Add(sensor);
                        if (PlatformManager.HWiNFO.MonitoredSensors.TryGetValue(SensorElementType.GPUPower, out sensor))
                            GPUentry.elements.Add(sensor);
                        if (PlatformManager.HWiNFO.MonitoredSensors.TryGetValue(SensorElementType.GPUTemperature, out sensor))
                            GPUentry.elements.Add(sensor);
                        row1.entries.Add(GPUentry);

                        OverlayEntry CPUentry = new("CPU", true);
                        if (PlatformManager.HWiNFO.MonitoredSensors.TryGetValue(SensorElementType.CPUUsage, out sensor))
                            CPUentry.elements.Add(sensor);
                        if (PlatformManager.HWiNFO.MonitoredSensors.TryGetValue(SensorElementType.CPUPower, out sensor))
                            CPUentry.elements.Add(sensor);
                        if (PlatformManager.HWiNFO.MonitoredSensors.TryGetValue(SensorElementType.CPUTemperature, out sensor))
                            CPUentry.elements.Add(sensor);
                        row2.entries.Add(CPUentry);

                        OverlayEntry RAMentry = new("RAM", true);
                        if (PlatformManager.HWiNFO.MonitoredSensors.TryGetValue(SensorElementType.PhysicalMemoryUsage, out sensor))
                            RAMentry.elements.Add(sensor);
                        row3.entries.Add(RAMentry);

                        OverlayEntry VRAMentry = new("VRAM", true);
                        if (PlatformManager.HWiNFO.MonitoredSensors.TryGetValue(SensorElementType.GPUMemoryUsage, out sensor))
                            VRAMentry.elements.Add(sensor);
                        row4.entries.Add(VRAMentry);

                        OverlayEntry BATTentry = new("BATT", true);
                        if (PlatformManager.HWiNFO.MonitoredSensors.TryGetValue(SensorElementType.BatteryChargeLevel, out sensor))
                            BATTentry.elements.Add(sensor);
                        if (PlatformManager.HWiNFO.MonitoredSensors.TryGetValue(SensorElementType.BatteryRemainingCapacity, out sensor))
                            BATTentry.elements.Add(sensor);
                        if (PlatformManager.HWiNFO.MonitoredSensors.TryGetValue(SensorElementType.BatteryRemainingTime, out sensor))
                            BATTentry.elements.Add(sensor);
                        row5.entries.Add(BATTentry);

                        OverlayEntry FPSentry = new("FPS", true);
                        FPSentry.elements.Add(new SensorElement()
                        {
                            Value = PlatformManager.RTSS.GetInstantaneousFramerate(processId),
                            szUnit = "FPS"
                        });
                        row6.entries.Add(FPSentry);

                        Content.Add(row1.ToString());
                        Content.Add(row2.ToString());
                        Content.Add(row3.ToString());
                        Content.Add(row4.ToString());

                        // not all devices have a battery
                        string row5str = row5.ToString();
                        if (!string.IsNullOrEmpty(row5str))
                            Content.Add(row5.ToString());

                        Content.Add(row6.ToString());
                    }
                    break;
            }

            return string.Join("\n", Content);
        }

        public static void Stop()
        {
            if (!IsInitialized)
                return;

            RefreshTimer.Stop();

            // unhook all processes
            foreach (int processId in OnScreenDisplay.Keys)
                RTSS_Unhooked(processId);

            IsInitialized = false;

            LogManager.LogInformation("{0} has stopped", "OSDManager");
        }

        private static void SettingsManager_SettingValueChanged(string name, object value)
        {
            switch (name)
            {
                case "OnScreenDisplayLevel":
                    {
                        OverlayLevel = Convert.ToInt16(value);

                        if (OverlayLevel >= 0)
                        {
                            RefreshTimer.Start();
                        }
                        else
                        {
                            RefreshTimer.Stop();

                            // clear UI on stop
                            foreach (var pair in OnScreenDisplay)
                            {
                                OSD processOSD = pair.Value;
                                processOSD.Update(string.Empty);
                            }
                        }
                    }
                    break;

                case "OnScreenDisplayRefreshRate":
                    {
                        RefreshInterval = Convert.ToInt32(value);

                        if (RefreshTimer.IsRunning())
                        {
                            RefreshTimer.Stop();
                            RefreshTimer.SetPeriod(RefreshInterval);
                            RefreshTimer.Start();
                        }
                    }
                    break;
            }
        }
    }

    public class OverlayEntry : IDisposable
    {
        public List<SensorElement> elements = new();
        public string Name { get; set; }

        public OverlayEntry(string name, bool indent = false)
        {
            this.Name = indent ? name + "\t" : name;
        }

        public void Dispose()
        {
            elements.Clear();
            elements = null;
        }
    }

    public class OverlayRow : IDisposable
    {
        public List<OverlayEntry> entries = new();

        public override string ToString()
        {
            List<string> rowStr = new();

            foreach (OverlayEntry entry in entries)
            {
                if (entry.elements is null || entry.elements.Count == 0)
                    continue;

                List<string> entriesStr = new() { entry.Name };

                foreach(SensorElement element in entry.elements)
                    entriesStr.Add(element.ToString());

                var ItemStr = string.Join(" ", entriesStr);
                rowStr.Add(ItemStr);
            }

            return string.Join(" | ", rowStr);
        }

        public void Dispose()
        {
            entries.Clear();
            entries = null;
        }
    }
}
