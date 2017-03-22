using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Windows.Forms;
using System.IO;

namespace UsbVidPid
{
    public partial class Form1 : Form
    {
        string SERIAL_KEK = "3727012A96BA038732122"; // серийный номер флэшки
        string VID_KEK = "048D"; // код производителя флэшки
        string PID_KEK = "1172"; // код устройста или как его там флэшки

        string dirToFile = "C:\\TestFile.txt"; // путь к файлу, наличие которого для нас критично важно
        bool fileOK = false;

        public Form1()
        {
            InitializeComponent();
        }
        public bool IsFlashInside()
        {
            bool resultat = false;

            {
                string PNPDeviceID = string.Empty;

                this.listBox1.Items.Clear(); //Предварительно очищаем список

                //Получение списка USB накопителей
                foreach (System.Management.ManagementObject drive in new System.Management.ManagementObjectSearcher(
                        "select * from Win32_USBHub where Caption='Запоминающее устройство для USB'").Get())
                {

                    PNPDeviceID = drive["PNPDeviceID"].ToString().Trim();
                    //Получение Ven устройства
                    listBox1.Items.Add("VID= " + parseVidFromDeviceID(drive["PNPDeviceID"].ToString().Trim()).Trim());

                    //Получение Prod устройства
                    listBox1.Items.Add("PID= " + parsePidFromDeviceID(drive["PNPDeviceID"].ToString().Trim()).Trim());

                    //Получение Серийного номера устройства
                    string[] splitDeviceId = drive["PNPDeviceID"].ToString().Trim().Split('\\');
                    listBox1.Items.Add("Серийный номер= " + splitDeviceId[2].Trim());
                    textBox1.Text = splitDeviceId[2].Trim();

                    // проверка на совпадение: должны быть идентичны с константами:
                    // 1. серийный номер флэшки
                    // 2. VID (коды производителя)
                    // 3. PID (код устройства)
                    if (
                            SERIAL_KEK == splitDeviceId[2].Trim() &
                            VID_KEK == parseVidFromDeviceID(drive["PNPDeviceID"].ToString().Trim()).Trim() &
                            PID_KEK == parsePidFromDeviceID(drive["PNPDeviceID"].ToString().Trim()).Trim()
                        )
                    {
                        this.textBox1.Text = drive["PNPDeviceID"].ToString().Trim().Trim();
                        listBox1.Items.Add(drive["PNPDeviceID"].ToString().Trim().Trim());
                        timer1.Enabled = false;
                        resultat = true;
                        MessageBox.Show("reboot");
                    }
                    else
                    {
                        timer1.Enabled = true;
                        resultat = false;
                    }
                    //Разделение списка устройств пустой строкой
                    listBox1.Items.Add("");
                }
            }

            return resultat;
        }
        public void run_command(string commandToRun)
        {
            // отвечает за запуск команд командной строки

            var proc1 = new ProcessStartInfo();
            string anyCommand = commandToRun; // например, "shutdown /r /t 5"
            proc1.UseShellExecute = true;

            proc1.WorkingDirectory = @"C:\Windows\System32";
            proc1.FileName = @"C:\Windows\System32\cmd.exe";
            proc1.Verb = "runas";
            proc1.Arguments = "/c " + anyCommand; // закрывать командную строку как только выполнили команду
            proc1.WindowStyle = ProcessWindowStyle.Hidden; // hidden to hide
            Process.Start(proc1);
        }
        public void POWER_RESET()
        {
            /* условия:
             * 1. флэшка есть + файла нет --- создать файл
             * 2. флэшки нет + файл есть --- удаляем файл
             */

            // 1.               +                or 2.
            if ((IsFlashInside() & !isFileOK()) | (!IsFlashInside() & isFileOK()))
            {
                if (!isFileOK()) createFile(); // создать файл если его нет (из ветки 1)
                run_command("whoami");
            }           
        }
        public void POWER_ON()
        {
            /* условия:
             * 1. флэшка есть + файл есть
             * 2. флэшки нет + нет файла
             */
            if ((IsFlashInside() & isFileOK()) | (!IsFlashInside() & !isFileOK()))
            {
                run_command("ping localhost");
            }
        }

        private bool isFileOK()
        {
            // проверяет наличие файла на нужном месте

            this.fileOK = File.Exists(dirToFile);
            return (this.fileOK);
        }
        public void createFile()
        {
            // если файла нет - создаёт пустой файл

            if (!this.fileOK)
            {
                System.IO.File.Create(dirToFile);                
            }            
        }
        
