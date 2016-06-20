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

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using NeuronDataReaderWraper;

namespace Neuron
{
	public class NeuronSource
	{
		public static int									NoDataFrameTimeOut = 5000;
		public delegate void ResumeActorDelegate( NeuronActor actor );
		public delegate void SuspendActorDelegate( NeuronActor actor );
		
		List<ResumeActorDelegate>							resumeActorCallbacks = new List<ResumeActorDelegate>();
		List<SuspendActorDelegate>							suspendActorCallbacks = new List<SuspendActorDelegate>();
		Dictionary<int, NeuronActor>						activeActors = new Dictionary<int, NeuronActor>();
		Dictionary<int, NeuronActor>						suspendedActors = new Dictionary<int, NeuronActor>();
		
		public Guid											guid = Guid.NewGuid();
		public string										address { get; private set; }
		public int											port { get; private set; }
		public int											commandServerPort{ get; private set; }
		public NeuronConnection.SocketType					socketType { get; private set; }
		public IntPtr										socketReference { get; private set; }
		public IntPtr										commandSocketReference { get; private set; }
		public int											numOfActiveActors { get { return activeActors.Count; } }
		public int											numOfSuspendedActors { get { return suspendedActors.Count; } }
		public int											referenceCounter { get; private set; }
		
		public NeuronSource( string address, int port, int commandServerPort, NeuronConnection.SocketType socketType, IntPtr socketReference, IntPtr commandSocketReference )
		{
			this.address = address;
			this.port = port;
			this.socketType = socketType;
			this.socketReference = socketReference;
			this.commandSocketReference = commandSocketReference;
			this.referenceCounter = 0;
			
			// QueryNumOfActors();
		}
		
		public void RegisterResumeActorCallback( ResumeActorDelegate callback )
		{
			if( callback != null )
			{
				resumeActorCallbacks.Add( callback );
			}
		}
		
		public void UnregisterResumeActorCallback( ResumeActorDelegate callback )
		{
			if( callback != null )
			{
				resumeActorCallbacks.Remove( callback );
			}
		}
		
		public void RegisterSuspendActorCallback( SuspendActorDelegate callback )
		{
			if( callback != null )
			{
				suspendActorCallbacks.Add( callback );
			}
		}
		
		public void UnregisterSuspendActorCallback( SuspendActorDelegate callback )
		{
			if( callback != null )
			{
				suspendActorCallbacks.Remove( callback );
			}
		}
		
		public void Grab()
		{
			++referenceCounter;
		}
		
		public void Release()
		{
			--referenceCounter;
		}
		
		public void OnDestroy()
		{
			SuspendAllActors();
		}
		
		public void OnUpdate()
		{
			int now = NeuronActor.GetTimeStamp();
			List<NeuronActor> suspend_list = new List<NeuronActor>();
			foreach( KeyValuePair<int, NeuronActor> iter in activeActors )
			{
				if( now - iter.Value.timeStamp > NoDataFrameTimeOut )
				{
					suspend_list.Add( iter.Value );
				}
			}
			
			List<NeuronActor> resume_list = new List<NeuronActor>();
			foreach( KeyValuePair<int, NeuronActor> iter in suspendedActors )
			{
				if( now - iter.Value.timeStamp < NoDataFrameTimeOut )
				{
					resume_list.Add( iter.Value );
				}
			}
			
			for( int i = 0; i < suspend_list.Count; ++i )
			{
				suspend_list[i].OnNoFrameData( suspend_list[i] );
				SuspendActor( suspend_list[i] );
			}
			
			for( int i = 0; i < resume_list.Count; ++i )
			{
				resume_list[i].OnResumeFrameData( resume_list[i] );
				ResumeActor( resume_list[i] );
			}
			
			// if actor suspended or resumed, query for actors count
			//if( suspend_list.Count > 0 || resume_list.Count > 0 )
			//{
			//	QueryNumOfActors();
			//}
		}
		
