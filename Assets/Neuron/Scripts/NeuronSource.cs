//
// Neuron data source handler class
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
using System.Runtime.InteropServices;

namespace Neuron
{
    public class NeuronSource
    {
        #region Public properties;

        public string Address { get; private set; }

        public int Port { get; private set; }

        public NeuronConnection.SocketType SocketType { get; private set; }

        public IntPtr Socket { get; private set; }

        public int ReferenceCounter { get; private set; }

        #endregion

        #region Public methods

        public NeuronSource(
            string address, int port,
            NeuronConnection.SocketType socketType, IntPtr socket
        )
        {
            Address = address;
            Port = port;
            SocketType = socketType;
            Socket = socket;
            ReferenceCounter = 0;
        }

        public int IncrementReferenceCount()
        {
            return ++ReferenceCounter;
        }
        
        public int DecrementReferenceCount()
        {
            return --ReferenceCounter;
        }

        public NeuronActor AcquireActor(int actorID)
        {
            return FindOrCreateActor(actorID);
        }

        #endregion

        #region Private variables

        List<NeuronActor> _actors = new List<NeuronActor>();

        // The main thread and the network thread will possibly access to the
        // actor list and duplicatively make actor objects from a same actor.
        // This lock object is used to avoid such a conflict.
        System.Object _actorsLock = new System.Object();

        #endregion

        #region Callback function

        public virtual void OnFrameDataReceived(IntPtr DataHeader, IntPtr data)
        {
            var header = new BvhDataHeader();

            try
            {
                header = (BvhDataHeader)Marshal.PtrToStructure(
                    DataHeader, typeof(BvhDataHeader)
                );
            }
            catch(Exception e)
            {
                Debug.LogException(e);
                return;
            }

            var actor = FindOrCreateActor((int)header.AvatarIndex);
            actor.OnReceivedMotionData(header, data);
        }

        #endregion

        #region Private functions

        NeuronActor FindOrCreateActor(int actorID)
        {
            lock (_actorsLock)
            {
                foreach (var actor in _actors)
                    if (actor.ActorID == actorID) return actor;

                var newActor = new NeuronActor(actorID);
                _actors.Add(newActor);

                return newActor;
            }
        }

        #endregion
    }
}