        public string parsePCIVendor(string s)
        {
            // выделяет из строки DeviceID строку = коду VEN

            // на входе строка вида PCI\VEN_8080&DEV_1E10&SUBSYSTEMS...
            string[] l,k;
            string res;

            l = s.Split('&'); // делит строку на вендора и устройства
            k = l[0].Split('_'); // берем подстроку с производителем (первая, т.е. нулевая в массиве)
            res = k[1]; // возвратит код (второй т.е. первый в массиве) VEN = из примера: 8080

            return (res);
        }
        public string parsePCIDevId(string s)
        {
            // выделяет из строки DeviceID строку = коду VEN

            // на входе строка вида PCI\VEN_8080&DEV_1E10&SUBSYSTEMS...
            string[] l, k;
            string res;

            l = s.Split('&'); // делит строку на вендора и устройства
            k = l[1].Split('_'); // берем подстроку с устройством (вторая, т.е. первая в массиве)
            res = k[1]; // возврат правой части, т.е. номера, здесь: 1E10

            return (res);
        }
        public string getPCIVendor(string deviceId)
        {
            string res = deviceId;
            string[] list;
            // по строке извлекает код производителя

            list = res.Split('\\');
            this.listBox2.Items.Add("---");

            // в цикле смотрим на содержимое строки DeviceID на наличие строки производителя
            for(int i = 0; i < list.Length; i++)
            {
                this.listBox2.Items.Add("- " + list[i].ToString());
                if (list[i].ToString().Contains("VEN"))
                {
                    // строго говоря, бесполезная функция
                    MessageBox.Show(parsePCIVendor(list[i]) + " - " + parsePCIDevId(list[i]));
                }
            }
            return res;
        }
    
        public void searchPCI()
        {
            string[] plist;
            string l = "";

            // Для выбора доступны вместо звёздочки: DeviceId,Name, Caption,PNPDeviceId,ServiceManufaturer,Description
            string queryPCI = "SELECT * FROM Win32_PnPEntity WHERE Service='pci' ";
            foreach (System.Management.ManagementObject devices in new System.Management.ManagementObjectSearcher(queryPCI).Get())
            {
                listBox2.Items.Add(devices["Name"].ToString().Trim());
                listBox2.Items.Add("\t -" + devices["DeviceId"].ToString().Trim());
                l = devices["DeviceId"].ToString().Trim();
            }
            plist = l.Split('\\');
            getPCIVendor(l);
        }
        
        private void button1_Click(object sender, EventArgs e)
        {
            {
                IsFlashInside();
                string PNPDeviceID = string.Empty;

                this.listBox1.Items.Clear(); //Предварительно очищаем список

                //Получение списка USB накопителей
                foreach (System.Management.ManagementObject drive in new System.Management.ManagementObjectSearcher(
                        "select * from Win32_USBHub where Caption='Запоминающее устройство для USB'").Get())
                {

                    PNPDeviceID = drive["PNPDeviceID"].ToString().Trim();
                    //Получение Ven устройства
                    listBox1.Items.Add("VID= " + parseVidFromDeviceID(drive["PNPDeviceID"].ToString().Trim()).Trim());

                    //Получение Prod устройства
                    listBox1.Items.Add("PID= " + parsePidFromDeviceID(drive["PNPDeviceID"].ToString().Trim()).Trim());

                    //Получение Серийного номера устройства
                    string[] splitDeviceId = drive["PNPDeviceID"].ToString().Trim().Split('\\');
                    listBox1.Items.Add("Серийный номер= " + splitDeviceId[2].Trim());
                    textBox1.Text = splitDeviceId[2].Trim();

                    timer1.Enabled = false;
                   
                    //Разделение списка устройств пустой строкой
                    listBox1.Items.Add("");
                    this.textBox1.Text = drive["PNPDeviceID"].ToString().Trim().Trim();
                }
                
            }

        }

        // разбор строк на коды производителя и устройства USB
        private string parseVidFromDeviceID(string deviceId)
        {
            string[] splitDeviceId = deviceId.Split('\\');
            string Prod;
            //Разбиваем строку на несколько частей. 
            //Каждая чать отделяется по символу &
            string[] splitProd = splitDeviceId[1].Split('&');

            Prod = splitProd[0].Replace("VID", ""); ;
            Prod = Prod.Replace("_", " ");
            return Prod;
        }
        private string parsePidFromDeviceID(string deviceId)
        {
            string[] splitDeviceId = deviceId.Split('\\');
            string Prod;
            //Разбиваем строку на несколько частей. 
            //Каждая чать отделяется по символу &
            string[] splitProd = splitDeviceId[1].Split('&');

            Prod = splitProd[1].Replace("PID_", ""); ;
            Prod = Prod.Replace("_", " ");
            return Prod;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            IsFlashInside(); // мониторит наличие флэшки
        }

