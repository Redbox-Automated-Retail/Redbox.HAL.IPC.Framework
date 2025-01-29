using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Redbox.HAL.IPC.Framework
{
    internal static class IPAddressHelper
    {
        private const string MicrosoftVideoDevice = "Microsoft TV/Video Connection";

        internal static IPAddress GetAddressForHostName(string hostName)
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
                return (IPAddress)null;
            IPAddress[] hostAddresses = Dns.GetHostAddresses(hostName);
            if (hostAddresses.Length == 0)
                return (IPAddress)null;
            if (string.Compare(hostName, "localhost", true) == 0)
                return hostAddresses[0];
            IPAddress addressForHostName = (IPAddress)null;
            IPAddress bindableAddress = IPAddressHelper.GetBindableAddress();
            foreach (IPAddress ipAddress in hostAddresses)
            {
                if (!(ipAddress.ToString() != bindableAddress.ToString()))
                {
                    addressForHostName = ipAddress;
                    break;
                }
            }
            return addressForHostName;
        }

        private static IPAddress GetBindableAddress()
        {
            foreach (NetworkInterface networkInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (!(networkInterface.Description == "Microsoft TV/Video Connection") || networkInterface.GetPhysicalAddress().GetAddressBytes() != new byte[6])
                {
                    IPInterfaceProperties ipProperties = networkInterface.GetIPProperties();
                    if (ipProperties != null)
                    {
                        foreach (UnicastIPAddressInformation unicastAddress in ipProperties.UnicastAddresses)
                        {
                            if (unicastAddress.Address.AddressFamily == AddressFamily.InterNetwork)
                                return unicastAddress.Address;
                        }
                    }
                }
            }
            return (IPAddress)null;
        }
    }
}
