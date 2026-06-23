using System;
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
    }
}
