//
// Neuron actor class
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
using System.Runtime.InteropServices;

namespace Neuron
{
    public class NeuronActor
    {
        #region Public properties

        public int ActorID { get; private set; }

        #endregion

        #region Public methods

        public NeuronActor(int actorID)
        {
            ActorID = actorID;
        }

        public Vector3 GetReceivedPosition(NeuronBones bone)
        {
            // Return zero if no position data is available.
            // (only "Hips" is available when displacement data is disabled)
            if (_header.bWithDisp == 0 && bone != NeuronBones.Hips)
                return Vector3.zero;

            // Calculate the data offset.
            var offset =
                (_header.bWithReference == 0 ? 0 : 6 ) + (int)bone * 6;

            // Retrieve the position data.
            var x = -_data[offset++];
            var y =  _data[offset++];
            var z =  _data[offset++];

            return new Vector3(x, y, z) * 0.01f;
        }

        public Vector3 GetReceivedRotation(NeuronBones bone)
        {
            // Calculate the data offset.
            var offset =
                (_header.bWithReference == 0 ? 3 : 9 ) +
                (int)bone * (_header.bWithDisp != 0 ? 6 : 3);

            // Retrieve the rotation data.
            var y = -_data[offset++];
            var x =  _data[offset++];
            var z = -_data[offset++];

            return new Vector3(x, y, z);
        }

        #endregion

        #region Private members

        const int kMaxFrameDataLength = ((int)NeuronBones.NumOfBones + 1) * 6;

        BvhDataHeader _header;

        float[] _data = new float[kMaxFrameDataLength];

        #endregion

        #region Data callback function

        public void OnReceivedMotionData(BvhDataHeader header, IntPtr data)
        {
            _header = header;
            try
            {
                Marshal.Copy(data, _data, 0, (int)header.DataCount);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        #endregion
    }
}
