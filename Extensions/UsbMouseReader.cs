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

[Combinator]
[Description("")]
[WorkflowElementCategory(ElementCategory.Source)]
public class UsbMouseReader
{

    private static UsbDevice MyUsbDevice;

    private static UsbDeviceFinder MyUsbFinder = new UsbDeviceFinder(0x046D, 0xC08B);


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

    private IObservable<Tuple<int, int>> ReadMouseData()
    {
        return Observable.Create<Tuple<int, int>>(observer =>
        {
            try
            {
                // Find and open the USB device.
                MyUsbDevice = UsbDevice.OpenUsbDevice(MyUsbFinder);

                // If the device is open and ready
                //if (MyUsbDevice == null)
                //{
                //    observer.OnError(new Exception("Device Not Found."));
                //    return 0;
                //}

                // If this is a "whole" USB device, it exposes an IUsbDevice interface.
                IUsbDevice wholeUsbDevice = MyUsbDevice as IUsbDevice;
                if (!ReferenceEquals(wholeUsbDevice, null))
                { 
                    wholeUsbDevice.SetConfiguration(1);
                    // Claim interface #0.
                    wholeUsbDevice.ClaimInterface(0);
                }

                // Open read endpoint 1.
                UsbEndpointReader reader = MyUsbDevice.OpenEndpointReader(ReadEndpointID.Ep01);

                byte[] readBuffer = new byte[8];
                int bytesRead;

                while (true)
                {
                    reader.Read(readBuffer, 1000, out bytesRead);

                    int y = UnsignedToSigned(Convert.ToInt32(readBuffer[2]), Convert.ToInt32(readBuffer[3]));
                    int x = UnsignedToSigned(Convert.ToInt32(readBuffer[4]), Convert.ToInt32(readBuffer[5]));


                    if (bytesRead > 0)
                    {

                        observer.OnNext(Tuple.Create(x, y));// readBuffer);
                    }
                    else
                    {
                        break; // No more data to read, end the observable sequence.
                    }
                }
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

        return mouseDataObservable.Select(data => new int[] { data.Item1, data.Item2 });

}
}
