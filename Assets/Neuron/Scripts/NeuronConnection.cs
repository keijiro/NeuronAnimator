//
// Neuron connection class
//
// Refactored by Keijiro Takahashi
// https://github.com/keijiro/NeuronRetargeting
//
// This is a derivative work of the Perception Neuron SDK. You can use this
// freely as one of "Perception Neuron SDK Derivatives". See LICENSE.pdf and
// their website for further details.
//
// The following description is from the original source code.
//

/************************************************************************************
 Copyright: Copyright 2014 Beijing Noitom Technology Ltd. All Rights reserved.
 Pending Patents: PCT/CN2014/085659 PCT/CN2014/071006

 Licensed under the Perception Neuron SDK License Beta Version (the “License");
 You may only use the Perception Neuron SDK when in compliance with the License,
 which is provided at the time of installation or download, or which
 otherwise accompanies this software in the form of either an electronic or a hard copy.

 A copy of the License is included with this package or can be obtained at:
 http://www.neuronmocap.com

 Unless required by applicable law or agreed to in writing, the Perception Neuron SDK
 distributed under the License is provided on an "AS IS" BASIS,
 WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 See the License for the specific language governing conditions and
 limitations under the License.
************************************************************************************/

using UnityEngine;
using NeuronDataReaderWraper;
using System;
using System.Collections.Generic;

namespace Neuron
{
    public static class NeuronConnection
    {
        #region Public functions

        public enum SocketType { TCP, UDP }

        public static NeuronSource Connect(string address, int port, SocketType socketType)
        {
            var source = FindConnection(address, port, socketType);

            if (source == null)
                source = CreateConnection(address, port, socketType);
            
            if (source != null) source.Grab();

            return source;
        }

        public static void Disconnect(NeuronSource source)
        {
            if (source == null) return;

            source.Release();

            if (source.referenceCounter == 0)
                DestroyConnection(source);
        }

        #endregion

        #region Internal members

        // The network thread will access to one of sources when receiving
        // data from a device. This lock object is used to protect the object
        // from deletion by the main thread.
        static System.Object _sourcesLock = new System.Object();

        static Dictionary<Guid, NeuronSource> _guidToSourceMap = new Dictionary<Guid, NeuronSource>();
        static Dictionary<IntPtr, NeuronSource> _socketToSourceMap = new Dictionary<IntPtr, NeuronSource>();

        static NeuronSource FindConnection(string address, int port, SocketType socketType)
        {
            if (socketType == SocketType.TCP)
            {
                foreach (var source in _guidToSourceMap.Values)
                    if (source.socketType == SocketType.TCP && source.address == address && source.port == port)
                        return source;
            }
            else // SocketType.UDP
            {
                foreach (var source in _guidToSourceMap.Values)
                    if (source.socketType == SocketType.UDP && source.port == port)
                        return source;
            }
            return null;
        }

        static NeuronSource CreateConnection(string address, int port, SocketType socketType)
        {
            // Try to make connection with using the native plugin.
            var socket = IntPtr.Zero;

            if (socketType == SocketType.TCP)
            {
                socket = NeuronDataReader.BRConnectTo(address, port);

                if (socket == IntPtr.Zero) {
                    Debug.LogError("[Neuron] Connection failed " + address + ":" + port);
                    return null;
                }

                Debug.Log("[Neuron] Connected " + address + ":" + port);
            }
            else
            {
                socket = NeuronDataReader.BRStartUDPServiceAt(port);

                if (socket == IntPtr.Zero) {
                    Debug.LogError("[Neuron] Failed listening " + port);
                    return null;
                }

                Debug.Log("[Neuron] Started listening " + port);
            }

            // If this is the first connection, register the reader callack.
            if (_guidToSourceMap.Count == 0)
                NeuronDataReader.BRRegisterFrameDataCallback(IntPtr.Zero, OnFrameDataReceived);

            // Create a new source.
            var source = new NeuronSource(address, port, socketType, socket);

            lock (_sourcesLock)
            {
                _guidToSourceMap.Add(source.guid, source);
                _socketToSourceMap.Add(socket, source);
            }

            return source;
        }

        static void DestroyConnection(NeuronSource source)
        {
            if (source == null) return;

            // We have to make a lock before removing the source from the maps.
            lock (_sourcesLock)
            {
                _guidToSourceMap.Remove(source.guid);
                _socketToSourceMap.Remove(source.socketReference);
            }
            // Now we can delete the source safely!

            source.OnDestroy();

            if (source.socketType == SocketType.TCP)
            {
                NeuronDataReader.BRCloseSocket(source.socketReference);
                Debug.Log("[Neuron] Disconnected " + source.address + ":" + source.port);
            }
            else
            {
                NeuronDataReader.BRCloseSocket(source.socketReference);
                Debug.Log("[Neuron] Stopped listening " + source.port + ", " + source.guid);
            }

            // Unregister the reader callback if this was the last connection.
            if (_guidToSourceMap.Count == 0)
                NeuronDataReader.BRRegisterFrameDataCallback(IntPtr.Zero, null);
        }

        static void OnFrameDataReceived(
            IntPtr customObject, IntPtr socket,
            IntPtr header, IntPtr data
        )
        {
            lock (_sourcesLock)
            {
                NeuronSource source;
                if (_socketToSourceMap.TryGetValue(socket, out source))
                    source.OnFrameDataReceived(header, data);
            }
        }

        #endregion
    }
}
