﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Vestris.VMWareLib;
using Vestris.VMWareLib.Tools;
using System.Threading;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;

namespace VM_Manager
{
    public class TrayContext : ApplicationContext
    {
        private VMWareVirtualHost _virtualHost;
        private VMWareVirtualMachine _virtualMachine;
        private GuestOS _guestOs;

        private OpenFileDialog _ofd;

        private string _title;

        private NotifyIcon _ni = new NotifyIcon();
        private ContextMenuStrip _cms = new ContextMenuStrip();
        private ToolStripMenuItem _menuConnect = new ToolStripMenuItem("&Connect");
        private ToolStripMenuItem _menuShutdown = new ToolStripMenuItem("&Shutdown");

        public TrayContext(string[] args)
        {
            // Embed dlls in exe:
            // http://adamthetech.com/2011/06/embed-dll-files-within-an-exe-c-sharp-winforms/
            AppDomain.CurrentDomain.AssemblyResolve += (s, a) =>
            {
                string resourceName = new AssemblyName(a.Name).Name + ".dll";
                string resource = Array.Find(this.GetType().Assembly.GetManifestResourceNames(), element => element.EndsWith(resourceName));

                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource))
                {
                    Byte[] assemblyData = new Byte[stream.Length];
                    stream.Read(assemblyData, 0, assemblyData.Length);
                    return Assembly.Load(assemblyData);
                }
            };

            // must be outside the constructor! else the dlls won't be found.
            _init(args);
        }

        private void _init(string[] args)
        {
            Application.ApplicationExit += (s, e) =>
            {
                if (_virtualHost != null && _virtualHost.IsConnected)
                    _virtualHost.Disconnect();
            };

            _ni.Icon = Properties.Resources.vmware;

            _menuConnect.Enabled = false;
            _menuConnect.Click += (s, e) => _connect();
            _cms.Items.Add(_menuConnect);
            _menuShutdown.Enabled = false;
            _menuShutdown.Click += (s, e) => _shutdown();
            _cms.Items.Add(_menuShutdown);

            _ni.DoubleClick += (s, e) => _connect();
            _ni.ContextMenuStrip = _cms;

            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            Version version = assembly.GetName().Version;
            _title = Application.ProductName + " v" + version.ToString(3);

            // declaring a virtual host
            _virtualHost = new VMWareVirtualHost();
            // connect to a local VMWare Workstation virtual host
            _virtualHost.ConnectToVMWarePlayer();

            _ofd = new OpenFileDialog();
            _ofd.DefaultExt = ".vmx";
            _ofd.Filter = "Virtual Machine (.vmx)|*.vmx";
            _ofd.DereferenceLinks = false;

            string fileName;
            if (args.Length > 0 && System.IO.File.Exists(args[0]))
            {
                fileName = args[0];
            }
            else
            {
                // open dialog
                if (_ofd.ShowDialog() != DialogResult.OK)
                {
                    _closeApplication();
                }
                fileName = _ofd.FileName;
            }

            _title = System.IO.Path.GetFileName(fileName) + " - " + _title;

            _ni.Text = _title;
            _ni.Visible = true;

            ThreadExit += (s, e) =>
            {
                _ni.Visible = false;
            };

            ThreadPool.QueueUserWorkItem(delegate
            {
                _virtualMachine = _virtualHost.Open(fileName);
                _guestOs = new GuestOS(_virtualMachine);

                // power on this virtual machine
                _virtualMachine.PowerOn(Consta.VIX_VMPOWEROP_NORMAL, VMWareInterop.Timeouts.PowerOnTimeout);
                // wait for VMWare Tools
                _virtualMachine.WaitForToolsInGuest(Consta.VIX_E_TIMEOUT_WAITING_FOR_TOOLS);

                _menuConnect.Enabled = true;
                _menuShutdown.Enabled = true;
                _ni.BalloonTipClicked += (s, e) => _connect();

                _ni.ShowBalloonTip(2 * 1000, System.IO.Path.GetFileName(fileName) + " is ready to rumble!", "Click to connect", ToolTipIcon.Info);
            });
        }

        private void _shutdown()
        {
            _menuConnect.Enabled = false;
            _menuShutdown.Enabled = false;

            ThreadPool.QueueUserWorkItem(delegate
            {
                // power off
                _virtualMachine.ShutdownGuest(VMWareInterop.Timeouts.PowerOffTimeout); //.PowerOff();

                _closeApplication();
            });
        }

        private void _connect()
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                Process.Start(Environment.GetFolderPath(Environment.SpecialFolder.System) + @"\mstsc.exe", @"/v:" + _guestOs.IpAddress);
            });
        }

        private void _closeApplication()
        {
            _ni.Visible = false;
            Environment.Exit(Environment.ExitCode);
        }
    }
}