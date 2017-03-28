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
using Microsoft.Win32;
using System.Threading;


namespace EGPU
{
    public partial class Form1 : Form     
    {

        bool formIsVisible = false;

        string SERIAL_KEK = "070A615348971B35"; // серийный номер флэшки
        string VID_KEK = "13FE"; // код производителя флэшки
        string PID_KEK = "4200"; // код устройста или как его там флэшки

        string dirToFile = "C:\\Users\\Slava\\AppData\\Local\\TestFile.txt";
        string configFile = "C:\\Users\\Slava\\AppData\\Local\\kek.txt"; // там же где и приложение: Application.StartupPath;

        // нужно указывать имя батника вместе с путем
        string FullNameBat1 = @"C:\\pci_off.bat"; // file ok, flash is, power on
        string FullNameBat2 = @"C:\\pci_on.bat";// bat2: "pci on.bat" no file, no flash  power on

        volatile bool flash = false, file = false;

        bool fileOK = false;

        public Form1()
        {
            InitializeComponent();
        }
        public void print_USB_devices()
        {
            string PNPDeviceID = string.Empty;
            this.listBox1.Items.Clear(); //Предварительно очищаем список

            //Получение списка USB накопителей
            foreach (System.Management.ManagementObject drive in new System.Management.ManagementObjectSearcher(
                    "select * from Win32_USBHub where Caption='Запоминающее устройство для USB'").Get())
            {

                {

                    PNPDeviceID = drive["PNPDeviceID"].ToString().Trim();
                    //Получение Ven устройства
                    listBox1.Items.Add("VID= " + parseVidFromDeviceID(drive["PNPDeviceID"].ToString().Trim()).Trim());

                    //Получение Prod устройства
                    listBox1.Items.Add("PID= " + parsePidFromDeviceID(drive["PNPDeviceID"].ToString().Trim()).Trim());

                    //Получение Серийного номера устройства
                    string[] splitDeviceId = drive["PNPDeviceID"].ToString().Trim().Split('\\');
                    listBox1.Items.Add("Serial= " + splitDeviceId[2].Trim());


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

                        listBox1.Items.Add(drive["PNPDeviceID"].ToString().Trim().Trim());
                        timer1.Enabled = false;
                        // POWER_RESET();
                    }
                    else
                    {
                        timer1.Enabled = true;
                    }
                    // end проверки на нашу флэшку

                    //Разделение списка устройств пустой строкой
                    listBox1.Items.Add("");
                }
            } // end for each

        }
        public bool IsFlashInside()
        {
            bool resultat = false;
            string PNPDeviceID = string.Empty;
         
            //Получение списка USB накопителей
            foreach (System.Management.ManagementObject drive in new System.Management.ManagementObjectSearcher(
                    "select * from Win32_USBHub where Caption='Запоминающее устройство для USB'").Get())
            {
                PNPDeviceID = drive["PNPDeviceID"].ToString().Trim();
                //Получение Серийного номера устройства
                string[] splitDeviceId = drive["PNPDeviceID"].ToString().Trim().Split('\\');

                // проверка на совпадение: должны быть идентичны с константами:
                // 1. серийный номер флэшки
                // 2. VID (коды производителя)
                // 3. PID (код устройства)
                if (
                        SERIAL_KEK == splitDeviceId[2].Trim() &&
                        VID_KEK == parseVidFromDeviceID(drive["PNPDeviceID"].ToString().Trim()).Trim() &&
                        PID_KEK == parsePidFromDeviceID(drive["PNPDeviceID"].ToString().Trim()).Trim()
                    )
                {
                    // timer1.Enabled = false;
                    resultat = true;
                    // POWER_RESET();
                }
                else
                {
                    timer1.Enabled = true;
                    resultat = false;
                }
                // end проверки на нашу флэшку
            } // end for each

            return resultat;
        } // bool IsFlashInside();

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
            /*
            // 2. флэшки нет + файл есть --- удаляем файл
            if (!IsFlashInside() && isFileOK())
            {
                deleteFile();
                label1.Invoke((MethodInvoker)delegate()
                {
                    label1.Text = "RESET";
                });

                // run_command("shutdown /r /t 5"); // команда на перезагрузку
                Environment.Exit(0); // досрочно закрыть приложение 
            }
            // 1. флэшка есть + файла нет --- создать файл
            else if ((IsFlashInside() && !isFileOK()))
            {
                createFile(); // создать файл если его нет (из ветки 1)
                label1.Invoke((MethodInvoker)delegate()
                {
                    label1.Text = "RESET";
                });

                // run_command("shutdown /r /t 5"); // команда на перезагрузку
                Environment.Exit(0); // досрочно закрыть приложение 
            }
            */
            
