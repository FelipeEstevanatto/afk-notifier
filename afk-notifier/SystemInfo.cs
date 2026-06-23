using System;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace AfkNotifier
{
    /// <summary>
    /// Informações de hardware do equipamento, lidas do Registro do Windows
    /// (HKLM\HARDWARE\DESCRIPTION\System\BIOS) — mesma origem usada pelo Windows
    /// para mostrar fabricante e modelo nas Informações do Sistema.
    /// </summary>
    internal static class SystemInfo
    {
        public static string GetMachineModel()
        {
            try
            {
                using RegistryKey? key = Registry.LocalMachine.OpenSubKey(
                    @"HARDWARE\DESCRIPTION\System\BIOS");

                if (key == null) return "";

                string manufacturer = Read(key, "SystemManufacturer");
                string product = Read(key, "SystemProductName");
                string family = Read(key, "SystemFamily");

                string model = !string.IsNullOrEmpty(product) ? product : family;

                // Prefixa o fabricante se ainda não estiver no nome do modelo
                if (!string.IsNullOrEmpty(manufacturer) &&
                    !model.StartsWith(manufacturer, StringComparison.OrdinalIgnoreCase))
                {
                    model = string.IsNullOrEmpty(model) ? manufacturer : $"{manufacturer} {model}";
                }

                return model.Trim();
            }
            catch
            {
                return ""; // Em caso de falha, não quebra o relatório
            }
        }

        private static string Read(RegistryKey key, string valueName) =>
            (key.GetValue(valueName) as string ?? "").Trim();

        /// <summary>Memória física do sistema via GlobalMemoryStatusEx (kernel32).</summary>
        public static MemoryStatus GetMemoryStatus()
        {
            var status = new MemoryStatus();
            try
            {
                var mem = new NativeMethods.MEMORYSTATUSEX
                {
                    dwLength = (uint)Marshal.SizeOf<NativeMethods.MEMORYSTATUSEX>()
                };

                if (NativeMethods.GlobalMemoryStatusEx(ref mem))
                {
                    const double GB = 1024.0 * 1024.0 * 1024.0;
                    status.TotalGb = mem.ullTotalPhys / GB;
                    status.AvailableGb = mem.ullAvailPhys / GB;
                    status.UsedGb = status.TotalGb - status.AvailableGb;
                    status.PercentUsed = (int)mem.dwMemoryLoad;
                }
            }
            catch { /* não quebra o relatório */ }

            return status;
        }

        /// <summary>Estado de energia/bateria via GetSystemPowerStatus (kernel32).</summary>
        public static PowerStatus GetPowerStatus()
        {
            var result = new PowerStatus();
            try
            {
                if (NativeMethods.GetSystemPowerStatus(out var sps))
                {
                    result.HasBattery = sps.BatteryFlag != 255 && (sps.BatteryFlag & 128) == 0;
                    result.BatteryPercent = sps.BatteryLifePercent == 255 ? -1 : sps.BatteryLifePercent;
                    result.OnAcPower = sps.ACLineStatus == 1;
                    result.Charging = (sps.BatteryFlag & 8) != 0;
                    result.Description = BuildPowerDescription(result);
                }
            }
            catch { /* não quebra o relatório */ }

            return result;
        }

        private static string BuildPowerDescription(PowerStatus p)
        {
            if (!p.HasBattery)
                return p.OnAcPower ? "Sem bateria (desktop)" : "Sem bateria";

            if (p.Charging) return "Carregando (na tomada)";
            return p.OnAcPower ? "Na tomada (não carregando)" : "Na bateria";
        }
    }

    internal class MemoryStatus
    {
        public double TotalGb { get; set; }
        public double UsedGb { get; set; }
        public double AvailableGb { get; set; }
        public int PercentUsed { get; set; }
    }

    internal class PowerStatus
    {
        public bool HasBattery { get; set; }
        public int BatteryPercent { get; set; } = -1;
        public bool OnAcPower { get; set; }
        public bool Charging { get; set; }
        public string Description { get; set; } = "—";
    }
}