        private void button2_Click(object sender, EventArgs e)
        {
            searchPCI();
        }

        private void button3_Click(object sender, EventArgs e)
        {
           
        }
    }
}
public static class DisableHardware
{
    const uint DIF_PROPERTYCHANGE = 0x12;
    const uint DICS_ENABLE = 1;
    const uint DICS_DISABLE = 2;  // disable device
    const uint DICS_FLAG_GLOBAL = 1; // not profile-specific
    const uint DIGCF_ALLCLASSES = 4;
    const uint DIGCF_PRESENT = 2;
    const uint ERROR_INVALID_DATA = 13;
    const uint ERROR_NO_MORE_ITEMS = 259;
    const uint ERROR_ELEMENT_NOT_FOUND = 1168;

    static DEVPROPKEY DEVPKEY_Device_DeviceDesc;
    static DEVPROPKEY DEVPKEY_Device_HardwareIds;

    //[StructLayout(LayoutKind.Sequential)]
    struct SP_CLASSINSTALL_HEADER
    {
        public UInt32 cbSize;
        public UInt32 InstallFunction;
    }

    //[StructLayout(LayoutKind.Sequential)]
    struct SP_PROPCHANGE_PARAMS
    {
        public SP_CLASSINSTALL_HEADER ClassInstallHeader;
        public UInt32 StateChange;
        public UInt32 Scope;
        public UInt32 HwProfile;
    }

    //[StructLayout(LayoutKind.Sequential)]
    struct SP_DEVINFO_DATA
    {
        public UInt32 cbSize;
        public Guid classGuid;
        public UInt32 devInst;
        public IntPtr reserved;     // CHANGE #1 - was UInt32
    }

    //[StructLayout(LayoutKind.Sequential)]
    struct DEVPROPKEY
    {
        public Guid fmtid;
        public UInt32 pid;
    }

   // [DllImport("setupapi.dll", SetLastError = true)]
    static extern IntPtr SetupDiGetClassDevsW(
        [In] ref Guid ClassGuid,
        [MarshalAs(UnmanagedType.LPWStr)]
string Enumerator,
        IntPtr parent,
        UInt32 flags);

    [DllImport("setupapi.dll", SetLastError = true)]
    static extern bool SetupDiDestroyDeviceInfoList(IntPtr handle);

    [DllImport("setupapi.dll", SetLastError = true)]
    static extern bool SetupDiEnumDeviceInfo(IntPtr deviceInfoSet,
        UInt32 memberIndex,
        [Out] out SP_DEVINFO_DATA deviceInfoData);

    [DllImport("setupapi.dll", SetLastError = true)]
    static extern bool SetupDiSetClassInstallParams(
        IntPtr deviceInfoSet,
        [In] ref SP_DEVINFO_DATA deviceInfoData,
        [In] ref SP_PROPCHANGE_PARAMS classInstallParams,
        UInt32 ClassInstallParamsSize);

    [DllImport("setupapi.dll", SetLastError = true)]
    static extern bool SetupDiChangeState(
        IntPtr deviceInfoSet,
        [In] ref SP_DEVINFO_DATA deviceInfoData);

    [DllImport("setupapi.dll", SetLastError = true)]
    static extern bool SetupDiGetDevicePropertyW(
            IntPtr deviceInfoSet,
            [In] ref SP_DEVINFO_DATA DeviceInfoData,
            [In] ref DEVPROPKEY propertyKey,
            [Out] out UInt32 propertyType,
            IntPtr propertyBuffer,
            UInt32 propertyBufferSize,
            out UInt32 requiredSize,
            UInt32 flags);

    [DllImport("setupapi.dll", SetLastError = true)]
    static extern bool SetupDiGetDeviceRegistryPropertyW(
      IntPtr DeviceInfoSet,
      [In] ref SP_DEVINFO_DATA DeviceInfoData,
      UInt32 Property,
      [Out] out UInt32 PropertyRegDataType,
      IntPtr PropertyBuffer,
      UInt32 PropertyBufferSize,
      [In, Out] ref UInt32 RequiredSize
    );