            if ( (file ^ flash) == true)
            {
                if (!file) { createFile(); }
                else       { deleteFile(); }
                
                // для фонового потока
                label1.Invoke((MethodInvoker)delegate()
                {
                    label1.Text = "RESET";
                });

                // run_command("shutdown /r /t 5"); // команда на перезагрузку
                Environment.Exit(0); // досрочно закрыть приложение 
            }
            
        }
        public void POWER_ON()
        {
            /* условия:
             * 1. флэшка есть + файл есть
             * 2. флэшки нет + нет файла
             */
            // if ((IsFlashInside() && isFileOK()) || (!IsFlashInside() && !isFileOK()))
            if ( !( flash ^ file ) )
            {
                // для фонового потока
                label1.Invoke((MethodInvoker)delegate()
                {
                    label1.Text = "POWER ON";
                });
            }
        }

        private bool isFileOK()
        {
            // проверяет наличие файла на нужном месте

            this.fileOK = File.Exists(dirToFile);
            return (this.fileOK);
        }
        public void deleteFile()
        {
            try
            { 
                File.Delete(dirToFile); 
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        public void createFile()
        {
            // если файла нет - создаёт пустой файл
            if (!this.fileOK)
            {
                using (var myFile = File.Create(dirToFile))
                {
                    // interact with myFile here, it will be disposed automatically
                    myFile.Close();
                } // создали файл и закрыли его тут же
            }            
        } // end of create file

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
            // перенесено в backgroundWorker1
            // POWER_ON();
            // POWER_RESET();

            // if ( (IsFlashInside() && isFileOK())&&((IsFlashInside() && isFileOK()) || (!IsFlashInside() && !isFileOK())) )
            if (flash && file) 
            {
                run_command(FullNameBat1);
            }
            // if ( (!IsFlashInside() && !isFileOK())&&((IsFlashInside() && isFileOK()) || (!IsFlashInside() && !isFileOK())) )
            if ( !flash && !file )
            {
                run_command(FullNameBat2);
            }

            if (formIsVisible)
            {
                print_USB_devices();
            }
        }

        //автозагрузка
        const string name = "MyTestApplication";
        public bool SetAutorunValue(bool autorun)
        {
            string ExePath = System.Windows.Forms.Application.ExecutablePath;
            RegistryKey reg;
            reg = Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run\\");
            try
            {
                if (autorun)
                    reg.SetValue(name, ExePath);
                else
                    reg.DeleteValue(name);
                reg.Close();
            }
            catch  {return false;}
            return true;
        } // end set autorun

        private void PutToTray(bool isPut)
        {
            formIsVisible = !isPut;

            this.notifyIcon1.Visible = isPut;
            this.WindowState = (isPut) ? FormWindowState.Minimized : FormWindowState.Normal;
            this.ShowInTaskbar = !isPut;
        }
        private void button4_Click(object sender, EventArgs e)
        {
            PutToTray(true);
        }

        ContextMenu trayMenu = new ContextMenu();
        private void Form1_Load(object sender, EventArgs e)
        {
            // считываем файл конфига (обновим переменные и галки на форме)
            string[] lines = new string[10];
            try
            {
            lines = System.IO.File.ReadAllLines(configFile);
            foreach (string line in lines)
            {
                // берем все строки из файла, обновляем по ним наши переменные                    
                if (line.Contains("VID"))
                {
                    VID_KEK = line.Substring(line.LastIndexOf('=') + 1).Trim();
                }
                else if (line.Contains("PID"))
                {
                    PID_KEK = line.Substring(line.LastIndexOf('=') + 1).Trim();
                }
                else if (line.Contains("SERIAL"))
                {
                    SERIAL_KEK = line.Substring(line.LastIndexOf('=') + 1).Trim();
                }
                else if (line.Contains("Startup"))
                {
                    string s = line.Substring(line.LastIndexOf('=') + 1).Trim();
                    if (s == "True")
                    {
                        checkBox2.Checked = true;
                    }
                    else
                    {
                        checkBox2.Checked = false;
                    }
                }
                

                else { MessageBox.Show("YOU ARE OLEN! " + line); }
                maskedTextBox1.Text = VID_KEK;
                maskedTextBox3.Text = PID_KEK;
                maskedTextBox2.Text = SERIAL_KEK;
                }
            } // end foreach
            catch 
            {
                MessageBox.Show("Конфигурационный файл неизвестно где");    
            }
            
            timer1.Enabled = true;
            if (backgroundWorker1.IsBusy != true)
            {
                // Start the asynchronous operation.
                backgroundWorker1.RunWorkerAsync();
            }
        } // end void
        
        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            PutToTray(false);
        }

        private void pCIDisconnectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            notifyIcon1.Visible = false;
            Environment.Exit(0);
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            PutToTray(false);
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender; // приводим отправителя к элементу типа CheckBox
            if (checkBox.Checked == true)
            {
                SetAutorunValue(true);
            } 
            else     
            {
                SetAutorunValue(false);
            }
        } // end void

        private void mess()
        {
            string message = "E-GPU";
            notifyIcon1.BalloonTipTitle = message;
            notifyIcon1.BalloonTipText = "Connect";
            
            notifyIcon1.ShowBalloonTip(500);
        }

        private void keeh()
        { 
           if (IsFlashInside())
           {
               mess();
           }        
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            // применить вендор и устрйство
            // запись в файл
            string text = "VID = " + maskedTextBox1.Text + "\n";
            text += "SERIAL = " + maskedTextBox2.Text + "\n";
            text += "PID = " + maskedTextBox3.Text + "\n";

            text += "Startup = " + Convert.ToString(checkBox2.Checked) + "\n";

            System.IO.File.WriteAllText(configFile, text);
            VID_KEK = maskedTextBox1.Text;
            SERIAL_KEK = maskedTextBox2.Text;
            PID_KEK = maskedTextBox3.Text;

        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            while (true)
            {
                flash = IsFlashInside();
                file = isFileOK();

                POWER_ON();
                POWER_RESET();
                Thread.Sleep(10);
            }
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            PutToTray(true);
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            notifyIcon1.Visible = false;
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            
        }
    }
}
    
