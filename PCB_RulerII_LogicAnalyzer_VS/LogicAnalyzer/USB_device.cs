/*
********************************************************************************
* COPYRIGHT(c) ЗАО «ЧИП и ДИП», 2017, 2018
* 
* Программное обеспечение предоставляется на условиях «как есть» (as is).
* При распространении указание автора обязательно.
********************************************************************************
*/


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibUsbDotNet;
using LibUsbDotNet.Main;

namespace LogicAnalyzer
{
    class USB_device
    {
        private const Int32 DEV_VID = 0x0483;
        private const Int32 DEV_PID = 0xA210;
        private const Int32 USBWriteTimeOut = 5000;
        private const Int32 USBReadTimeOut = 5000;
        
        static private UsbDevice USBDev;
        static private UsbEndpointWriter USBDevWriter;
        static private UsbEndpointReader USBDevReader;


        //--------------------------------------------------------------------------------------------------------
        public static Boolean Open()
        {
            UsbDeviceFinder DeviceFinder = new UsbDeviceFinder(DEV_VID, DEV_PID);

            USBDev = UsbDevice.OpenUsbDevice(DeviceFinder);

            if (USBDev == null)
                return false;
            
            IUsbDevice wholeUsbDevice = USBDev as IUsbDevice;
            if (!ReferenceEquals(wholeUsbDevice, null))
            {
                wholeUsbDevice.SetConfiguration(1);
                wholeUsbDevice.ClaimInterface(0);
            }

            USBDevReader = USBDev.OpenEndpointReader(ReadEndpointID.Ep01);
            USBDevWriter = USBDev.OpenEndpointWriter(WriteEndpointID.Ep01);

            return true;
        }
        //--------------------------------------------------------------------------------------------------------
        public static void Close()
        {
            if (USBDev != null)
            {
                if (USBDev.IsOpen)
                {
                    IUsbDevice wholeUsbDevice = USBDev as IUsbDevice;
                    if (!ReferenceEquals(wholeUsbDevice, null))
                    {
                        wholeUsbDevice.ReleaseInterface(0);
                    }

                    USBDev.Close();
                }

                USBDev = null;

                UsbDevice.Exit();
            }
        }
        //--------------------------------------------------------------------------------------------------------
        public static Boolean Write(byte[] Buffer)
        {
            ErrorCode USB_Error = ErrorCode.None;
            Int32 BytesWritten = 0;

            if (USBDevWriter == null)
                return false;

            USB_Error = USBDevWriter.Write(Buffer, USBWriteTimeOut, out BytesWritten);

            if ((USB_Error != ErrorCode.None) || (BytesWritten != Buffer.Length))
                return false;

            return true;
        }
        //--------------------------------------------------------------------------------------------------------
        public static Boolean Read(byte[] Buffer)
        {
            ErrorCode USB_Error = ErrorCode.None;
            Int32 BytesRead = 0;

            if (USBDevReader == null)
                return false;

            USB_Error = USBDevReader.Read(Buffer, USBReadTimeOut, out BytesRead);

            if ((USB_Error != ErrorCode.None) || (BytesRead != Buffer.Length))
                return false;

            return true;
        }
    }
}
