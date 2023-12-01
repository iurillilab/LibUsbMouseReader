using Bonsai;
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using LibUsbDotNet.Main;
using LibUsbDotNet;
using System.Reactive.Disposables;
using System.Xml.Serialization;

namespace Bonsai.Extensions
{
    [Combinator]
    [Description("")]
    [WorkflowElementCategory(ElementCategory.Source)]
    public class UsbMouseReader
    {

        private static UsbDevice MyUsbDevice;

        private static UsbDeviceFinder MyUsbFinder = new UsbDeviceFinder(0x046D, 0xC08B);

        private static List<UsbDevice> usbDevicesList = new List<UsbDevice>();
        private static List<UsbEndpointReader> usbEndpointReaders = new List<UsbEndpointReader>();

        private static UsbDevice MyUsbDeviceBack = null;
        private static UsbDevice MyUsbDeviceLat = null;

        private int deviceCount = 0;

        private int UnsignedToSigned(int u, int d)
        {
            if (d < 127)
            {
                return (int)(d * 256 + u);
            }
            else
            {
                return (int)((d - 255) * 256 - 256 + u);
            }
        }

        private IObservable<int[]> ReadMouseData()
        {
            return Observable.Create<int[]>(observer =>
            {
                try
                {
                    UsbRegDeviceList allDevices = UsbDevice.AllDevices;

                    Console.WriteLine("Looping..................");
                    // foreach (UsbRegistry usbRegistry in allDevices)

                    for (int iDevice = 0; iDevice < UsbDevice.AllDevices.Count; iDevice++)
                    {

                        UsbRegistry usbRegistry = allDevices.ToList()[iDevice];
                        UsbDevice myUsbDevice = null;
                        usbRegistry.Open(out myUsbDevice);
                        Console.WriteLine(myUsbDevice.Info.ToString());
                        //{
                        // for some reason we find more than two mice:
                        if (myUsbDevice.Info.ProductString.ToString().Equals("G502 Hero Gaming Mouse", StringComparison.OrdinalIgnoreCase) & (usbEndpointReaders.Count <= 2))
                        {
                            Console.WriteLine("Trying to add");

                            usbDevicesList.Add(myUsbDevice);

                            IUsbDevice wholeUsbDevice = myUsbDevice as IUsbDevice; // C# 7.3 doesn't support "as!" for non-nullable types

                            if (wholeUsbDevice != null)
                            {
                                wholeUsbDevice.SetConfiguration(1);
                                // Claim interface # 0.
                                wholeUsbDevice.ClaimInterface(0);

                                UsbEndpointReader reader = wholeUsbDevice.OpenEndpointReader(ReadEndpointID.Ep01);

                                usbEndpointReaders.Add(reader);
                                Console.WriteLine("Adding");
                                Console.WriteLine(reader.ToString());
                                Console.WriteLine("Added");

                            }


                        }
                    }

                    int y_back = 0;
                    int x_back = 0;
                    int y_lat = 0;
                    int x_lat = 0;

                    byte[] readBuffer = new byte[8];
                    int bytesRead1;
                    int bytesRead2;
                    Console.WriteLine("1");

                    usbEndpointReaders[0].Read(readBuffer, 1000, out bytesRead1);

                    if (bytesRead1 > 0)
                    {
                        x_back = UnsignedToSigned(Convert.ToInt32(readBuffer[2]), Convert.ToInt32(readBuffer[3]));
                        y_back = UnsignedToSigned(Convert.ToInt32(readBuffer[4]), Convert.ToInt32(readBuffer[5]));
                    }

                    //Console.WriteLine("2");

                    //try
                    //{
                    //    usbEndpointReaders[1].Read(readBuffer, 10, out bytesRead2);

                    //    if (bytesRead2 > 0)
                    //    {
                    //        x_lat = UnsignedToSigned(Convert.ToInt32(readBuffer[2]), Convert.ToInt32(readBuffer[3]));
                    //        y_lat = UnsignedToSigned(Convert.ToInt32(readBuffer[4]), Convert.ToInt32(readBuffer[5]));
                    //    }
                    //}
                    //catch { }
                    Console.WriteLine("3");



                    int[] readNumbers = { x_back, y_back, x_lat, y_lat };
                    Console.WriteLine(readNumbers[0]);
                    Console.WriteLine(readNumbers[2]);
                    //Console.WriteLine(bytesRead1 + bytesRead2);
                    observer.OnNext(readNumbers);
                    //// Find and open the USB device.
                    //MyUsbDevice = UsbDevice.OpenUsbDevice(MyUsbFinder);

                    //// If the device is open and ready
                    ////if (MyUsbDevice == null)
                    ////{
                    ////    observer.OnError(new Exception("Device Not Found."));
                    ////    return 0;
                    ////}

                    //// If this is a "whole" USB device, it exposes an IUsbDevice interface.
                    //IUsbDevice wholeUsbDevice = MyUsbDevice as IUsbDevice;
                    //if (!ReferenceEquals(wholeUsbDevice, null))
                    //{
                    //    wholeUsbDevice.SetConfiguration(1);
                    //    // Claim interface #0.
                    //    wholeUsbDevice.ClaimInterface(0);
                    //}

                    //// Open read endpoint 1.
                    //UsbEndpointReader reader = MyUsbDevice.OpenEndpointReader(ReadEndpointID.Ep01);

                    //byte[] readBuffer = new byte[8];
                    //int bytesRead;

                    //while (true)
                    //{
                    //    reader.Read(readBuffer, 1000, out bytesRead);

                    //    int y = UnsignedToSigned(Convert.ToInt32(readBuffer[2]), Convert.ToInt32(readBuffer[3]));
                    //    int x = UnsignedToSigned(Convert.ToInt32(readBuffer[4]), Convert.ToInt32(readBuffer[5]));


                    //    if (bytesRead > 0)
                    //    {

                    //        observer.OnNext(Tuple.Create(x, y));// readBuffer);
                    //    }
                    //    else
                    //    {
                    //        break; // No more data to read, end the observable sequence.
                    //    }
                    //}
                }
                catch (Exception ex)
                {
                    observer.OnError(ex);
                }
                finally
                {
                    // Close USB device.
                    MyUsbDevice.Close();
                    UsbDevice.Exit();
                    observer.OnCompleted();
                }

                return Disposable.Empty; // Clean-up logic if needed.
            });
        }

        public IObservable<int[]> Process()
        {
            var mouseDataObservable = ReadMouseData();

            return mouseDataObservable.Select(data => data);

        }
    }
}