    static DisableHardware()
    {
        DisableHardware.DEVPKEY_Device_DeviceDesc = new DEVPROPKEY();
        DEVPKEY_Device_DeviceDesc.fmtid = new Guid(
                0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67,
                0xd1, 0x46, 0xa8, 0x50, 0xe0);
        DEVPKEY_Device_DeviceDesc.pid = 2;

        DEVPKEY_Device_HardwareIds = new DEVPROPKEY();
        DEVPKEY_Device_HardwareIds.fmtid = new Guid(
            0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67,
            0xd1, 0x46, 0xa8, 0x50, 0xe0);
        DEVPKEY_Device_HardwareIds.pid = 3;
    }




    public static void DisableDevice(Func<string, bool> filter, bool disable = true)
    {
        IntPtr info = IntPtr.Zero;
        Guid NullGuid = Guid.Empty;
        try
        {
            info = SetupDiGetClassDevsW(
                ref NullGuid,
                null,
                IntPtr.Zero,
                DIGCF_ALLCLASSES);
            CheckError("SetupDiGetClassDevs");

            SP_DEVINFO_DATA devdata = new SP_DEVINFO_DATA();
            devdata.cbSize = (UInt32)Marshal.SizeOf(devdata);

            // Get first device matching device criterion.
            for (uint i = 0; ; i++)
            {
                SetupDiEnumDeviceInfo(info,
                    i,
                    out devdata);
                // if no items match filter, throw
                if (Marshal.GetLastWin32Error() == ERROR_NO_MORE_ITEMS)
                    CheckError("No device found matching filter.", 0xcffff);
                CheckError("SetupDiEnumDeviceInfo");

                string devicepath = GetStringPropertyForDevice(info,
                                           devdata, 1); // SPDRP_HARDWAREID

                // Uncomment to print name/path
                //Console.WriteLine(GetStringPropertyForDevice(info,
                //                         devdata, DEVPKEY_Device_DeviceDesc));
                //Console.WriteLine("   {0}", devicepath);
                if (devicepath != null && filter(devicepath)) break;

            }

            SP_CLASSINSTALL_HEADER header = new SP_CLASSINSTALL_HEADER();
            header.cbSize = (UInt32)Marshal.SizeOf(header);
            header.InstallFunction = DIF_PROPERTYCHANGE;

            SP_PROPCHANGE_PARAMS propchangeparams = new SP_PROPCHANGE_PARAMS();
            propchangeparams.ClassInstallHeader = header;
            propchangeparams.StateChange = disable ? DICS_DISABLE : DICS_ENABLE;
            propchangeparams.Scope = DICS_FLAG_GLOBAL;
            propchangeparams.HwProfile = 0;

            SetupDiSetClassInstallParams(info,
                ref devdata,
                ref propchangeparams,
                (UInt32)Marshal.SizeOf(propchangeparams));
            CheckError("SetupDiSetClassInstallParams");

            SetupDiChangeState(
                info,
                ref devdata);
            CheckError("SetupDiChangeState");
        }
        finally
        {
            if (info != IntPtr.Zero)
                SetupDiDestroyDeviceInfoList(info);
        }
    }
    private static void CheckError(string message, int lasterror = -1)
    {

        int code = lasterror == -1 ? Marshal.GetLastWin32Error() : lasterror;
        if (code != 0)
            throw new ApplicationException(
                String.Format("Error disabling hardware device (Code {0}): {1}",
                    code, message));
    }

    private static string GetStringPropertyForDevice(IntPtr info, SP_DEVINFO_DATA devdata,
        uint propId)
    {
        uint proptype, outsize;
        IntPtr buffer = IntPtr.Zero;
        try
        {
            uint buflen = 512;
            buffer = Marshal.AllocHGlobal((int)buflen);
            outsize = 0;
            // CHANGE #2 - Use this instead of SetupDiGetDeviceProperty 
            SetupDiGetDeviceRegistryPropertyW(
                info,
                ref devdata,
                propId,
                out proptype,
                buffer,
                buflen,
                ref outsize);
            byte[] lbuffer = new byte[outsize];
            Marshal.Copy(buffer, lbuffer, 0, (int)outsize);
            int errcode = Marshal.GetLastWin32Error();
            if (errcode == ERROR_INVALID_DATA) return null;
            CheckError("SetupDiGetDeviceProperty", errcode);
            return Encoding.Unicode.GetString(lbuffer);
        }
        finally
        {
            if (buffer != IntPtr.Zero)
                Marshal.FreeHGlobal(buffer);
        }
    }
}
