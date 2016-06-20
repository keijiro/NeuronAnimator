/* Copyright: Copyright 2014 Beijing Noitom Technology Ltd. All Rights reserved.
* Pending Patents: PCT/CN2014/085659 PCT/CN2014/071006
* 
* Licensed under the Neuron SDK License Beta Version (the “License");
* You may only use the Neuron SDK when in compliance with the License,
* which is provided at the time of installation or download, or which
* otherwise accompanies this software in the form of either an electronic or a hard copy.
* 
* Unless required by applicable law or agreed to in writing, the Neuron SDK
* distributed under the License is provided on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing conditions and
* limitations under the License.
*/


using System;
using System.Text;
using System.Runtime.InteropServices;       // For DllImport()


namespace NeuronDataReaderWraper
{
    #region Basic data types
    /// <summary>
    /// Socket connection status
    /// </summary>
    public enum SocketStatus
    {
        CS_Running,                         // Socket is working correctly
        CS_Starting,                        // Is trying to start service
        CS_OffWork,                         // Not working
    };

    /// <summary>
    /// Data version
    /// </summary>
    public struct DataVersion
    {
        public byte BuildNumb;              // Build number
        public byte Revision;               // Revision number
        public byte Minor;                  // Subversion number
        public byte Major;                  // Major version number
    };

    /// <summary>
    /// Header format of BVH data
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack=1)]
    public struct BvhDataHeader
    {
        public ushort Token1;               // Package start token: 0xDDFF
        public DataVersion DataVersion;     // Version of community data format: 1.1.0.0
        public ushort DataCount;            // Values count
        public byte bWithDisp;              // With/out displacement
        public byte bWithReference;         // With/out reference bone data at first
        public uint AvatarIndex;            // Avatar index
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string AvatarName;           // Avatar name
        public uint FrameIndex;             // Frame data index
        public uint Reserved;               // Reserved, only enable this package has 64bytes length
        public uint Reserved1;              // Reserved, only enable this package has 64bytes length
        public uint Reserved2;              // Reserved, only enable this package has 64bytes length
        public ushort Token2;               // Package end token: 0xEEFF
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct CalcDataHeader
    {
        public ushort Token1;               // Package start token: 0x88FF
        public DataVersion DataVersion;     // Version of community data format. e.g.: 1.0.0.3
        public UInt32 DataCount;            // Values count
        public UInt32 AvatarIndex;          // Avatar index
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string AvatarName;           // Avatar name
        public UInt32 FrameIndex;           // Frame data index
        public UInt32 Reserved1;            // Reserved, only enable this package has 64bytes length
        public UInt32 Reserved2;            // Reserved, only enable this package has 64bytes length
        public UInt32 Reserved3;            // Reserved, only enable this package has 64bytes length
        public ushort Token2;               // Package end token: 0x99FF
    };

    #endregion

    #region Callbacks for data output
    /// <summary>
    /// FrameDataReceived CALLBACK
    /// Remarks
    ///   The related information of the data stream can be obtained from BvhDataHeader.
    /// </summary>
    /// <param name="customObject">User defined object.</param>
    /// <param name="sockRef">Connector reference of TCP/IP client as identity.</param>
    /// <param name="DataHeader">A DataHeader type pointer, to output the BVH/Calculation data format information.</param>
    /// <param name="data">Float type array pointer, to output binary data.</param>
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]   
    public delegate void FrameDataReceived(IntPtr customObject, IntPtr sockRef, IntPtr DataHeader, IntPtr data);
    
    /// <summary>
    /// SocketStatusChanged CALLBACK
    /// Remarks
    ///   As convenient, use BRGetSocketStatus() to get status manually other than register this callback
    /// </summary>
    /// <param name="customObject">User defined object.</param>
    /// <param name="sockRef">Socket reference of TCP or UDP service identity.</param>
    /// <param name="bvhDataHeader">Socket connection status</param>
    /// <param name="data">Socket status description.</param>
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]   
    public delegate void SocketStatusChanged(IntPtr customObject, IntPtr sockRef, SocketStatus status, [MarshalAs(UnmanagedType.LPStr)]string msg);
    #endregion

    // API exporter
    public class NeuronDataReader
    {
        #region Importor definition
#if UNITY_IPHONE && !UNITY_EDITOR
		private const string ReaderImportor = "__Internal";
#elif _WINDOWS
		private const string ReaderImportor = "NeuronDataReader.dll";
#else
        private const string ReaderImportor = "NeuronDataReader";
#endif
        #endregion

        #region Functions API
        /// <summary>
        /// Register receiving and parsed frame bvh data callback
        /// </summary>
        /// <param name="customedObj">Client defined object. Can be null</param>
        /// <param name="handle">Client defined function.</param>
        [DllImport(ReaderImportor, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern void BRRegisterFrameDataCallback(IntPtr customedObj, FrameDataReceived handle);
        /// <summary>
        /// Register receiving and parsed frame calculation data callback
        /// </summary>
        /// <param name="customedObj">Client defined object. Can be null</param>
        /// <param name="handle">Client defined function.</param>
        [DllImport(ReaderImportor, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern void BRRegisterCalculationDataCallback(IntPtr customedObj, FrameDataReceived handle);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [DllImport(ReaderImportor, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        //[return: MarshalAs(UnmanagedType.LPStr)]
		private static extern IntPtr BRGetLastErrorMessage();
		/// <summary>
		/// Call this function to get what error occurred in library.
		/// </summary>
		/// <returns></returns> 
        public static string strBRGetLastErrorMessage()
        {
            // Get message pointer
            IntPtr ptr = BRGetLastErrorMessage();
            // Construct a string from the pointer.
			return Marshal.PtrToStringAnsi(ptr);
        }

        // Register TCP socket status callback
        [DllImport(ReaderImportor, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern void BRRegisterSocketStatusCallback(IntPtr customedObj, SocketStatusChanged handle);

        // Connect to server by TCP/IP
        [DllImport(ReaderImportor, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern IntPtr BRConnectTo(string serverIP, int nPort);

        // Start a UDP service to receive data at 'nPort'
        [DllImport(ReaderImportor, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern IntPtr BRStartUDPServiceAt(int nPort);

        // Check TCP/UDP service status
        [DllImport(ReaderImportor, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern SocketStatus BRGetSocketStatus(IntPtr sockRef);

        // Close a TCP/UDP service
        [DllImport(ReaderImportor, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern void BRCloseSocket(IntPtr sockRef);
        
        #endregion
    }
}
