using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace GbpKiller
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("G-Buster Browser - Killer");
            Console.WriteLine("=========================");
            Console.WriteLine("");
            Console.WriteLine("Este aplicativo tentará remover os processos GbpSv.exe.");
            if (SystemInformation.BootMode == BootMode.Normal)
            {
                if (!UacHelper.IsProcessElevated)
                {
                    Console.WriteLine("Você precisa rodar este removedor com direitos administrativos.");
                    Console.WriteLine("Com o botão direito sobre o executável, escolha Executar como Administrador.");
                    Thread.Sleep(3000);
                    return;
                }


                Console.WriteLine("Este removedor fará alterações no registro e apagará o GbpSv.");
                Console.WriteLine("Execute por sua conta em risco. NO WARRANTY.");
                Console.Write("Seu computadore será reiniciado no modo seguro. Continuar? [S,N] ");
                if (Console.ReadKey(false).KeyChar.ToString().ToUpper() != "S")
                    return;
                var p = new ProcessStartInfo()
                {
                    FileName = "bcdedit",
                    Arguments = "/set {current} safeboot network",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                };
                Process.Start(p);
                using (var runOnce = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnce", true))
                {
                    runOnce.SetValue("*UndoSB", "bcdedit /deletevalue {current} safeboot", RegistryValueKind.String);
                    runOnce.SetValue("*GbpKiller", "cmd /c \"" + System.Reflection.Assembly.GetExecutingAssembly().Location + "\"");

                }

                p.FileName = "SHUTDOWN ";
                p.Arguments = "-r -f -t 07";
                Process.Start(p).WaitForExit();

                return;
            }



            Console.WriteLine("Este removedor fará alterações no registro e apagará o GbpSv.");
            Console.WriteLine("Execute por sua conta em risco. NO WARRANTY.");
            Console.Write("Todos os navegadores seão fechados. Deseja continuar? [S,N]");
            if (Console.ReadKey(false).KeyChar.ToString().ToUpper() != "S")
                return;
            Console.WriteLine();
            Console.WriteLine();

            MatarProcessos();
            RemoverPermissaoProcessos();
            MatarProcessos();
            RemoverRegistro();
            RemoverArquivos();

            Console.Write("Pressione qualquer tecla para reiniciar.");
            Console.ReadKey(true);
            Process.Start(new ProcessStartInfo()
            {
                FileName = "bcdedit",
                Arguments = "/deletevalue {current} safeboot"
            }).WaitForExit();
            Process.Start(new ProcessStartInfo()
            {
                FileName = "SHUTDOWN",
                Arguments = "-r -f -t 10",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true
            }).WaitForExit();

        }

        private static void RemoverPermissaoProcessos()
        {
            var everyone = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
            Console.Write("Removendo permissões Gbp... ");
            foreach (var dir in CaminhosGBP)
            {
                try
                {
                    var dirInfo = new DirectoryInfo(Environment.ExpandEnvironmentVariables(dir));
                    if (dirInfo.Exists)
                    {

                        foreach (var f in dirInfo.GetFiles())
                        {
                            if (Path.GetExtension(f.Name).ToLower() == ".exe")
                            {
                                var fs = new FileSecurity();
                                fs.SetOwner(new NTAccount(WindowsIdentity.GetCurrent().Name));

                                fs.AddAccessRule(new FileSystemAccessRule(everyone, FileSystemRights.ExecuteFile, AccessControlType.Deny));
                                f.SetAccessControl(fs);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex.Message);
                }
            }
            Console.WriteLine("OK.");
        }

        private static void RemoverArquivos()
        {
            Console.Write("Removendo arquivos do Gbp... ");
            foreach (var dir in CaminhosGBP)
            {
                try
                {
                    var dirInfo = new DirectoryInfo(Environment.ExpandEnvironmentVariables(dir));
                    if (dirInfo.Exists)
                    {
                        var ds = new DirectorySecurity();
                        ds.SetOwner(new NTAccount(WindowsIdentity.GetCurrent().Name));
                        dirInfo.SetAccessControl(ds);
                        dirInfo.Delete(true);
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex.Message);
                }
            }
            Console.WriteLine("OK.");
        }
        private static void RemoverRegistro()
        {
            Console.Write("Removendo chaves de registro Gbp... ");
            foreach (var chave in ChavesGBP)
            {
                var baseKeyStr = chave.Split('\\')[0];
                var subKey = string.Join("\\", chave.Split('\\').Skip(1).ToArray());
                var baseKey = Microsoft.Win32.Registry.ClassesRoot;

                switch (baseKeyStr)
                {
                    case "HKEY_CLASSES_ROOT":
                        baseKey = Microsoft.Win32.Registry.ClassesRoot;
                        break;
                    case "HKEY_LOCAL_MACHINE":
                        baseKey = Microsoft.Win32.Registry.LocalMachine;
                        break;
                    case "HKEY_CURRENT_USER":
                        baseKey = Microsoft.Win32.Registry.CurrentUser;
                        break;
                    case "HKEY_CURRENT_CONFIG":
                        baseKey = Microsoft.Win32.Registry.CurrentConfig;
                        break;
                    default:
                        throw new NotImplementedException();
                }

                try
                {
                    var rk = baseKey.OpenSubKey(subKey, true);
                    if (rk != null)
                    {
                        var rs = new RegistrySecurity();
                        rs.SetOwner(new NTAccount(WindowsIdentity.GetCurrent().Name));
                        rk.SetAccessControl(rs);
                        rk.Close();
                        baseKey.DeleteSubKeyTree(subKey);
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(subKey + ": " + ex.Message);
                }
            }

            Console.WriteLine("OK.");
        }

        private static void MatarProcessos()
        {
            Console.Write("Matando processos Gbp... ");
            var psExec = new Process();

            foreach (var exe in ExecutaveisGBP)
            {
                var procs = Process.GetProcessesByName(exe);
                foreach (var p in procs)
                {
                    try
                    {
                        p.Kill();
                    }
                    catch { }
                }
            }


            Console.WriteLine("OK.");
        }

        private static readonly string[] ChavesGBP = new string[] {
            @"HKEY_CLASSES_ROOT\GbIeh.GbExplorerPersistObj",
            @"HKEY_CLASSES_ROOT\GbIeh.GbExplorerPersistObj.1",
            @"HKEY_CLASSES_ROOT\GbiehCef.GbIehObj",
            @"HKEY_CLASSES_ROOT\GbiehCef.GbIehObj.1",
            @"HKEY_CLASSES_ROOT\GbiehCef.GbPluginObj",
            @"HKEY_CLASSES_ROOT\GbiehCef.GbPluginObj.1",
            @"HKEY_CLASSES_ROOT\GbiehUni.GbIehObj",
            @"HKEY_CLASSES_ROOT\GbiehUni.GbIehObj.1",
            @"HKEY_CLASSES_ROOT\GbiehUni.GbPluginObj",
            @"HKEY_CLASSES_ROOT\GbiehUni.GbPluginObj.1",
            @"HKEY_CLASSES_ROOT\Interface\{C41A1C0D-EA6C-11D4-B1B8-444553540003}",
            @"HKEY_CLASSES_ROOT\Interface\{C41A1C0D-EA6C-11D4-B1B8-444553540008}",
            @"HKEY_CLASSES_ROOT\TypeLib\{C41A1C01-EA6C-11D4-B1B8-444553540003}",
            @"HKEY_CLASSES_ROOT\TypeLib\{C41A1C01-EA6C-11D4-B1B8-444553540008}",
            @"HKEY_CLASSES_ROOT\Wow6432Node\CLSID\{32A5804C-50B2-4295-8252-C32751FE0008}",
            @"HKEY_CLASSES_ROOT\Wow6432Node\CLSID\{98C11555-BC81-40aa-A053-DAADC5630003}",
            @"HKEY_CLASSES_ROOT\Wow6432Node\CLSID\{C41A1C0E-EA6C-11D4-B1B8-444553540003}",
            @"HKEY_CLASSES_ROOT\Wow6432Node\CLSID\{C41A1C0E-EA6C-11D4-B1B8-444553540008}",
            @"HKEY_CLASSES_ROOT\Wow6432Node\CLSID\{E37CB5F0-51F5-4395-A808-5FA49E399003}",
            @"HKEY_CLASSES_ROOT\Wow6432Node\CLSID\{E37CB5F0-51F5-4395-A808-5FA49E399008}",
            @"HKEY_CLASSES_ROOT\Wow6432Node\Interface\{C41A1C0D-EA6C-11D4-B1B8-444553540003}",
            @"HKEY_CLASSES_ROOT\Wow6432Node\Interface\{C41A1C0D-EA6C-11D4-B1B8-444553540008}",
            @"HKEY_CLASSES_ROOT\Wow6432Node\TypeLib\{C41A1C01-EA6C-11D4-B1B8-444553540003}",
            @"HKEY_CLASSES_ROOT\Wow6432Node\TypeLib\{C41A1C01-EA6C-11D4-B1B8-444553540008}",
            @"HKEY_CLASSES_ROOT\Interface\{7827CCC3-0DEB-4CFB-911C-AA777C882003}",
            @"HKEY_CLASSES_ROOT\Interface\{7827CCC3-0DEB-4CFB-911C-AA777C882008}",
            @"HKEY_CLASSES_ROOT\Wow6432Node\Interface\{7827CCC3-0DEB-4CFB-911C-AA777C882003}",
            @"HKEY_CLASSES_ROOT\Wow6432Node\Interface\{7827CCC3-0DEB-4CFB-911C-AA777C882008}",
            @"HKEY_CURRENT_CONFIG\System\CurrentControlSet\Enum\ROOT\LEGACY_GBPSV",
            @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\GBPRCM",
            @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\GbpSv"
        };

        private static readonly string[] CaminhosGBP = new string[] {
            @"%PROGRAMFILES% (x86)\GbPlugin",
            @"%PROGRAMFILES%\GbPlugin",
            @"%PROGRAMFILES% (x86)\Diebold",
            @"%PROGRAMFILES%\Diebold",
        };

        private static readonly string[] ExecutaveisGBP = new string[] {
            @"mmc",
            @"chrome",
            @"iexplore",
            @"firefox",
            @"opera",
            @"gbpsv",
            @"core"
        };

    }
}