		public virtual void OnFrameDataReceived( IntPtr DataHeader, IntPtr data )
		{
			BvhDataHeader header = new BvhDataHeader();
			try
			{
				header = (BvhDataHeader)Marshal.PtrToStructure( DataHeader, typeof( BvhDataHeader ) );
			}
			catch( Exception e )
			{
				Debug.LogException( e );
			}
			
			int actorID = (int)header.AvatarIndex;			
			NeuronActor actor = null;
			// find active actor
			actor = FindActiveActor( actorID );
			if( actor != null )
			{
				// if actor is active
				actor.OnReceivedMotionData( header, data );
			}
			else
			{
				// find suspended actor
				actor = FindSuspendedActor( actorID );
				if( actor == null )
				{
					// if no such actor, create one
					actor = CreateActor( actorID );
				}
				
				actor.OnReceivedMotionData( header, data );
			}
		}
		
		public virtual void OnSocketStatusChanged( SocketStatus status, string msg )
		{
		}
		
		public NeuronActor AcquireActor( int actorID )
		{
			NeuronActor actor = FindActiveActor( actorID );
			if( actor != null )
			{
				return actor;
			}
			
			actor = FindSuspendedActor( actorID );
			if( actor != null )
			{
				return actor;
			}
			
			actor = CreateActor( actorID );
			return actor;
		}
		
		public NeuronActor[] GetActiveActors()
		{
			NeuronActor[] actors = new NeuronActor[activeActors.Count];
			activeActors.Values.CopyTo( actors, 0 );
			return actors;
		}
		
		public NeuronActor[] GetActors()
		{
			NeuronActor[] actors = new NeuronActor[activeActors.Count + suspendedActors.Count];
			activeActors.Values.CopyTo( actors, 0 );
			activeActors.Values.CopyTo( actors, activeActors.Count );
			return actors;
		}
		
		NeuronActor CreateActor( int actorID )
		{
			NeuronActor find = FindSuspendedActor( actorID );
			if( find == null )
			{
				NeuronActor actor = new NeuronActor( this, actorID );
				suspendedActors.Add( actorID, actor );
				return actor;
			}
			return find;
		}
		
		void DestroyActor( int actorID )
		{
			activeActors.Remove( actorID );
			suspendedActors.Remove( actorID );
		}
		
		void SuspendActor( NeuronActor actor )
		{
			activeActors.Remove( actor.actorID );
			suspendedActors.Add( actor.actorID, actor );
			
			Debug.Log( string.Format( "[NeuronSource] Suspend actor {0}", actor.guid.ToString( "N" ) ) );
			for( int i = 0; i < suspendActorCallbacks.Count; ++i )
			{
				suspendActorCallbacks[i]( actor );
			}
		}
		
		void ResumeActor( NeuronActor actor )
		{
			suspendedActors.Remove( actor.actorID );
			activeActors.Add( actor.actorID, actor );
			
			Debug.Log( string.Format( "[NeuronSource] Resume actor {0}", actor.guid.ToString( "N" ) ) );
			
			for( int i = 0; i < resumeActorCallbacks.Count; ++i )
			{
				resumeActorCallbacks[i]( actor );
			}
		}
		
		void SuspendAllActors()
		{
			List<NeuronActor> suspend_list = new List<NeuronActor>();
			foreach( KeyValuePair<int, NeuronActor> iter in activeActors )
			{
				suspend_list.Add( iter.Value );
			}
			
			for( int i = 0; i < suspend_list.Count; ++i )
			{
				suspend_list[i].OnNoFrameData( suspend_list[i] );
				SuspendActor( suspend_list[i] );
			}
		}
		
		NeuronActor FindSuspendedActor( int actorID )
		{
			NeuronActor actor = null;
			suspendedActors.TryGetValue( actorID, out actor );
			return actor;
		}
		
		NeuronActor FindActiveActor( int actorID )
		{
			NeuronActor actor = null;
			activeActors.TryGetValue( actorID, out actor  );
			return actor;
		}
	}
}